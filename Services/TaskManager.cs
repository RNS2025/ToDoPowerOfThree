using PowerOfThree.Models;
using Blazored.LocalStorage;

namespace PowerOfThree.Services
{
    public class TaskManager
    {
        private readonly ILocalStorageService _localStorage;
        private List<TodoTask> _tasks = new();
        private DailyTaskSelection? _todaySelection;

        public event Action? TasksChanged;
        public event Action? TodaySelectionChanged;

        public TaskManager(ILocalStorageService localStorage)
        {
            _localStorage = localStorage;
        }

        public async Task InitializeAsync()
        {
            _tasks = await _localStorage.GetItemAsync<List<TodoTask>>("tasks") ?? new();

            // Catch-up logic for recurring tasks
            bool tasksModified = false;
            foreach (var task in _tasks)
            {
                if (task.IsRecurring && task.RecurrenceUnit != RecurrenceUnitType.None && task.Deadline < DateTime.Today)
                {
                    // Advance deadline until it's today or in the future
                    while (task.Deadline < DateTime.Today)
                    {
                        task.Deadline = CalculateNextDueDate(task.Deadline, task.RecurrenceUnit, task.RecurrenceInterval);
                        tasksModified = true;
                    }
                    task.IsCompleted = false; // Ensure it's not marked completed if its deadline passed
                }
            }
            if (tasksModified)
            {
                await SaveTasksAsync(); // Save changes if any deadlines were advanced
            }

            _todaySelection = await _localStorage.GetItemAsync<DailyTaskSelection>("todaySelection");

            // Reset today's selection if it's from a different day
            if (_todaySelection?.SelectionDate.Date != DateTime.Today)
            {
                _todaySelection = null;
                await _localStorage.RemoveItemAsync("todaySelection");
            }
        }

        public List<TodoTask> GetAllTasks() => _tasks.ToList();
        public DailyTaskSelection? GetTodaySelection() => _todaySelection;

        public async Task AddTaskAsync(TodoTask task)
        {
            _tasks.Add(task);
            await SaveTasksAsync();
            TasksChanged?.Invoke();
        }

        public async Task UpdateTaskAsync(TodoTask updatedTask)
        {
            var index = _tasks.FindIndex(t => t.Id == updatedTask.Id);
            if (index >= 0)
            {
                // Handle completion logic for recurring tasks specifically
                if (updatedTask.IsCompleted && updatedTask.IsRecurring && updatedTask.RecurrenceUnit != RecurrenceUnitType.None)
                {
                    var originalTask = _tasks[index]; // Get current deadline before updating
                    updatedTask.CompletedAt = DateTime.Now; // Mark when this instance was completed
                    updatedTask.Deadline = CalculateNextDueDate(originalTask.Deadline, updatedTask.RecurrenceUnit, updatedTask.RecurrenceInterval);
                    updatedTask.IsCompleted = false; // Reset for the next occurrence
                }
                else if (updatedTask.IsCompleted) // Non-recurring task completed
                {
                    updatedTask.CompletedAt = DateTime.Now;
                }
                // If a recurring task is being edited and IsRecurring is unchecked, or unit is None
                if (!updatedTask.IsRecurring || updatedTask.RecurrenceUnit == RecurrenceUnitType.None)
                {
                    updatedTask.IsRecurring = false; // Ensure consistency
                    updatedTask.RecurrenceUnit = RecurrenceUnitType.None;
                }


                _tasks[index] = updatedTask;
                await SaveTasksAsync();

                // Update today's selection if this task is selected
                if (_todaySelection?.SelectedTasks.Any(t => t.Id == updatedTask.Id) == true)
                {
                    var selectionIndex = _todaySelection.SelectedTasks.FindIndex(t => t.Id == updatedTask.Id);
                    if (selectionIndex >= 0)
                    {
                        // Ensure the task in today's selection reflects the (potentially) new deadline
                        // and its IsCompleted status (which will be false for a just-completed recurring task)
                        _todaySelection.SelectedTasks[selectionIndex] = updatedTask;
                        await SaveTodaySelectionAsync();
                        TodaySelectionChanged?.Invoke();
                    }
                }
                TasksChanged?.Invoke();
            }
        }

        public async Task DeleteTaskAsync(Guid taskId)
        {
            _tasks.RemoveAll(t => t.Id == taskId);

            // Remove from today's selection if present
            if (_todaySelection != null)
            {
                _todaySelection.SelectedTasks.RemoveAll(t => t.Id == taskId);
                await SaveTodaySelectionAsync();
                TodaySelectionChanged?.Invoke();
            }

            await SaveTasksAsync();
            TasksChanged?.Invoke();
        }

        public async Task CompleteTaskAsync(Guid taskId)
        {
            var task = _tasks.FirstOrDefault(t => t.Id == taskId);
            if (task != null && !task.IsCompleted)
            {
                task.IsCompleted = true;
                task.CompletedAt = DateTime.Now;
                await UpdateTaskAsync(task);
            }
        }

