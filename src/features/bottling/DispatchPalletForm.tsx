// Collects pallet scans and confirms dispatch as a single operation.
import { useRef, useState, type FormEvent, type ReactElement } from 'react';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { FormField } from '@/components/FormField';
import { dispatchPallets } from './api';

export function DispatchPalletForm(): ReactElement {
  const [clientName, setClientName] = useState('');
  const [truckPlates, setTruckPlates] = useState('');
  const [scan, setScan] = useState('');
  const [codes, setCodes] = useState<string[]>([]);
  const [scanError, setScanError] = useState('');
  const lastRequest = useRef({ key: '', id: '' });
  const cache = useQueryClient();
  const dispatch = useMutation({
    mutationFn: dispatchPallets,
    onSuccess: async () => {
      setCodes([]);
      await Promise.all(['pallets', 'packable-batches', 'batches', 'recall', 'reports'].map(
        (key) => cache.invalidateQueries({ queryKey: [key] })
      ));
    },
  });

  function addCode(): void {
    const code = scan.trim().toUpperCase();
    if (!/^[A-Z0-9-]{1,50}$/.test(code) || codes.includes(code) || codes.length === 100) {
      setScanError('Enter a unique pallet barcode. Maximum 100 pallets per dispatch.');
      return;
    }
    setScanError('');
    setCodes([...codes, code]);
    setScan('');
  }

  function submit(event: FormEvent): void {
    event.preventDefault();
    if (!codes.length || scan.trim() || !clientName.trim() || !truckPlates.trim()) return;
    const payload = { clientName: clientName.trim(), truckPlates: truckPlates.trim(), palletCodes: codes };
    const key = JSON.stringify(payload);
    if (key !== lastRequest.current.key) lastRequest.current = { key, id: crypto.randomUUID() };
    dispatch.mutate({ ...payload, requestId: lastRequest.current.id });
  }

  return (
    <form onSubmit={submit} className="space-y-3 rounded-xl border bg-white p-4">
      <h2 className="font-semibold">Confirm dispatch</h2>
      <fieldset disabled={dispatch.isPending} className="space-y-3">
        <FormField label="Customer" value={clientName} maxLength={100} required
          onChange={(event) => setClientName(event.target.value)} />
        <FormField label="Truck plate" value={truckPlates} maxLength={50} required
          onChange={(event) => setTruckPlates(event.target.value)} />
        <div className="flex flex-wrap items-end gap-2">
          <FormField label="Scan pallet" value={scan} maxLength={50} onChange={(event) => setScan(event.target.value)}
            onKeyDown={(event) => { if (event.key === 'Enter') { event.preventDefault(); addCode(); } }} />
          <button type="button" onClick={addCode} className="min-h-11 rounded-md border px-3">Add pallet</button>
        </div>
        {scanError && <p role="alert" className="text-red-700">{scanError}</p>}
        <ul className="flex flex-wrap gap-2">
          {codes.map((code) => (
            <li key={code}>
              <button type="button" aria-label={`Remove pallet ${code}`} className="min-h-11 rounded border px-2 font-mono"
                onClick={() => setCodes(codes.filter((item) => item !== code))}>{code} ×</button>
            </li>
          ))}
        </ul>
        <button className="min-h-11 rounded-md bg-brand-600 px-4 text-white disabled:opacity-60"
          disabled={!codes.length || Boolean(scan.trim()) || dispatch.isPending}>
          {dispatch.isPending ? 'Confirming…' : `Dispatch ${codes.length} pallet(s)`}
        </button>
      </fieldset>
      {dispatch.data && <p role="status">{dispatch.data.length} pallet(s) dispatched.</p>}
      {dispatch.error && <p role="alert" className="text-red-700">{dispatch.error.message}</p>}
    </form>
  );
}
