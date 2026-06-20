export type Role = 'Admin' | 'Consumer' | 'BillingAdmin';

export interface LoginResponse {
  token: string;
  email: string;
  fullName: string;
  role: Role;
}

export interface Property {
  id: string;
  name: string;
  city: string;
  address: string;
  description?: string | null;
  createdAtUtc: string;
}

export const ConnectionType = { SinglePhase: 0, ThreePhase: 1 } as const;

export interface Meter {
  id: string;
  propertyId: string;
  serialNumber: string;
  connectionType: number;
  maxApprovedPowerKw: number;
  note?: string | null;
  pairingStatus: number; // 0 = Unpaired, 1 = Paired
}

export interface TelemetryPoint {
  observationTime: string;
  totalEnergyKwh: number;
  currentLoadKw: number;
  voltage?: number | null;
  tariff: number; // 0 = High (VT), 1 = Low (NT)
}

export interface MeterStatus {
  meterId: string;
  serialNumber: string;
  connectionType: number;
  lastTotalEnergyKwh: number;
  lastLoadKw: number;
  lastVoltage?: number | null;
  currentTariff: number;
  lastHeartbeatUtc: string;
  isOnline: boolean;
}

/** Pushed over SignalR ("ReceiveMeterUpdate"). */
export interface MeterLiveUpdate {
  propertyId: string;
  meterId: string;
  serialNumber: string;
  connectionType: number;
  totalEnergyKwh: number;
  currentLoadKw: number;
  voltage?: number | null;
  tariff: number;
  observationTime: string;
}

export const TariffLabel = (t: number) => (t === 0 ? 'ВТ' : 'НТ');

/** 0 = RSD, 1 = kWh — mirrors Domain.Limits.LimitUnit */
export const LimitUnit = { Rsd: 0, Kwh: 1 } as const;
export const LimitUnitLabel = (u: number) => (u === 0 ? 'РСД' : 'kWh');

export interface LimitDto {
  value: number;
  unit: number;
}

export interface SetLimitRequest {
  value: number;
  unit: number;
}

export interface TariffModel {
  id: string;
  name: string;
  greenLimitKwh: number;
  blueLimitKwh: number;
  greenHighPriceRsd: number;
  greenLowPriceRsd: number;
  blueHighPriceRsd: number;
  blueLowPriceRsd: number;
  redHighPriceRsd: number;
  redLowPriceRsd: number;
  powerPriceRsdPerKw: number;
  supplierFeeRsd: number;
  isActive: boolean;
  createdAtUtc: string;
  activatedAtUtc?: string | null;
}

export interface Invoice {
  id: string;
  propertyId: string;
  meterId: string;
  serialNumber: string;
  year: number;
  month: number;
  issuedAtUtc: string;
  highTariffKwh: number;
  lowTariffKwh: number;
  greenKwh: number;
  blueKwh: number;
  redKwh: number;
  totalKwh: number;
  totalAmountRsd: number;
  status: number;
}

export interface InvoicePage {
  items: Invoice[];
  page: number;
  pageSize: number;
  totalCount: number;
}

export interface TelemetryHistory {
  meterId: string;
  serialNumber: string;
  points: TelemetryPoint[];
}

export interface GeneratedInvoices {
  year: number;
  month: number;
  created: number;
  skipped: number;
}

export interface ManualReadingDto {
  id: string;
  meterId: string;
  serialNumber: string;
  consumerId: string;
  declaredTotalEnergyKwh: number;
  note?: string | null;
  originalImageUrl: string;
  optimizedImageUrl?: string | null;
  status: 'PendingReview' | 'Processed' | 'Rejected';
  submittedAtUtc: string;
  reviewedAtUtc?: string | null;
  reviewNote?: string | null;
}

export const ManualReadingStatusLabel = (s: ManualReadingDto['status']) => {
  switch (s) {
    case 'Processed': return 'Обрађено';
    case 'Rejected': return 'Одбијено';
    default: return 'На чекању';
  }
};

// ── Faza 10: admin network overview ──────────────────────────────
export interface MeterNetworkStatus {
  meterId: string;
  serialNumber: string;
  connectionType: number;
  pairingStatus: number;
  isOnline: boolean;
  lastHeartbeatUtc?: string | null;
  propertyId: string;
  propertyName: string;
  ownerId: string;
  ownerName: string;
  monthConsumptionKwh: number;
  lastInvoiceStatus?: number | null;
  lastInvoiceIssuedAtUtc?: string | null;
}

export interface PaymentRecord {
  invoiceId: string;
  serialNumber: string;
  consumerId: string;
  consumerName: string;
  year: number;
  month: number;
  totalAmountRsd: number;
  issuedAtUtc: string;
  paidAtUtc?: string | null;
}

export interface InvoiceStatistics {
  totalInvoices: number;
  paidInvoices: number;
  unpaidInvoices: number;
  emailsSent: number;
  emailsNotSent: number;
  totalAmountPaidRsd: number;
  totalAmountUnpaidRsd: number;
}

export interface AlertLog {
  id: string;
  type: number;
  severity: number;
  audience: number;
  meterId: string;
  serialNumber: string;
  message: string;
  occurredAtUtc: string;
  emailSent: boolean;
}

export const AlertTypeLabel = (t: number) =>
  (['Пад напона', 'Бројило offline', 'Нагли скок потрошње', 'Прекорачен лимит'][t] ?? 'Упозорење');

export const AlertSeverityLabel = (s: number) => (s === 1 ? 'Критично' : 'Упозорење');
