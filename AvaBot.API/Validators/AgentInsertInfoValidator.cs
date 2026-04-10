using FluentValidation;
using AvaBot.DTO;

namespace AvaBot.API.Validators;

public class AgentInsertInfoValidator : AbstractValidator<AgentInsertInfo>
{
    public AgentInsertInfoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Nome e obrigatorio")
            .MaximumLength(260).WithMessage("Nome deve ter no maximo 260 caracteres");

        RuleFor(x => x.SystemPrompt)
            .NotEmpty().WithMessage("Prompt de sistema e obrigatorio");
    }
}
