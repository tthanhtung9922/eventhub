using Finmy.Budgeting.Application.Envelopes.Dtos;
using Finmy.Budgeting.Domain.Envelopes;

namespace Finmy.Budgeting.Application.Abstractions;

public interface IEnvelopeRepository
{
    void Add(Envelope envelope);
    void Remove(Envelope envelope);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
    Task<Envelope?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<(IReadOnlyList<Envelope> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken);
    Task<IReadOnlyList<MonthlyCategorySummary>> GetMonthlySummaryAsync(DateTimeOffset monthStartUtc, DateTimeOffset monthEndUtc, CancellationToken cancellationToken);
}
