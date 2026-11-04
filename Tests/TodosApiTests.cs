using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Core.Interfaces.Repositories;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Todo.Models;
using TodoItem = Core.Models.Todo;

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

    private Task<HttpResponseMessage> SetCompletion(Guid id, bool isComplete) =>
        _client.PutAsJsonAsync($"/api/todos/{id}/completion", new { isComplete });

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
    [InlineData("​")]
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
        var title = new string('x', TodoItem.MaxTitleLength + 1);

        var response = await _client.PostAsJsonAsync("/api/todos", new { title });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_measures_the_title_after_trimming()
    {
        var title = new string('x', TodoItem.MaxTitleLength) + "          ";

        var response = await _client.PostAsJsonAsync("/api/todos", new { title });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Create_rejects_null_title()
    {
        var response = await _client.PostAsJsonAsync("/api/todos", new { title = (string?)null });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Completion_marks_a_todo_complete_and_back()
    {
        var todo = await CreateTodo("Buy milk");

        var completed = await SetCompletion(todo.Id, true);
        var body = await completed.Content.ReadFromJsonAsync<TodoResponse>();
        Assert.True(body!.IsComplete);
        Assert.NotNull(body.CompletedAt);
        Assert.True((await GetTodos()).Single().IsComplete);

        await SetCompletion(todo.Id, false);
        Assert.False((await GetTodos()).Single().IsComplete);
    }

    [Fact]
    public async Task Completion_can_safely_be_repeated()
    {
        var todo = await CreateTodo("Buy milk");

        var first = await (await SetCompletion(todo.Id, true)).Content.ReadFromJsonAsync<TodoResponse>();
        var second = await (await SetCompletion(todo.Id, true)).Content.ReadFromJsonAsync<TodoResponse>();

        Assert.True(second!.IsComplete);
        Assert.Equal(first!.CompletedAt, second.CompletedAt);
    }

    [Fact]
    public async Task Completion_rejects_a_request_without_the_state()
    {
        var todo = await CreateTodo("Buy milk");

        var response = await _client.PutAsJsonAsync($"/api/todos/{todo.Id}/completion", new { });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
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
        Assert.Equal(HttpStatusCode.NotFound, (await SetCompletion(id, true)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.DeleteAsync($"/api/todos/{id}")).StatusCode);
    }

    [Fact]
    public async Task Malformed_id_returns_400()
    {
        var completion = await _client.PutAsJsonAsync("/api/todos/abc/completion", new { isComplete = true });

        Assert.Equal(HttpStatusCode.BadRequest, (await _client.GetAsync("/api/todos/abc")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, completion.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.DeleteAsync("/api/todos/abc")).StatusCode);
    }

    [Theory]
    [InlineData("{\"isComplete\":null}", "isComplete")]
    [InlineData("{\"isComplete\":\"true\"}", "$.isComplete")]
    [InlineData("{", "$")]
    public async Task An_invalid_body_reports_only_what_is_wrong_with_it(string body, string field)
    {
        var todo = await CreateTodo("Buy milk");
        var content = new StringContent(body, Encoding.UTF8, "application/json");

        var response = await _client.PutAsync($"/api/todos/{todo.Id}/completion", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var errors = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errors");
        var reported = errors.EnumerateObject().Select(error => error.Name).ToList();
        Assert.Equal([field], reported, StringComparer.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("null")]
    [InlineData("")]
    public async Task A_request_without_a_body_returns_400(string body)
    {
        var content = new StringContent(body, Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("/api/todos", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task An_unexpected_failure_returns_problem_details()
    {
        var client = _factory
            .WithWebHostBuilder(host => host.ConfigureTestServices(services =>
                services.AddSingleton<ITodoRepository, BrokenRepository>()))
            .CreateClient();

        var response = await client.GetAsync("/api/todos");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.DoesNotContain("Storage is broken", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Publishes_an_OpenAPI_document_with_the_endpoints()
    {
        var document = await _client.GetFromJsonAsync<JsonElement>("/openapi/v1.json");

        var paths = document.GetProperty("paths").EnumerateObject().Select(path => path.Name).ToList();
        Assert.Contains("/api/todos", paths);
        Assert.Contains("/api/todos/{id}/completion", paths);
    }

    [Fact]
    public async Task Reports_health()
    {
        var response = await _client.GetAsync("/healthz");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }

    private sealed class BrokenRepository : ITodoRepository
    {
        public List<TodoItem> GetAll() => throw new InvalidOperationException("Storage is broken");
        public TodoItem? GetById(Guid id) => throw new InvalidOperationException("Storage is broken");
        public TodoItem Create(TodoItem entity) => throw new InvalidOperationException("Storage is broken");
        public bool Delete(Guid id) => throw new InvalidOperationException("Storage is broken");
        public TodoItem? SetComplete(Guid id, bool isComplete, DateTimeOffset now) =>
            throw new InvalidOperationException("Storage is broken");
    }
}
