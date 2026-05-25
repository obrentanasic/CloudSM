using Azure;
using Azure.Data.Tables;
using SmartMetering.Application.Abstractions;
using SmartMetering.Domain.Common;
using SmartMetering.Domain.Metering;
using SmartMetering.Infrastructure.Storage.Entities;
using SmartMetering.Infrastructure.Storage.Mappers;

namespace SmartMetering.Infrastructure.Storage;

public sealed class MeterStatusTableRepository : IMeterStatusRepository
{
    private readonly TableClient _table;

    public MeterStatusTableRepository(StorageOptions options)
    {
        _table = new TableClient(options.ConnectionString, StorageOptions.MeterStatusesTable);
        _table.CreateIfNotExists();
    }

    public async Task SaveAsync(MeterStatus status, CancellationToken ct = default)
    {
        var entity = MeterStatusMapper.ToEntity(status);
        // Overwrite the snapshot each cycle.
        await _table.UpsertEntityAsync(entity, TableUpdateMode.Replace, ct);
    }

    public async Task<MeterStatus?> GetByMeterAsync(EntityId meterId, CancellationToken ct = default)
    {
        try
        {
            var response = await _table.GetEntityAsync<MeterStatusEntity>(
                MeterStatusEntity.Partition, meterId.ToString(), cancellationToken: ct);
            return MeterStatusMapper.ToDomain(response.Value);
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
    }

    public async Task<IReadOnlyList<MeterStatus>> GetAllAsync(CancellationToken ct = default)
    {
        var results = new List<MeterStatus>();
        var query = _table.QueryAsync<MeterStatusEntity>(
            e => e.PartitionKey == MeterStatusEntity.Partition, cancellationToken: ct);

        await foreach (var entity in query)
        {
            results.Add(MeterStatusMapper.ToDomain(entity));
        }

        return results;
    }
}
