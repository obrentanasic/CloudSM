using Azure.Data.Tables;
using SmartMetering.Application.Abstractions;
using SmartMetering.Domain.Alerts;
using SmartMetering.Infrastructure.Storage.Entities;
using SmartMetering.Infrastructure.Storage.Mappers;

namespace SmartMetering.Infrastructure.Storage;

public sealed class AlertLogTableRepository : IAlertLogRepository
{
    private readonly TableClient _table;

    public AlertLogTableRepository(StorageOptions options)
    {
        _table = new TableClient(options.ConnectionString, StorageOptions.AlertLogsTable);
        _table.CreateIfNotExists();
    }

    public async Task SaveAsync(AlertLogEntry entry, CancellationToken ct = default)
    {
        var entity = AlertLogMapper.ToEntity(entry);
        await _table.AddEntityAsync(entity, ct);
    }

    public async Task<IReadOnlyList<AlertLogEntry>> GetRecentAsync(int take, CancellationToken ct = default)
    {
        var results = new List<AlertLogEntry>();
        // RowKey is reverse-chronological (see AlertLogEntity.BuildRowKey), so ascending order is newest-first.
        var query = _table.QueryAsync<AlertLogEntity>(
            e => e.PartitionKey == AlertLogEntity.Partition, maxPerPage: Math.Clamp(take, 1, 1000), cancellationToken: ct);

        await foreach (var entity in query)
        {
            results.Add(AlertLogMapper.ToDomain(entity));
            if (results.Count >= take)
            {
                break;
            }
        }

        return results;
    }
}
