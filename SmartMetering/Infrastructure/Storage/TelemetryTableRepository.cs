using Azure.Data.Tables;
using SmartMetering.Application.Abstractions;
using SmartMetering.Domain.Common;
using SmartMetering.Domain.Metering;
using SmartMetering.Infrastructure.Storage.Entities;
using SmartMetering.Infrastructure.Storage.Mappers;

namespace SmartMetering.Infrastructure.Storage;

public sealed class TelemetryTableRepository : ITelemetryRepository
{
    private readonly TableClient _table;

    public TelemetryTableRepository(StorageOptions options)
    {
        _table = new TableClient(options.ConnectionString, StorageOptions.TelemetriesTable);
        _table.CreateIfNotExists();
    }

    public async Task SaveAsync(Telemetry telemetry, CancellationToken ct = default)
    {
        var entity = TelemetryMapper.ToEntity(telemetry);
        await _table.AddEntityAsync(entity, ct);
    }

    public async Task<IReadOnlyList<Telemetry>> GetRecentAsync(EntityId meterId, int take, CancellationToken ct = default)
    {
        var results = new List<Telemetry>();
        var partition = meterId.ToString();

        // RowKey is reverse-ticks-prefixed, so the partition is already newest-first.
        var query = _table.QueryAsync<TelemetryEntity>(e => e.PartitionKey == partition, maxPerPage: take, cancellationToken: ct);

        await foreach (var entity in query)
        {
            results.Add(TelemetryMapper.ToDomain(entity));
            if (results.Count >= take)
            {
                break;
            }
        }

        return results;
    }

    public async Task<IReadOnlyList<Telemetry>> GetForPeriodAsync(EntityId meterId, DateTime fromUtc, DateTime toUtc, CancellationToken ct = default)
    {
        var results = new List<Telemetry>();
        var partition = meterId.ToString();
        var from = new DateTimeOffset(DateTime.SpecifyKind(fromUtc, DateTimeKind.Utc));
        var to = new DateTimeOffset(DateTime.SpecifyKind(toUtc, DateTimeKind.Utc));

        var query = _table.QueryAsync<TelemetryEntity>(
            e => e.PartitionKey == partition && e.ObservationTime >= from && e.ObservationTime < to,
            cancellationToken: ct);

        await foreach (var entity in query)
        {
            results.Add(TelemetryMapper.ToDomain(entity));
        }

        return results.OrderBy(t => t.ObservationTime).ToList();
    }

    public async Task<Telemetry?> GetPreviousBeforeAsync(EntityId meterId, DateTime beforeUtc, CancellationToken ct = default)
    {
        var partition = meterId.ToString();
        var before = new DateTimeOffset(DateTime.SpecifyKind(beforeUtc, DateTimeKind.Utc));

        var query = _table.QueryAsync<TelemetryEntity>(
            e => e.PartitionKey == partition && e.ObservationTime < before,
            maxPerPage: 1,
            cancellationToken: ct);

        await foreach (var entity in query)
        {
            return TelemetryMapper.ToDomain(entity);
        }

        return null;
    }
}
