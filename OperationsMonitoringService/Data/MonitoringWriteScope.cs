using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace OperationsMonitoringService.Data;

// Infrastructure synchronization only. Fever rules remain in the ingestion service.
internal sealed class MonitoringWriteScope(IDbContextTransaction? transaction) : IAsyncDisposable
{
    private bool disposed;
    public static async Task<MonitoringWriteScope> BeginAsync(
        MonitoringDbContext database, Guid cattleId, Guid deviceId, DateTime recordedAtUtc,
        CancellationToken cancellationToken)
    {
        if (!database.Database.IsNpgsql())
            return new MonitoringWriteScope(null);

        var transaction = await database.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            // Serialize the animal AND retry key across processes. Globally sorted
            // lock acquisition also covers conflicting retries for different animals.
            var keys = new[]
            {
                LockKey($"cattle:{cattleId:D}"),
                LockKey($"reading:{deviceId:D}:{recordedAtUtc.Ticks}")
            };
            foreach (var key in keys.Distinct().Order())
                await database.Database.ExecuteSqlInterpolatedAsync(
                    $"SELECT pg_advisory_xact_lock({key})", cancellationToken);
            return new MonitoringWriteScope(transaction);
        }
        catch
        {
            await transaction.DisposeAsync();
            throw;
        }
    }

    public Task CommitAsync(CancellationToken cancellationToken) =>
        transaction?.CommitAsync(cancellationToken) ?? Task.CompletedTask;

    public async Task RollbackAsync(CancellationToken cancellationToken)
    {
        if (transaction is null) return;
        await transaction.RollbackAsync(cancellationToken);
        // Clear the completed transaction from DbContext before any retry lookup.
        await DisposeAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (disposed) return;
        disposed = true;
        if (transaction is not null) await transaction.DisposeAsync();
    }

    private static long LockKey(string value) => BinaryPrimitives.ReadInt64BigEndian(
        SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
