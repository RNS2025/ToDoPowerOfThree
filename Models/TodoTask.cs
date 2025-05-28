namespace PowerOfThree.Models
{
    public class TodoTask
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> SubTasks { get; set; } = new();
        public DateTime Deadline { get; set; } = DateTime.Today.AddDays(1);
        public string Priority { get; set; } = "Medium"; // "Low", "Medium", "High"
        public bool IsCompleted { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? CompletedAt { get; set; }
        public bool IsSelectedForToday { get; set; } = false;
        public string Category { get; set; } = "General"; // For better organization
        public int EstimatedMinutes { get; set; } = 30; // Time estimation

        // Recurrence Properties
        public bool IsRecurring { get; set; } = false;
        public RecurrenceUnitType RecurrenceUnit { get; set; } = RecurrenceUnitType.None;
        public int RecurrenceInterval { get; set; } = 1; // e.g., Every 1 Day, Every 2 Weeks
    }

    public enum RecurrenceUnitType
    {
        None,
        Day,
        Week,
        Month,
        Year
    }

    public enum TaskGenerationMode
    {
        Random,
        ByPriority,
        ByDeadline,
        Balanced // 1 High, 1 Medium, 1 Low
    }

    public class DailyTaskSelection
    {
        public List<TodoTask> SelectedTasks { get; set; } = new();
        public DateTime SelectionDate { get; set; } = DateTime.Today;
        public TaskGenerationMode GenerationMode { get; set; }
        public int CompletedCount => SelectedTasks.Count(t => t.IsCompleted);
        public bool IsAllCompleted => SelectedTasks.Count == 3 && CompletedCount == 3;
    }
}
