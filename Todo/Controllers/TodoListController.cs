using Microsoft.AspNetCore.Mvc;

namespace Todo.Controllers;

public class TodoListController : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Add()
    {
        return View();
    }
}