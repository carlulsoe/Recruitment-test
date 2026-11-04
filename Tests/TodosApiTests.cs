using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Todo.Models;

namespace Tests;

/// <summary>Sends real HTTP requests through the whole ASP.NET Core pipeline.</summary>
public class TodosApiTests : IDisposable
{
    private readonly WebApplicationFactory<Program> _factory = new();
    private readonly HttpClient _client;

    public TodosApiTests() => _client = _factory.CreateClient();

    public void Dispose() => _factory.Dispose();

    private async Task<TodoResponse> CreateTodo(string title)
    {
        var response = await _client.PostAsJsonAsync("/api/todos", new { title });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TodoResponse>())!;
    }

    private async Task<List<TodoResponse>> GetTodos() =>
        (await _client.GetFromJsonAsync<List<TodoResponse>>("/api/todos"))!;

    [Fact]
    public async Task List_starts_empty()
    {
        Assert.Empty(await GetTodos());
    }

    [Fact]
    public async Task Create_returns_201_with_location_of_the_new_todo()
    {
        var response = await _client.PostAsJsonAsync("/api/todos", new { title = "  Buy milk  " });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await _client.GetFromJsonAsync<TodoResponse>(response.Headers.Location);
        Assert.Equal("Buy milk", created!.Title);
    }

    [Fact]
    public async Task Created_todo_appears_in_the_list()
    {
        var created = await CreateTodo("Buy milk");

        Assert.Equal([created], await GetTodos());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Create_rejects_blank_title(string title)
    {
        var response = await _client.PostAsJsonAsync("/api/todos", new { title });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(await GetTodos());
    }

    [Fact]
    public async Task Create_rejects_missing_title()
    {
        var response = await _client.PostAsJsonAsync("/api/todos", new { });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_rejects_title_over_the_limit()
    {
        var title = new string('x', CreateTodoRequest.MaxTitleLength + 1);

        var response = await _client.PostAsJsonAsync("/api/todos", new { title });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Toggle_marks_a_todo_complete_and_back()
    {
        var todo = await CreateTodo("Buy milk");

        var completed = await _client.PostAsync($"/api/todos/{todo.Id}/toggle", null);
        var body = await completed.Content.ReadFromJsonAsync<TodoResponse>();
        Assert.True(body!.IsComplete);
        Assert.NotNull(body.CompletedAt);
        Assert.True((await GetTodos()).Single().IsComplete);

        await _client.PostAsync($"/api/todos/{todo.Id}/toggle", null);
        Assert.False((await GetTodos()).Single().IsComplete);
    }

    [Fact]
    public async Task Delete_removes_the_todo()
    {
        var todo = await CreateTodo("Buy milk");

        var response = await _client.DeleteAsync($"/api/todos/{todo.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Empty(await GetTodos());
    }

    [Fact]
    public async Task Unknown_id_returns_404()
    {
        var id = Guid.NewGuid();

        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync($"/api/todos/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.PostAsync($"/api/todos/{id}/toggle", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.DeleteAsync($"/api/todos/{id}")).StatusCode);
    }
}
