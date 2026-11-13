// Shows the packing and dispatch workstations alongside current pallets.
import type { ReactElement } from 'react';
import { useQuery } from '@tanstack/react-query';
import { getPallets } from './api';
import { PackPalletForm } from './PackPalletForm';
import { DispatchPalletForm } from './DispatchPalletForm';

export function PalletPage(): ReactElement {
  const pallets = useQuery({ queryKey: ['pallets'], queryFn: getPallets });

  return (
    <section className="space-y-5">
      <h1 className="text-2xl font-semibold text-gray-900">Pallets & dispatch</h1>
      <div className="grid items-start gap-4 lg:grid-cols-2">
        <PackPalletForm />
        <DispatchPalletForm />
      </div>
      <h2 className="font-semibold">Pallets awaiting dispatch</h2>
      {pallets.isLoading && <p role="status">Loading pallets…</p>}
      {pallets.error && <p role="alert" className="text-red-700">{pallets.error.message}</p>}
      {pallets.data?.length === 0 && <p className="text-sm text-gray-600">No pallets in the dispatch queue.</p>}
      <ul className="divide-y rounded-xl border bg-white">
        {pallets.data?.map((pallet) => (
          <li className="flex flex-wrap justify-between gap-2 px-4 py-3 text-sm" key={pallet.id}>
            <span className="font-mono">{pallet.code}</span>
            <span>{pallet.batchNumber} · {pallet.quantity} · {pallet.state.replace(/_/g, ' ')}</span>
          </li>
        ))}
      </ul>
    </section>
  );
}
