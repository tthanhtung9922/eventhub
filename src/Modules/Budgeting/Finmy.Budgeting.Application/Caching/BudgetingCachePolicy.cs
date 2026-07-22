using Microsoft.Extensions.Caching.Hybrid;

namespace Finmy.Budgeting.Application.Caching;

public static class BudgetingCachePolicy
{
    public const string EnvelopeListKeyPrefix = "envelopes:list";
    public static readonly HybridCacheEntryOptions EnvelopeListEntry = new()
    {
        Expiration = TimeSpan.FromMinutes(2),
        LocalCacheExpiration = TimeSpan.FromSeconds(30)
    };

    public const string EnvelopeSummaryKeyPrefix = "envelopes:summary";
    public static readonly HybridCacheEntryOptions MonthlySummaryEntry = new()
    {
        Expiration = TimeSpan.FromMinutes(10),
        LocalCacheExpiration = TimeSpan.FromMinutes(2)
    };
}
