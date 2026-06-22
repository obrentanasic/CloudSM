import { useEffect, useRef } from 'react';
import * as signalR from '@microsoft/signalr';
import { API_BASE, getToken } from '../api/client';
import type { MeterLiveUpdate } from '../types';

/**
 * Connects to the telemetry hub and subscribes to one property group at a time.
 * On propertyId change it leaves the previous group and joins the new one
 * (matches the spec's tab-switch requirement).
 */
export function useTelemetryHub(
  propertyId: string | null,
  onUpdate: (update: MeterLiveUpdate) => void,
): void {
  const onUpdateRef = useRef(onUpdate);
  onUpdateRef.current = onUpdate;

  useEffect(() => {
    if (!propertyId) {
      return;
    }

    const connection = new signalR.HubConnectionBuilder()
      .withUrl(`${API_BASE}/hubs/telemetry`, {
        accessTokenFactory: () => getToken() ?? '',
      })
      .withAutomaticReconnect()
      .build();

    connection.on('ReceiveMeterUpdate', (update: MeterLiveUpdate) => onUpdateRef.current(update));

    let cancelled = false;
    const joinProperty = () => connection.invoke('JoinPropertyGroup', propertyId);

    connection.onreconnected(() => {
      if (!cancelled) {
        void joinProperty().catch((err) => console.error('SignalR group rejoin error:', err));
      }
    });

    connection
      .start()
      .then(() => (cancelled ? undefined : joinProperty()))
      .catch((err) => console.error('SignalR connection error:', err));

    return () => {
      cancelled = true;
      connection
        .invoke('LeavePropertyGroup', propertyId)
        .catch(() => undefined)
        .finally(() => connection.stop());
    };
  }, [propertyId]);
}
