import { useEffect, useState } from 'react';
import { API_BASE, getToken } from '../api/client';

/**
 * Renders an <img> for a private API-proxied blob (e.g. a manual-reading photo).
 * A plain <img src="..."> can't attach an Authorization header, so we fetch the
 * bytes ourselves and hand the browser a local object URL.
 */
export function AuthImage({ path, alt, className }: { path: string; alt: string; className?: string }) {
  const [url, setUrl] = useState<string | null>(null);
  const [error, setError] = useState(false);

  useEffect(() => {
    let cancelled = false;
    let objectUrl: string | null = null;
    setError(false);
    setUrl(null);

    const token = getToken();
    fetch(`${API_BASE}${path}`, { headers: token ? { Authorization: `Bearer ${token}` } : undefined })
      .then((res) => (res.ok ? res.blob() : Promise.reject(new Error(`HTTP ${res.status}`))))
      .then((blob) => {
        if (cancelled) return;
        objectUrl = URL.createObjectURL(blob);
        setUrl(objectUrl);
      })
      .catch(() => { if (!cancelled) setError(true); });

    return () => {
      cancelled = true;
      if (objectUrl) URL.revokeObjectURL(objectUrl);
    };
  }, [path]);

  if (error) return <span className="muted small">Слика није доступна.</span>;
  if (!url) return <span className="muted small">Учитавање слике…</span>;
  return <img src={url} alt={alt} className={className} />;
}
