using SmartMetering.Domain.Common;
using SmartMetering.Domain.Metering;

namespace SmartMetering.Application.Abstractions;

public interface IMeterStatusRepository
{
    Task SaveAsync(MeterStatus status, CancellationToken ct = default);

    Task<MeterStatus?> GetByMeterAsync(EntityId meterId, CancellationToken ct = default);

    Task<IReadOnlyList<MeterStatus>> GetAllAsync(CancellationToken ct = default);
}
