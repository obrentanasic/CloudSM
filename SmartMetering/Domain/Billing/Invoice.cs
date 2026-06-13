using SmartMetering.Domain.Common;

namespace SmartMetering.Domain.Billing;

/// <summary>Monthly bill for one smart meter.</summary>
public sealed class Invoice : AggregateRoot
{
    private Invoice()
    {
        SerialNumber = string.Empty;
        TextBlobName = string.Empty;
        PdfBlobName = string.Empty;
    }

    private Invoice(
        EntityId consumerId,
        EntityId propertyId,
        EntityId meterId,
        EntityId tariffModelId,
        string serialNumber,
        int year,
        int month,
        decimal highTariffKwh,
        decimal lowTariffKwh,
        decimal greenHighKwh,
        decimal greenLowKwh,
        decimal blueHighKwh,
        decimal blueLowKwh,
        decimal redHighKwh,
        decimal redLowKwh,
        decimal greenAmountRsd,
        decimal blueAmountRsd,
        decimal redAmountRsd,
        decimal fixedAmountRsd,
        decimal totalAmountRsd)
    {
        ConsumerId = consumerId;
        PropertyId = propertyId;
        MeterId = meterId;
        TariffModelId = tariffModelId;
        SerialNumber = serialNumber;
        Year = year;
        Month = month;
        HighTariffKwh = highTariffKwh;
        LowTariffKwh = lowTariffKwh;
        GreenHighKwh = greenHighKwh;
        GreenLowKwh = greenLowKwh;
        BlueHighKwh = blueHighKwh;
        BlueLowKwh = blueLowKwh;
        RedHighKwh = redHighKwh;
        RedLowKwh = redLowKwh;
        GreenAmountRsd = greenAmountRsd;
        BlueAmountRsd = blueAmountRsd;
        RedAmountRsd = redAmountRsd;
        FixedAmountRsd = fixedAmountRsd;
        TotalAmountRsd = totalAmountRsd;
        Status = InvoiceStatus.Unpaid;
        TextBlobName = string.Empty;
        PdfBlobName = string.Empty;
        IssuedAtUtc = DateTime.UtcNow;
    }

    public EntityId ConsumerId { get; private set; }

    public EntityId PropertyId { get; private set; }

    public EntityId MeterId { get; private set; }

    public EntityId TariffModelId { get; private set; }

    public string SerialNumber { get; private set; }

    public int Year { get; private set; }

    public int Month { get; private set; }

    public DateTime IssuedAtUtc { get; private set; }

    public decimal HighTariffKwh { get; private set; }

    public decimal LowTariffKwh { get; private set; }

    public decimal GreenHighKwh { get; private set; }

    public decimal GreenLowKwh { get; private set; }

    public decimal BlueHighKwh { get; private set; }

    public decimal BlueLowKwh { get; private set; }

    public decimal RedHighKwh { get; private set; }

    public decimal RedLowKwh { get; private set; }

    public decimal GreenAmountRsd { get; private set; }

    public decimal BlueAmountRsd { get; private set; }

    public decimal RedAmountRsd { get; private set; }

    public decimal FixedAmountRsd { get; private set; }

    public decimal TotalAmountRsd { get; private set; }

    public InvoiceStatus Status { get; private set; }

    public string TextBlobName { get; private set; }

    public string PdfBlobName { get; private set; }

    public decimal TotalKwh => HighTariffKwh + LowTariffKwh;

    public decimal GreenKwh => GreenHighKwh + GreenLowKwh;

    public decimal BlueKwh => BlueHighKwh + BlueLowKwh;

    public decimal RedKwh => RedHighKwh + RedLowKwh;

    public static Invoice Create(
        EntityId consumerId,
        EntityId propertyId,
        EntityId meterId,
        EntityId tariffModelId,
        string serialNumber,
        int year,
        int month,
        decimal highTariffKwh,
        decimal lowTariffKwh,
        decimal greenHighKwh,
        decimal greenLowKwh,
        decimal blueHighKwh,
        decimal blueLowKwh,
        decimal redHighKwh,
        decimal redLowKwh,
        decimal greenAmountRsd,
        decimal blueAmountRsd,
        decimal redAmountRsd,
        decimal fixedAmountRsd,
        decimal totalAmountRsd) =>
        new(
            consumerId,
            propertyId,
            meterId,
            tariffModelId,
            serialNumber,
            year,
            month,
            highTariffKwh,
            lowTariffKwh,
            greenHighKwh,
            greenLowKwh,
            blueHighKwh,
            blueLowKwh,
            redHighKwh,
            redLowKwh,
            greenAmountRsd,
            blueAmountRsd,
            redAmountRsd,
            fixedAmountRsd,
            totalAmountRsd);

    public void AttachDocuments(string textBlobName, string pdfBlobName)
    {
        TextBlobName = textBlobName;
        PdfBlobName = pdfBlobName;
    }

    public void MarkPaid() => Status = InvoiceStatus.Paid;
}
