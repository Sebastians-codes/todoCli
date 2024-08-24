using Microsoft.Data.Sqlite;

namespace todoCli;

public class TodoRepository {
    private const string ConnectionString = "Data Source=todos.db";

    public TodoRepository() {
        InitializeDatabase();
    }

    private void InitializeDatabase() {
        using SqliteConnection connection = new(ConnectionString);
        connection.Open();

        SqliteCommand command = connection.CreateCommand();
        command.CommandText = @"
                    CREATE TABLE IF NOT EXISTS Todos (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Title TEXT NOT NULL,
                        Description TEXT,
                        IsCompleted INTEGER NOT NULL,
                        ReminderOff INTEGER NOT NULL DEFAULT 0,
                        CreatedAt TEXT NOT NULL,
                        DueDate TEXT
                    )";
        command.ExecuteNonQuery();
    }

    public async Task<Todo> AddTodoAsync(Todo todo) {
        await using SqliteConnection connection = new(ConnectionString);
        await connection.OpenAsync();

        SqliteCommand command = connection.CreateCommand();
        command.CommandText = @"
                    INSERT INTO Todos (Title, Description, IsCompleted, ReminderOff, CreatedAt, DueDate)
                    VALUES (@Title, @Description, @IsCompleted, @ReminderOff, @CreatedAt, @DueDate);
                    SELECT last_insert_rowid();";

        command.Parameters.AddWithValue("@Title", todo.Title);
        command.Parameters.AddWithValue("@Description", todo.Description);
        command.Parameters.AddWithValue("@IsCompleted", todo.IsCompleted ? 1 : 0);
        command.Parameters.AddWithValue("@ReminderOff", todo.ReminderOff ? 1 : 0);
        command.Parameters.AddWithValue("@CreatedAt", todo.CreatedAt.ToString("O"));
        command.Parameters.AddWithValue("@DueDate", todo.DueDate.ToString("O"));

        int id = Convert.ToInt32(await command.ExecuteScalarAsync());
        todo.Id = id;

        return todo;
    }

    public async Task<List<Todo>> GetAllTodosAsync() {
        List<Todo> todos = [];

        await using SqliteConnection connection = new(ConnectionString);
        await connection.OpenAsync();

        SqliteCommand command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM Todos";

        await using SqliteDataReader reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync()) {
            todos.Add(new Todo {
                Id = reader.GetInt32(0),
                Title = reader.GetString(1),
                Description = reader.IsDBNull(2) ? null : reader.GetString(2),
                IsCompleted = reader.GetInt32(3) == 1,
                ReminderOff = reader.GetInt32(4) == 1,
                CreatedAt = DateTime.Parse(reader.GetString(5)),
                DueDate = reader.IsDBNull(6) ? DateTime.MinValue : DateTime.Parse(reader.GetString(6))
            });
        }

        return todos;
    }

    public async Task UpdateTodoAsync(Todo todo) {
        await using SqliteConnection connection = new(ConnectionString);
        await connection.OpenAsync();

        SqliteCommand command = connection.CreateCommand();
        command.CommandText = @"
        UPDATE Todos 
        SET Title = @Title, Description = @Description, IsCompleted = @IsCompleted, 
            ReminderOff = @ReminderOff, CreatedAt = @CreatedAt, DueDate = @DueDate
        WHERE Id = @Id";

        command.Parameters.AddWithValue("@Id", todo.Id);
        command.Parameters.AddWithValue("@Title", todo.Title);
        command.Parameters.AddWithValue("@Description", todo.Description);
        command.Parameters.AddWithValue("@IsCompleted", todo.IsCompleted ? 1 : 0);
        command.Parameters.AddWithValue("@ReminderOff", todo.ReminderOff ? 1 : 0);
        command.Parameters.AddWithValue("@CreatedAt", todo.CreatedAt.ToString("O"));
        command.Parameters.AddWithValue("@DueDate", todo.DueDate.ToString("O"));

        await command.ExecuteNonQueryAsync();
    }
}