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
                _tasks[index] = updatedTask;
                await SaveTasksAsync();

                // Update today's selection if this task is selected
                if (_todaySelection?.SelectedTasks.Any(t => t.Id == updatedTask.Id) == true)
                {
                    var selectionIndex = _todaySelection.SelectedTasks.FindIndex(t => t.Id == updatedTask.Id);
                    if (selectionIndex >= 0)
                    {
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

            // Try to get one of each priority
            var highPriority = tasks.Where(t => t.Priority == "High").OrderBy(t => t.Deadline).FirstOrDefault();
            var mediumPriority = tasks.Where(t => t.Priority == "Medium").OrderBy(t => t.Deadline).FirstOrDefault();
            var lowPriority = tasks.Where(t => t.Priority == "Low").OrderBy(t => t.Deadline).FirstOrDefault();

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
                await _localStorage.SetItemAsync("todaySelection", _todaySelection);
            }
        }
    }
}
