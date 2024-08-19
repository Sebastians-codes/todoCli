namespace todoCli;

public class Todo {
    public int Id { get; set; }
    public string? Title { get; init; }
    public string? Description { get; init; }
    public bool IsCompleted { get; set; }
    public bool ReminderOff { get; set; }
    public DateTime CreatedAt { get; init; }
    public DateTime DueDate { get; set; }
}