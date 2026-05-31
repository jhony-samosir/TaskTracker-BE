using FluentValidation;
using TaskTracker.API.DTOs;

namespace TaskTracker.API.Validators;

public sealed class CreateTaskRequestValidator : AbstractValidator<CreateTaskRequest>
{
    public CreateTaskRequestValidator()
    {
        RuleFor(x => x.TaskTitle).NotEmpty().MaximumLength(255);
        RuleFor(x => x.TaskDescription).MaximumLength(4000);
        RuleFor(x => x.AssigneeUserId).GreaterThan(0);
        RuleFor(x => x.ReviewerUserId).GreaterThan(0);
        RuleFor(x => x.DeadlineDate).Must(BeTodayOrFuture).WithMessage("DeadlineDate must be today or in the future.");
        RuleFor(x => x.TaskStatusId).GreaterThan(0);
    }

    private static bool BeTodayOrFuture(DateTime deadlineDate) => deadlineDate.Date >= DateTime.UtcNow.Date;
}

public sealed class UpdateTaskRequestValidator : AbstractValidator<UpdateTaskRequest>
{
    public UpdateTaskRequestValidator()
    {
        RuleFor(x => x.TaskTitle).NotEmpty().MaximumLength(255);
        RuleFor(x => x.TaskDescription).MaximumLength(4000);
        RuleFor(x => x.AssigneeUserId).GreaterThan(0);
        RuleFor(x => x.ReviewerUserId).GreaterThan(0);
        RuleFor(x => x.DeadlineDate).Must(BeTodayOrFuture).WithMessage("DeadlineDate must be today or in the future.");
        RuleFor(x => x.TaskStatusId).GreaterThan(0);
    }

    private static bool BeTodayOrFuture(DateTime deadlineDate) => deadlineDate.Date >= DateTime.UtcNow.Date;
}

public sealed class UpdateTaskStatusRequestValidator : AbstractValidator<UpdateTaskStatusRequest>
{
    public UpdateTaskStatusRequestValidator()
    {
        RuleFor(x => x.TaskStatusId).GreaterThan(0);
        RuleFor(x => x.Remarks).MaximumLength(2000);
    }
}

public sealed class AddCommentRequestValidator : AbstractValidator<AddCommentRequest>
{
    public AddCommentRequestValidator() => RuleFor(x => x.CommentText).NotEmpty().MaximumLength(4000);
}

public sealed class AttachTaskDocumentRequestValidator : AbstractValidator<AttachTaskDocumentRequest>
{
    public AttachTaskDocumentRequestValidator()
    {
        RuleFor(x => x.FileName).NotEmpty().MaximumLength(255);
        RuleFor(x => x.MimeType).NotEmpty().MaximumLength(100);
        RuleFor(x => x.FileBase64).NotEmpty();
        RuleFor(x => x.DocumentType).NotEmpty().MaximumLength(50);
        RuleFor(x => x.DocumentVersion).GreaterThan(0);
        RuleFor(x => x.FileSize).GreaterThanOrEqualTo(0).When(x => x.FileSize.HasValue);
    }
}
