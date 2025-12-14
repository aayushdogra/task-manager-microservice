using FluentValidation;
using TaskManager.Dto;

namespace TaskManager.Validators;

public class UpdateTaskRequestValidator: AbstractValidator<UpdateTaskRequest>
{
    public UpdateTaskRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required")
            .MinimumLength(2)
            .MaximumLength(100);

        RuleFor(x => x.Description)
            .MaximumLength(500);
    }
}