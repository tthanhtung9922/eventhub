using Finmy.Budgeting.Domain.Receipts;

namespace Finmy.Budgeting.Application.Abstractions;

public interface IReceiptRepository
{
    void Add(Receipt receipt);
    void Remove(Receipt receipt);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
    Task<Receipt?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
}
