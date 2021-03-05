using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var app = WebApplication.Create(args);

async Task<List<TodoItem>> GetTodos()
{
    using var db = new TodoDbContext();
    return await db.Todos.ToListAsync();
}

async Task<StatusCodeResult> CreateTodo([FromBody] TodoItem todo)
{
    using var db = new TodoDbContext();
    await db.Todos.AddAsync(todo);
    await db.SaveChangesAsync();

    return new StatusCodeResult(204);
}

async Task<StatusCodeResult> UpdateCompleted([FromRoute] int id, [FromBody] TodoItem inputTodo)
{
    using var db = new TodoDbContext();
    var todo = await db.Todos.FindAsync(id);

    if (todo is null)
    {
        return new StatusCodeResult(404);
    }

    todo.IsComplete = inputTodo.IsComplete;

    await db.SaveChangesAsync();

    return new StatusCodeResult(204);
}

async Task<StatusCodeResult> DeleteTodo([FromRoute] int id)
{
    using var db = new TodoDbContext();
    var todo = await db.Todos.FindAsync(id);

    if (todo is null)
    {
        return new StatusCodeResult(404);
    }

    db.Todos.Remove(todo);
    await db.SaveChangesAsync();

    return new StatusCodeResult(204);
}

app.MapGet("/api/todos", (Func<Task<List<TodoItem>>>)GetTodos);
app.MapPost("/api/todos", (Func<TodoItem, Task<StatusCodeResult>>)CreateTodo);
app.MapPost("/api/todos/{id}", (Func<int, TodoItem, Task<StatusCodeResult>>)UpdateCompleted);
app.MapDelete("/api/todos/{id}", (Func<int, Task<StatusCodeResult>>)DeleteTodo);

await app.RunAsync();