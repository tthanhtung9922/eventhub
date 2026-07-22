using FluentValidation;

namespace Finmy.Budgeting.Application.Envelopes.Dtos;

public sealed class MonthlySummaryRequestValidator : AbstractValidator<MonthlySummaryRequest>
{
    public MonthlySummaryRequestValidator()
    {
        RuleFor(x => x.Year).InclusiveBetween(2000, 2100);
        RuleFor(x => x.Month).InclusiveBetween(1, 12);
    }
}