        public async Task<DailyTaskSelection> GenerateTodayTasksAsync(TaskGenerationMode mode)
        {
            var availableTasks = _tasks.Where(t => !t.IsCompleted).ToList();
            var selectedTasks = new List<TodoTask>();

            switch (mode)
            {
                case TaskGenerationMode.Random:
                    selectedTasks = SelectRandomTasks(availableTasks, 3);
                    break;

                case TaskGenerationMode.ByPriority:
                    selectedTasks = SelectByPriority(availableTasks);
                    break;

                case TaskGenerationMode.ByDeadline:
                    selectedTasks = SelectByDeadline(availableTasks);
                    break;

                case TaskGenerationMode.Balanced:
                    selectedTasks = SelectBalanced(availableTasks);
                    break;
            }

            _todaySelection = new DailyTaskSelection
            {
                SelectedTasks = selectedTasks,
                SelectionDate = DateTime.Today,
                GenerationMode = mode
            };

            await SaveTodaySelectionAsync();
            TodaySelectionChanged?.Invoke();
            return _todaySelection;
        }

        private List<TodoTask> SelectRandomTasks(List<TodoTask> tasks, int count)
        {
            var random = new Random();
            return tasks.OrderBy(x => random.Next()).Take(Math.Min(count, tasks.Count)).ToList();
        }

        private List<TodoTask> SelectByPriority(List<TodoTask> tasks)
        {
            var priorityOrder = new Dictionary<string, int>
            {
                {"High", 3}, {"Medium", 2}, {"Low", 1}
            };

            return tasks
                .OrderByDescending(t => priorityOrder.GetValueOrDefault(t.Priority, 0))
                .ThenBy(t => t.Deadline)
                .Take(3)
                .ToList();
        }

        private List<TodoTask> SelectByDeadline(List<TodoTask> tasks)
        {
            return tasks
                .OrderBy(t => t.Deadline)
                .Take(3)
                .ToList();
        }

        private List<TodoTask> SelectBalanced(List<TodoTask> tasks)
        {
            var result = new List<TodoTask>();
            var availableTasks = tasks.Where(t => !t.IsCompleted).ToList(); // Ensure we only consider non-completed

            // Try to get one of each priority
            var highPriority = availableTasks.Where(t => t.Priority == "High").OrderBy(t => t.Deadline).FirstOrDefault();
            var mediumPriority = availableTasks.Where(t => t.Priority == "Medium").OrderBy(t => t.Deadline).FirstOrDefault();
            var lowPriority = availableTasks.Where(t => t.Priority == "Low").OrderBy(t => t.Deadline).FirstOrDefault();

            if (highPriority != null) result.Add(highPriority);
            if (mediumPriority != null) result.Add(mediumPriority);
            if (lowPriority != null) result.Add(lowPriority);

            // Fill remaining slots with highest priority available
            var remaining = tasks.Except(result).OrderByDescending(t => t.Priority == "High" ? 3 : t.Priority == "Medium" ? 2 : 1);
            result.AddRange(remaining.Take(3 - result.Count));

            return result.Take(3).ToList();
        }

        public async Task ClearTodaySelectionAsync()
        {
            _todaySelection = null;
            await _localStorage.RemoveItemAsync("todaySelection");
            TodaySelectionChanged?.Invoke();
        }

        private async Task SaveTasksAsync()
        {
            await _localStorage.SetItemAsync("tasks", _tasks);
        }

        private async Task SaveTodaySelectionAsync()
        {
            if (_todaySelection != null)
            {
                // Ensure tasks in today's selection are fresh copies from _tasks
                // This is important if a recurring task's deadline was advanced.
                for (int i = 0; i < _todaySelection.SelectedTasks.Count; i++)
                {
                    var selectedTaskId = _todaySelection.SelectedTasks[i].Id;
                    var masterTask = _tasks.FirstOrDefault(t => t.Id == selectedTaskId);
                    if (masterTask != null)
                    {
                        _todaySelection.SelectedTasks[i] = masterTask;
                    }
                }
                await _localStorage.SetItemAsync("todaySelection", _todaySelection);
            }
        }

        private DateTime CalculateNextDueDate(DateTime currentDueDate, RecurrenceUnitType unit, int interval)
        {
            if (interval <= 0) interval = 1; // Ensure interval is positive

            return unit switch
            {
                RecurrenceUnitType.Day => currentDueDate.AddDays(interval),
                RecurrenceUnitType.Week => currentDueDate.AddDays(interval * 7),
                RecurrenceUnitType.Month => currentDueDate.AddMonths(interval),
                RecurrenceUnitType.Year => currentDueDate.AddYears(interval),
                _ => currentDueDate.AddDays(1) // Default or None case
            };
        }
    }
}
