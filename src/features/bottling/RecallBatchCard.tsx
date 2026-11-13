// Shows pallet disposition and lets quality staff record a batch hold.
import { useState, type FormEvent, type ReactElement } from 'react';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { FormField } from '@/components/FormField';
import { setBatchHold, type RecallBatch } from './api';

interface Props {
  batch: RecallBatch;
  canHold: boolean;
}

export function RecallBatchCard({ batch, canHold }: Props): ReactElement {
  const [reason, setReason] = useState('');
  const cache = useQueryClient();
  const hold = useMutation({
    mutationFn: () => setBatchHold(batch.id, !batch.isHeld, reason.trim()),
    onSuccess: async () => {
      await Promise.all(['recall', 'pallets', 'packable-batches', 'shipping'].map(
        (key) => cache.invalidateQueries({ queryKey: [key] })
      ));
      setReason('');
    },
  });

  function submit(event: FormEvent): void {
    event.preventDefault();
    if (reason.trim()) hold.mutate();
  }

  const packed = batch.pallets.reduce((total, pallet) => total + pallet.quantity, 0);
  return (
    <article className="space-y-3 rounded-xl border bg-white p-4">
      <h3 className="font-semibold">{batch.batchNumber}</h3>
      <p className="text-sm text-gray-700">
        {batch.state.replace(/_/g, ' ')} · {batch.isHeld ? 'On quality hold' : 'No active hold'}
        {' · '}Produced {batch.producedQuantity} · Packed {packed}
      </p>
      {batch.legacyClient && <p>Whole batch shipped to {batch.legacyClient}</p>}
      {batch.pallets.length > 0 && (
        <ul className="divide-y rounded-md border">
          {batch.pallets.map((pallet) => (
            <li key={pallet.id} className="flex flex-wrap justify-between gap-2 px-3 py-2 text-sm">
              <span className="font-mono">{pallet.code}</span>
              <span>{pallet.quantity} · {pallet.state.replace(/_/g, ' ')}{pallet.clientName && ` · ${pallet.clientName}`}</span>
            </li>
          ))}
        </ul>
      )}
      {canHold && batch.state !== 'Shipped' && batch.state !== 'Destroyed' && (
        <form onSubmit={submit} className="flex flex-wrap items-end gap-3">
          <FormField id={`hold-reason-${batch.id}`} label="Quality decision reason" value={reason} maxLength={500}
            onChange={(event) => setReason(event.target.value)} required />
          <button className="min-h-11 rounded-md border border-gray-400 px-4 text-sm font-medium" disabled={hold.isPending}>
            {hold.isPending ? 'Saving…' : batch.isHeld ? 'Release hold' : 'Place quality hold'}
          </button>
        </form>
      )}
      {hold.error && <p role="alert" className="text-sm text-red-700">{hold.error.message}</p>}
    </article>
  );
}
