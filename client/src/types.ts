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
