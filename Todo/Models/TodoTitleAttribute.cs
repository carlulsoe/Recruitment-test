using System.ComponentModel.DataAnnotations;
using TodoItem = Core.Models.Todo;

namespace Todo.Models;

/// <summary>Reports the domain's title rule as a validation error, so an invalid title gets a 400.</summary>
public sealed class TodoTitleAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext) =>
        TodoItem.FindTitleProblem(value as string ?? "") is { } problem
            ? new ValidationResult(problem, [validationContext.MemberName!])
            : ValidationResult.Success;
}
