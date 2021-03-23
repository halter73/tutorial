# Tutorial

**Goal**: In this exercise, the participants will be asked to build the backend of a TodoReact App. The user will be exploring minimal hosting and routing APIs for writing this backend.

**What is the minimal?**: Minimal is new way to build hosting and routing APIs make it **easy** to write web applications.  

## Prerequisites

1. Install [.NET Core 6.0 preview](https://github.com/dotnet/installer/tree/7c91bd82ab5dcc208886fd55f9cfaa0c385dddcb#installers-and-binaries) (TODO: Create friendlier download page.)
1. Install [Node.js](https://nodejs.org/en/) 14 or later.

## Setup

[Download](https://github.com/halter73/tutorial/archive/halter73/mapaction.zip) or clone this repository. Unzip it, and navigate to the Tutorial folder which contains the `TodoReact` frontend application.

   > If using [Visual Studio Code](https://code.visualstudio.com/), install the [C# extension](https://marketplace.visualstudio.com/items?itemName=ms-vscode.csharp) for C# support.


**Please Note: The completed exercise is available in the [samples folder](https://github.com/halter73/tutorial/tree/halter73/mapaction/Sample). Feel free to reference it at any point during the tutorial.**

## Tasks

### Run the frontend application

1. Once you clone the Todo repo, navigate to the `TodoReact` folder inside of the `Tutorial` folder and run the following commands 

    ```
    TodoReact> npm i 
    TodoReact> npm start
    ```

    - The commands above
        - Restores packages `npm i `
        - Starts the react app `npm start`

1. The app will load but have no functionality

    ![image](https://user-images.githubusercontent.com/2546640/75070087-86307c80-54c0-11ea-8012-c78813f1dfd6.png)

    > `Proxy error: Could not proxy request /api/todos from localhost:3000 to http://localhost:5000/` is expected.

    ![proxyerror](https://user-images.githubusercontent.com/2546640/112073462-8929f080-8b4a-11eb-872d-843528103380.gif)

    > Keep this React app running as we'll need it once we build the back-end in the upcoming steps

### Build backend

#### Create a new minimal project

1. Open a new terminal navigate to the `Tutorial` folder.

1. Install the MinimalHost template using the `dotnet CLI`. Copy the command below into a terminal or command prompt to install the template.

    ```
    Tutorial> dotnet new -i MinimalHost.Templates::0.1.*-* --nuget-source https://f.feedz.io/minimal/tutorial/nuget/index.json
    ```

    This will make the `MinimalHost` templates available in the `dotnet new` command.
    ![template](https://user-images.githubusercontent.com/2546640/112074057-c04cd180-8b4b-11eb-8aee-4dc2e0fc344a.gif)



1. Create a new MinimalHost application and add the necessary packages in the `TodoApi` folder.

    ```
    Tutorial> dotnet new minimalhost -n TodoApi
    ```

#### Create the database model

1. Open the `TodoApi` Folder in the editor of your choice.


1. Create a file called  `TodoItem.cs` in the TodoApi folder. Add the content below:

    ```C#
    using System.Text.Json.Serialization;

    public class TodoItem
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("isComplete")]
        public bool IsComplete { get; set; }
    }
    ```

   The above model will be used for reading in JSON and storing todo items into the database.
1. Create a file called `TodoDbContext.cs` with the following contents:

    ```C#
    using Microsoft.EntityFrameworkCore;

    public class TodoDbContext : DbContext
    {
        public DbSet<TodoItem> Todos { get; set; }
        
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseInMemoryDatabase("Todos");
        }
    }
    ```

    This code does 2 things:
     - It exposes a `Todos` property which represents the list of todo items in the database.
     - The call to `UseInMemoryDatabase` wires up the in memory database storage. Data will only be persisted/stored as long as the application is running.

1. Now we're going to use `dotnet watch` to run the server side application:

    ```
    TodoApi> dotnet watch run
    ```

    > This will watch our application for source code changes and will restart the process as a result.

#### Expose the list of todo items

1. Above `await app.RunAsync();`, create a method called `GetTodos` inside of the `Program.cs` file:

    ```C#
   async Task<List<TodoItem>> GetTodos()
   {
       using var db = new TodoDbContext();
       return await db.Todos.ToListAsync();
    }
    await app.RunAsync();
    ```

    This method gets the list of todo items from the database and returns it. Returned values are written as JSON to the HTTP response.

1. Wire up `GetTodos` to the `api/todos` route by calling `MapGet`. This will happening before `await app.RunAsync();`:

    ```C#
    app.MapGet("/api/todos", (Func<Task<List<TodoItem>>>)GetTodos);

    await app.RunAsync();
    ```

1. Navigate to the URL http://localhost:5000/api/todos in the browser. It should return an empty JSON array.

    <img src="https://user-images.githubusercontent.com/2546640/75116317-1a235500-5635-11ea-9a73-e6fc30639865.png" alt="empty json array" style="text-align:center" width =70% />

#### Adding a new todo item

1. In `Program.cs`, create another method called `CreateTodo`:

    ```C#
    async Task<StatusCodeResult> CreateTodo([FromBody] TodoItem todo)
    {
        using var db = new TodoDbContext();
        await db.Todos.AddAsync(todo);
        await db.SaveChangesAsync();

        return new StatusCodeResult(204);
    }
    ```

    The above method reads the `TodoItem` from the incoming HTTP request and adds it to the database. `[FromBody]` indicates the `todo` parameter will be read from the request body as JSON.

    Once the changes are saved, the method responds with the successful `204` HTTP status code and an empty response body.

1. Wire up `CreateTodo` to the `api/todos` route with `MapPost`:

    ```C#
    app.MapGet("/api/todos", (Func<Task<List<TodoItem>>>)GetTodos);
    app.MapPost("/api/todos", (Func<TodoItem, Task<StatusCodeResult>>)CreateTodo);

    await app.RunAsync();
    ```

1. Navigate to the `TodoReact` application which should be running on http://localhost:3000. Now, you will able to add new items. Refresh the TodoApi on http://localhost:5000 the page should show the stored todo items.
![create](https://user-images.githubusercontent.com/2546640/112079312-52f26e00-8b56-11eb-8aa0-ba56c91174f6.gif)

#### Changing the state of todo items

1. In `Program.cs`, create another method called `UpdateCompleted` below `CreateTodo`:

    ```C#
    async Task<StatusCodeResult> UpdateCompleted(
        [FromRoute] int id,
        [FromBody] TodoItem inputTodo)
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
    ```

    `[FromRoute]` indicates the `int id` method parameter will be populated from the route parameter of the same name (the `{id}` in `/api/todos/{id}` below).

    The body of the method uses the id to find the todo item in the database. It then updates it the `TodoItem.IsComplete` property to match the uploaded JSON todo and saves it back to the database.

1. Wire up `UpdateCompleted` to the `api/todos/{id}` route with `MapPost`:

    ```C#
    app.MapGet("/api/todos", (Func<Task<List<TodoItem>>>)GetTodos);
    app.MapPost("/api/todos", (Func<TodoItem, Task<StatusCodeResult>>)CreateTodo);
    app.MapPost("/api/todos/{id}", (Func<int, TodoItem, Task<StatusCodeResult>>)UpdateCompleted);

    await app.RunAsync();
    ```

#### Deleting a todo item

1. In `Program.cs` create another method called `DeleteTodo`:

    ```C#
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
    ```

    The above logic is very similar to `UpdateCompleted` but instead. it removes the todo item from the database after finding it.

1. Wire up `DeleteTodo` to the `/api/todos/{id}` route with `MapDelete`:

    ```C#
    app.MapGet("/api/todos", (Func<Task<List<TodoItem>>>)GetTodos);
    app.MapPost("/api/todos", (Func<TodoItem, Task<StatusCodeResult>>)CreateTodo);
    app.MapPost("/api/todos/{id}", (Func<int, TodoItem, Task<StatusCodeResult>>)UpdateCompleted);
    app.MapDelete("/api/todos/{id}", (Func<int, Task<StatusCodeResult>>)DeleteTodo);

    await app.RunAsync();
    ```

## Test the application

The application should now be fully functional. 
![Complete](https://user-images.githubusercontent.com/2546640/112080532-8d5d0a80-8b58-11eb-9c46-a6cb9c084bb7.gif)
