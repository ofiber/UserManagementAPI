var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add a singleton service to manage user data
builder.Services.AddSingleton<IUserService, UserService>();

var app = builder.Build();

// Middleware to catch unhandled exceptions and return consistent error responses in JSON format
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        var errorResponse = new { Message = "An unexpected error occurred.", Details = ex.Message };
        await context.Response.WriteAsJsonAsync(errorResponse);
    }
});

// Middleware to validate tokens
app.Use(async (context, next) =>
{
    if (!context.Request.Headers.TryGetValue("Authorization", out var token) || !ValidateTokenAttribute.ValidateToken(token))
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        await context.Response.WriteAsync("Unauthorized");
        return;
    }

    await next();
});

// Middleware to log HTTP method, request path, and response status code
app.Use(async (context, next) =>
{
    // Log request details
    var method = context.Request.Method;
    var path = context.Request.Path;
    Console.WriteLine($"Request: {method} {path}");

    // Call the next middleware in the pipeline
    await next();

    // Log response details
    var statusCode = context.Response.StatusCode;
    Console.WriteLine($"Response: {statusCode}");
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGet("/users", (IUserService userService) =>
{
    return userService.GetAllUsers();
})
.WithName("GetAllUsers")
.WithOpenApi();

app.MapGet("/users/{id}", (IUserService userService, int id) =>
{
    var user = userService.GetUserById(id);
    return user is not null ? Results.Ok(user) : Results.NotFound();
})
.WithName("GetUserById")
.WithOpenApi();

app.MapPost("/users", (IUserService userService, User user) =>
{
    userService.AddUser(user);
    return Results.Created($"/users/{user.Id}", user);
})
.WithName("CreateUser")
.WithOpenApi();

app.MapPut("/users/{id}", (IUserService userService, int id, User updatedUser) =>
{
    var user = userService.UpdateUser(id, updatedUser);
    return user is not null ? Results.Ok(user) : Results.NotFound();
})
.WithName("UpdateUser")
.WithOpenApi();

app.MapDelete("/users/{id}", (IUserService userService, int id) =>
{
    var result = userService.DeleteUser(id);
    return result ? Results.NoContent() : Results.NotFound();
})
.WithName("DeleteUser")
.WithOpenApi();

app.Run();

record User(int Id, string Name, string Email);

interface IUserService
{
    IEnumerable<User> GetAllUsers();
    User? GetUserById(int id);
    void AddUser(User user);
    User? UpdateUser(int id, User updatedUser);
    bool DeleteUser(int id);
}

class UserService : IUserService
{
    private readonly List<User> _users = new();
    private int _nextId = 1;

    public IEnumerable<User> GetAllUsers() => _users;

    public User? GetUserById(int id) => _users.FirstOrDefault(u => u.Id == id);

    public void AddUser(User user)
    {
        user = user with { Id = _nextId++ };
        _users.Add(user);
    }

    public User? UpdateUser(int id, User updatedUser)
    {
        var user = GetUserById(id);
        if (user is null) return null;

        user = user with { Name = updatedUser.Name, Email = updatedUser.Email };
        _users[_users.FindIndex(u => u.Id == id)] = user;
        return user;
    }

    public bool DeleteUser(int id)
    {
        var user = GetUserById(id);
        if (user is null) return false;

        _users.Remove(user);
        return true;
    }
}

class ValidateTokenAttribute
{
    // Token validation method
    public static bool ValidateToken(string token)
    {
        // Implement your token validation logic here
        // For example, you can check if the token matches a predefined value
        return token == "valid-token";
    }
}
