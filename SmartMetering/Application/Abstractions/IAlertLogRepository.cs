using SmartMetering.Domain.Alerts;

namespace SmartMetering.Application.Abstractions;

public interface IAlertLogRepository
{
    Task SaveAsync(AlertLogEntry entry, CancellationToken ct = default);

    /// <summary>Most recent alerts first, capped at <paramref name="take"/>.</summary>
    Task<IReadOnlyList<AlertLogEntry>> GetRecentAsync(int take, CancellationToken ct = default);
}
