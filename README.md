# Tutorial

**Goal**: In this exercise, the participants will be asked to build the backend of a TodoReact App.  The user will be exploring the functionality of Houdini, a server-side framework.

**What is Houdini**: Houdini makes it **easy** to write web applications.  

**Why Houdini**: Houdini is lightweight server-side framework designed to scale-up as your application grows in complexity. 

## Prerequisites

1. Install [.NET Core 6.0 preview](https://github.com/dotnet/installer/tree/7c91bd82ab5dcc208886fd55f9cfaa0c385dddcb#installers-and-binaries) (TODO: Create friendlier download page.)
1. Install [Node.js](https://nodejs.org/en/) 14 or later.

## Setup

Download this [repository](https://github.com/halter73/tutorial/archive/halter73/mapaction.zip). Unzip it, and navigate to the Tutorial folder which contains the `TodoReact` frontend application.

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

    > Keep this React app running as we'll need it once we build the back-end in the upcoming steps

### Build backend - Houdini

#### Create a new project

1. Open a new shell inside of the `Tutorial` folder.

1. Install the FeatherHttp template using the `dotnet CLI`. Copy the command below into a terminal or command prompt to install the template.

    ```
    Tutorial> dotnet new -i FeatherHttp.Templates::0.1.*-* --nuget-source https://f.feedz.io/featherhttp/framework/nuget/index.json
    ```

    This will make the `FeatherHttp` templates available in the `dotnet new` command.


1. Create a new FeatherHttp application and add the necessary packages in the `TodoApi` folder.

    ```
    Tutorial> dotnet new feather -n TodoApi
    ```

1. Open the `TodoApi` Folder in the editor of your choice.

1. Open `TodoApi.csproj` in the editor and update `<TargetFramework>net5.0</TargetFramework>` to `<TargetFramework>net6.0</TargetFramework>`.

1. Also add `https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet6/nuget/v3/index.json;` to the `RestoreSources` section of `TodoApi.csproj`.

    ```xml
    <RestoreSources>
        $(RestoreSources);
        https://api.nuget.org/v3/index.json;
        https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet6/nuget/v3/index.json;
        https://f.feedz.io/featherhttp/framework/nuget/index.json;
    </RestoreSources>
    ```

1. Add a preview NuGet package (`Microsoft.EntityFrameworkCore.InMemory`) required for the next section.

    ```
    Tutorial> cd TodoApi
    TodoApi> dotnet add package Microsoft.EntityFrameworkCore.InMemory -v 6.0.*-*
    ```

#### Create the database model

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
     - The call to `UseInMemoryDatabase` wires up the in memory database storage. Data will only be persisted as long as the application is running.

1. Now we're going to use `dotnet watch` to run the server side application:

    ```
    TodoApi> dotnet watch run
    ```

    > This will watch our application for source code changes and will restart the process as a result.

#### Expose the list of todo items

1. Add the appropriate `usings` to the top of the `Program.cs` file.

    ```C#
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;
    ```

    This will import the required namespaces so that the application compiles successfully.

1. Above `await app.RunAsync();`, create a top-level method called `GetTodos` inside of the `Program.cs` file:

    ```C#
    async Task<List<TodoItem>> GetTodos()
    {
        using var db = new TodoDbContext();
        return await db.Todos.ToListAsync();
    }
    ```

    This method gets the list of todo items from the database and returns it. Returned values are written as JSON to the HTTP response.
    
1. Wire up `GetTodos` to the `api/todos` route by calling `MapGet` before the existing call to `await app.RunAsync();`:

    ```C#
    app.MapGet("/api/todos", (Func<Task<List<TodoItem>>>)GetTodos);

    await app.RunAsync();
    ```

1. Navigate to the URL http://localhost:5000/api/todos in the browser. It should return an empty JSON array.

    <img src="https://user-images.githubusercontent.com/2546640/75116317-1a235500-5635-11ea-9a73-e6fc30639865.png" alt="empty json array" style="text-align:center" width =70% />

#### Adding a new todo item

1. In `Program.cs`, create another top-level method called `CreateTodo`:

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
    app.MapPost("/api/todos", (Func<TodoItem, Task<StatusCodeResult>>)CreateTodo);;

    await app.RunAsync();
    ```

1. Navigate to the `TodoReact` application which should be running on http://localhost:3000. The application should be able to add new todo items. Also, refreshing the page should show the stored todo items.
![image](https://user-images.githubusercontent.com/2546640/75119637-bc056a80-5652-11ea-81c8-71ea13d97a3c.png)

#### Changing the state of todo items
1. In `Program.cs`, create another local method called `UpdateCompleted` below `CreateTodo`:
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

1. In `Program.cs` create another local method called `DeleteTodo`:

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
![image](https://user-images.githubusercontent.com/2546640/75119891-08ea4080-5655-11ea-96be-adab4990ad65.png)
