using Core.Interfaces.Repositories;
using Infrastructure.Repositories;
using Microsoft.AspNetCore.StaticFiles;

var builder = WebApplication.CreateBuilder(args);

// Without this, a body that cannot be parsed also reports a confusing "request field is required".
builder.Services.AddControllers(options =>
    options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true);
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();
builder.Services.AddResponseCompression();

builder.Services.AddSingleton(TimeProvider.System);

// Singleton because the repository holds the data itself.
builder.Services.AddSingleton<ITodoRepository, TodoRepository>();

var app = builder.Build();

app.UseExceptionHandler();
app.UseResponseCompression();

app.UseDefaultFiles();
app.UseStaticFiles(new StaticFileOptions { OnPrepareResponse = SetCacheHeaders });

app.MapOpenApi();
app.MapHealthChecks("/healthz");
app.MapControllers();

app.Run();

// Vite puts a content hash in every file name under /assets, so those files never change.
// index.html refers to them by name, so it must be checked again each time.
static void SetCacheHeaders(StaticFileResponseContext context) =>
    context.Context.Response.Headers.CacheControl =
        context.Context.Request.Path.StartsWithSegments("/assets")
            ? "public, max-age=31536000, immutable"
            : "no-cache";
