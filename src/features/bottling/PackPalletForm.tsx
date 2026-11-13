// Records a barcode and quantity against a batch cleared by quality.
import { useState, type FormEvent, type ReactElement } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { FormField, FormSelect } from '@/components/FormField';
import { getPackableBatches, packPallet } from './api';

export function PackPalletForm(): ReactElement {
  const [batchId, setBatchId] = useState('');
  const [code, setCode] = useState('');
  const [quantity, setQuantity] = useState('');
  const cache = useQueryClient();
  const batches = useQuery({ queryKey: ['packable-batches'], queryFn: getPackableBatches });
  const pack = useMutation({
    mutationFn: () => packPallet(Number(batchId), code, Number(quantity)),
    onSuccess: async () => {
      setCode('');
      setQuantity('');
      await cache.invalidateQueries({ queryKey: ['pallets'] });
    },
  });

  function submit(event: FormEvent): void {
    event.preventDefault();
    if (batchId && code.trim()) pack.mutate();
  }

  return (
    <form onSubmit={submit} className="space-y-3 rounded-xl border bg-white p-4">
      <h2 className="font-semibold">Pack a pallet</h2>
      <FormSelect label="Released batch" value={batchId} onChange={setBatchId} options={[
        { value: '', label: batches.isLoading ? 'Loading batches…' : 'Choose a batch' },
        ...(batches.data ?? []).map((batch) => ({ value: String(batch.id), label: batch.batchNumber })),
      ]} />
      {batches.error && <p role="alert" className="text-red-700">{batches.error.message}</p>}
      <FormField label="New pallet barcode" value={code} maxLength={50}
        onChange={(event) => setCode(event.target.value)} required />
      <FormField label="Pallet quantity" type="number" min="0.01" step="0.01" value={quantity}
        onChange={(event) => setQuantity(event.target.value)} required />
      <button className="min-h-11 rounded-md bg-brand-600 px-4 text-white disabled:opacity-60"
        disabled={!batchId || pack.isPending}>{pack.isPending ? 'Saving…' : 'Record pallet'}</button>
      {pack.data && <p role="status">Pallet {pack.data.code} recorded.</p>}
      {pack.error && <p role="alert" className="text-red-700">{pack.error.message}</p>}
    </form>
  );
}
