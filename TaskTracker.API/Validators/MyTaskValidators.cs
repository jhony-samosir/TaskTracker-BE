using FluentValidation;
using TaskTracker.API.DTOs;

namespace TaskTracker.API.Validators;

public sealed class UpdateMyTaskStatusRequestValidator : AbstractValidator<UpdateMyTaskStatusRequest>
{
    public UpdateMyTaskStatusRequestValidator()
    {
        RuleFor(x => x.TaskStatusId).GreaterThan(0);
        RuleFor(x => x.Remarks).MaximumLength(2000);
    }
}

public sealed class AddMyTaskCommentRequestValidator : AbstractValidator<AddMyTaskCommentRequest>
{
    public AddMyTaskCommentRequestValidator()
    {
        RuleFor(x => x.CommentText).NotEmpty().MaximumLength(4000);
    }
}
