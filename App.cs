namespace todoCli;

public class App {
    private readonly TodoRepository _repository = new();

    public async Task RunAsync(string[] args) {
        await CheckOverdueTodosAsync();

        if (args.Length == 0) {
            await ListUncompletedTodosAsync();
            Console.ReadKey();
        }
        else {
            switch (args[0].ToLower()) {
                case "add":
                    await AddTodoAsync();
                    break;
                case "comp":
                    if (args.Length > 1)
                        await CompleteTodoAsync(args[1]);
                    else
                        Console.WriteLine("Please provide an ID or title to complete a todo.");
                    break;
                case "over":
                    await ListOverdueTodosAsync();
                    break;
                default:
                    Console.WriteLine(
                        "Invalid command. Use 'add' to add a new todo, 'comp' to complete a todo, 'over' to list overdue todos, or run without parameters to list uncompleted todos.");
                    break;
            }
        }
    }

    private async Task AddTodoAsync() {
        Console.WriteLine("Adding a new todo");

        Console.Write("Title: ");
        string? title = Console.ReadLine();

        Console.Write("Description (optional, press Enter to skip): ");
        string? description = Console.ReadLine();

        Console.Write("Due date (yyyy-MM-dd, press Enter to skip): ");
        string? dueDateString = Console.ReadLine();
        DateTime dueDate = DateTime.MinValue;
        if (!string.IsNullOrWhiteSpace(dueDateString)) {
            if (DateTime.TryParse(dueDateString, out DateTime parsedDate))
                dueDate = parsedDate;
            else
                Console.WriteLine("Invalid date format. Using default value.");
        }

        Todo newTodo = new Todo {
            Title = title,
            Description = description,
            IsCompleted = false,
            ReminderOff = false,
            CreatedAt = DateTime.Now,
            DueDate = dueDate
        };

        Todo addedTodo = await _repository.AddTodoAsync(newTodo);
        Console.WriteLine($"Todo added successfully with ID: {addedTodo.Id}");
    }

    private async Task CompleteTodoAsync(string idOrTitle) {
        List<Todo> todos = await _repository.GetAllTodosAsync();

        List<Todo> matchingTodos = int.TryParse(idOrTitle, out int id)
            ? todos.Where(t => t.Id == id).ToList()
            : todos.Where(t => t.Title != null && t.Title.Equals(idOrTitle, StringComparison.OrdinalIgnoreCase))
                .ToList();

        switch (matchingTodos.Count) {
            case 0:
                Console.WriteLine("No matching todo found.");
                return;
            case > 1: {
                Console.WriteLine("Multiple todos found with the same title. Please choose by ID:");
                foreach (Todo todo in matchingTodos) {
                    Console.WriteLine($"ID: {todo.Id}, Title: {todo.Title}, Description: {todo.Description}");
                }

                Console.Write("Enter the ID of the todo you want to complete: ");
                if (int.TryParse(Console.ReadLine(), out int chosenId)) {
                    Todo? chosenTodo = matchingTodos.FirstOrDefault(t => t.Id == chosenId);
                    if (chosenTodo != null) {
                        await CompleteTodo(chosenTodo);
                    }
                    else {
                        Console.WriteLine("Invalid ID entered.");
                    }
                }
                else {
                    Console.WriteLine("Invalid input. Please enter a valid ID.");
                }

                break;
            }
            default:
                await CompleteTodo(matchingTodos[0]);
                break;
        }
    }

    private async Task CheckOverdueTodosAsync() {
        List<Todo> todos = await _repository.GetAllTodosAsync();
        DateTime today = DateTime.Today;
        bool updatesPerformed = false;

        foreach (Todo todo in todos.Where(todo =>
                     todo is { IsCompleted: false, ReminderOff: false } && todo.DueDate != DateTime.MinValue &&
                     todo.DueDate.Date < today)) {
            todo.ReminderOff = true;
            await _repository.UpdateTodoAsync(todo);
            updatesPerformed = true;
        }

        if (updatesPerformed) {
            Console.WriteLine("Some overdue todos have had their reminders turned off.");
        }
    }

    private async Task ListOverdueTodosAsync() {
        List<Todo> todos = await _repository.GetAllTodosAsync();
        DateTime today = DateTime.Today;
        List<Todo> overdueTodos = todos
            .Where(t => !t.IsCompleted && t.DueDate != DateTime.MinValue && t.DueDate.Date < today).ToList();

        if (overdueTodos.Count == 0) {
            Console.WriteLine("No overdue todos found.");
        }
        else {
            Console.WriteLine("Overdue todos:");
            foreach (Todo todo in overdueTodos) {
                Console.WriteLine($"ID: {todo.Id}, Title: {todo.Title}");
                Console.WriteLine($"Description: {todo.Description}");
                Console.WriteLine($"Due Date: {todo.DueDate:yyyy-MM-dd}");
                Console.WriteLine($"Reminder: {(todo.ReminderOff ? "Off" : "On")}");
                Console.WriteLine("---");
            }
        }
    }

    private async Task ListUncompletedTodosAsync() {
        List<Todo> todos = await _repository.GetAllTodosAsync();
        List<Todo> uncompletedTodos = todos.Where(t => t is { IsCompleted: false, ReminderOff: false }).ToList();

        if (uncompletedTodos.Count == 0) {
            Console.WriteLine("No uncompleted todos with active reminders found.");
        }
        else {
            Console.WriteLine("Uncompleted todos with active reminders:");
            foreach (Todo todo in uncompletedTodos) {
                Console.WriteLine($"ID: {todo.Id}, Title: {todo.Title}");
                Console.WriteLine($"Description: {todo.Description}");
                Console.WriteLine(
                    $"Due Date: {(todo.DueDate == DateTime.MinValue ? "Not set" : todo.DueDate.ToString("yyyy-MM-dd"))}");
                Console.WriteLine("---");
            }
        }
    }


    private async Task CompleteTodo(Todo todo) {
        todo.IsCompleted = true;
        todo.ReminderOff = true;
        await _repository.UpdateTodoAsync(todo);
        Console.WriteLine($"Todo '{todo.Title}' (ID: {todo.Id}) has been marked as completed.");
    }
}