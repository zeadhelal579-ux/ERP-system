// Traces a material lot to affected batches, pallets and customers.
import { useState, type FormEvent, type ReactElement } from 'react';
import { useQuery } from '@tanstack/react-query';
import { FormField } from '@/components/FormField';
import { usePermissions } from '@/hooks/usePermissions';
import { getRecall } from './api';
import { RecallBatchCard } from './RecallBatchCard';

export function RecallPage(): ReactElement {
  const [barcode, setBarcode] = useState('');
  const [lotCode, setLotCode] = useState('');
  const { hasRole } = usePermissions();
  const recall = useQuery({
    queryKey: ['recall', lotCode],
    queryFn: () => getRecall(lotCode),
    enabled: lotCode.length > 0,
    retry: false,
  });

  function search(event: FormEvent): void {
    event.preventDefault();
    const code = barcode.trim().toUpperCase();
    if (!code) return;
    if (lotCode === code) void recall.refetch();
    else setLotCode(code);
  }

  return (
    <section className="space-y-5">
      <div>
        <h1 className="text-2xl font-semibold text-gray-900">Lot recall</h1>
        <p className="mt-1 text-sm text-gray-600">Find every recorded batch and destination affected by a material lot.</p>
      </div>
      <form onSubmit={search} className="flex flex-wrap items-end gap-3 rounded-xl border bg-white p-4">
        <FormField label="Material lot barcode" value={barcode} maxLength={50}
          onChange={(event) => setBarcode(event.target.value)} dir="ltr" required />
        <button className="min-h-11 rounded-md bg-brand-600 px-4 text-white" disabled={recall.isFetching}>
          {recall.isFetching ? 'Searching…' : 'Trace lot'}
        </button>
      </form>
      {recall.error && <p role="alert" className="text-red-700">{recall.error.message}</p>}
      {recall.data && !recall.error && (
        <>
          <header className="rounded-xl border bg-white p-4">
            <h2 className="font-semibold">{recall.data.materialName}</h2>
            <p className="break-all font-mono text-sm">{recall.data.lotCode}</p>
            <p className="mt-2 text-sm text-gray-600">
              {recall.data.supplierName} · Supplier lot {recall.data.supplierLot ?? '—'} · Stock {recall.data.stockQuantity} {recall.data.unit}
            </p>
          </header>
          {recall.data.batches.length === 0 && <p>No production batches recorded for this lot.</p>}
          {recall.data.batches.map((batch) => (
            <RecallBatchCard key={batch.id} batch={batch} canHold={hasRole('QualityManager')} />
          ))}
        </>
      )}
    </section>
  );
}
