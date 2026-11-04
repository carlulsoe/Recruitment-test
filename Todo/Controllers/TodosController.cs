using Core.Interfaces.Repositories;
using Microsoft.AspNetCore.Mvc;
using Todo.Models;

namespace Todo.Controllers;

[ApiController]
[Route("api/todos")]
public class TodosController(ITodoRepository repository) : ControllerBase
{
    [HttpGet]
    public IEnumerable<TodoResponse> GetAll() =>
        repository.GetAll().Select(TodoResponse.From);

    [HttpGet("{id:guid}")]
    public ActionResult<TodoResponse> GetById(Guid id)
    {
        var todo = repository.GetById(id);
        return todo is null ? NotFound() : TodoResponse.From(todo);
    }

    [HttpPost]
    public ActionResult<TodoResponse> Create(CreateTodoRequest request)
    {
        var todo = repository.Create(new Core.Models.Todo { Title = request.Title.Trim() });
        return CreatedAtAction(nameof(GetById), new { id = todo.Id }, TodoResponse.From(todo));
    }

    [HttpPost("{id:guid}/toggle")]
    public ActionResult<TodoResponse> Toggle(Guid id)
    {
        var todo = repository.ToggleTodo(id);
        return todo is null ? NotFound() : TodoResponse.From(todo);
    }

    [HttpDelete("{id:guid}")]
    public IActionResult Delete(Guid id) =>
        repository.Delete(id) ? NoContent() : NotFound();
}
