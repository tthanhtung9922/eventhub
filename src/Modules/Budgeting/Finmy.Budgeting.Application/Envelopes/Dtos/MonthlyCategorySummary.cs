using System.ComponentModel;

namespace Finmy.Budgeting.Application.Envelopes.Dtos;

[ImmutableObject(true)]
public sealed record MonthlyCategorySummary(Guid CategoryId, string CategoryName, decimal TotalAllocated, int EnvelopeCount);