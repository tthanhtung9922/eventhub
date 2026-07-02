using Microsoft.EntityFrameworkCore;

namespace EventHub.Identity.Infrastructure.Persistence;

public sealed class IdentityDbContext(DbContextOptions<IdentityDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        // Cấu hình các thực thể (DbSet) sẽ được thêm ở các bước sau
    }
}
