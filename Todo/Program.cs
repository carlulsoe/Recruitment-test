using Core.Interfaces.Repositories;
using Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// Singleton because the repository holds the data itself.
builder.Services.AddSingleton<ITodoRepository, TodoRepository>();

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapControllers();

app.Run();
