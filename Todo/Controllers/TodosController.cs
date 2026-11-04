using Core.Interfaces.Repositories;
using Microsoft.AspNetCore.Mvc;
using Todo.Models;
using TodoItem = Core.Models.Todo;

namespace Todo.Controllers;

[ApiController]
[Route("api/todos")]
public class TodosController(ITodoRepository repository, TimeProvider clock) : ControllerBase
{
    [HttpGet]
    public IEnumerable<TodoResponse> GetAll() =>
        repository.GetAll().Select(TodoResponse.From);

    [HttpGet("{id}")]
    public ActionResult<TodoResponse> GetById(Guid id)
    {
        var todo = repository.GetById(id);
        return todo is null ? NotFound() : TodoResponse.From(todo);
    }

    [HttpPost]
    public ActionResult<TodoResponse> Create(CreateTodoRequest request)
    {
        var todo = repository.Create(TodoItem.Create(request.Title, clock.GetUtcNow()));
        return CreatedAtAction(nameof(GetById), new { id = todo.Id }, TodoResponse.From(todo));
    }

    [HttpPut("{id}/completion")]
    public ActionResult<TodoResponse> SetCompletion(Guid id, SetCompletionRequest request)
    {
        // [Required] has already rejected a request without a value.
        var todo = repository.SetComplete(id, request.IsComplete!.Value, clock.GetUtcNow());
        return todo is null ? NotFound() : TodoResponse.From(todo);
    }

    [HttpDelete("{id}")]
    public IActionResult Delete(Guid id) =>
        repository.Delete(id) ? NoContent() : NotFound();
}
