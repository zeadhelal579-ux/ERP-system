// Starts a batch from issued material lots and a catalog product.
import { useRef, useState, type FormEvent, type ReactElement } from 'react';
import { useQuery } from '@tanstack/react-query';
import { FormSelect } from '@/components/FormField';
import { getIssues, getProducts, type MaterialIssue } from '@/features/bottling/api';
import { useStartBatch } from '../hooks';

function issueLabel(issue: MaterialIssue): string {
  return `${issue.materialName} · ${issue.lotCode ?? 'Legacy lot'} · ${issue.quantity} ${issue.unit}`;
}

export function StartBatchModal({ onClose }: { onClose: () => void }): ReactElement {
  const products = useQuery({ queryKey: ['bottling-products'], queryFn: getProducts });
  const issues = useQuery({ queryKey: ['available-issues'], queryFn: getIssues });
  const startBatch = useStartBatch();
  const [productId, setProductId] = useState('');
  const [primaryIssueId, setPrimaryIssueId] = useState('');
  const [additional, setAdditional] = useState<number[]>([]);
  const request = useRef({ key: '', id: '' });
  const primary = issues.data?.find((issue) => issue.id === Number(primaryIssueId));

  async function submit(event: FormEvent): Promise<void> {
    event.preventDefault();
    if (!primary || !productId || startBatch.isPending) return;
    const payload = {
      productId: Number(productId),
      rawMaterialId: primary.materialId,
      issuedMaterialQty: primary.quantity,
      primaryIssueId: primary.id,
      additionalIssueIds: additional.filter((id) => id !== primary.id),
    };
    const key = JSON.stringify(payload);
    if (request.current.key !== key) request.current = { key, id: crypto.randomUUID() };
    try {
      await startBatch.mutateAsync({ ...payload, requestId: request.current.id });
      onClose();
    } catch {
      // The mutation exposes the server error without closing the form.
    }
  }

  const error = startBatch.error ?? issues.error ?? products.error;
  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 p-4">
      <form aria-labelledby="start-batch-heading" role="dialog" aria-modal="true" onSubmit={submit}
        className="max-h-[90vh] w-full max-w-xl space-y-4 overflow-y-auto rounded-xl bg-white p-6 shadow-xl">
        <h3 id="start-batch-heading" className="font-semibold">Start new batch</h3>
        <FormSelect label="Product" value={productId} onChange={setProductId} options={[
          { value: '', label: products.isLoading ? 'Loading products…' : 'Choose a product' },
          ...(products.data ?? []).map((product) => ({ value: String(product.id), label: product.productName })),
        ]} />
        {products.data?.length === 0 && <p className="text-sm">No products are configured in the catalog.</p>}
        <FormSelect label="Primary material issue" value={primaryIssueId} onChange={(id) => {
          setPrimaryIssueId(id);
          setAdditional((selected) => selected.filter((item) => item !== Number(id)));
        }} options={[
          { value: '', label: issues.isLoading ? 'Loading issues…' : 'Choose a material issue' },
          ...(issues.data ?? []).map((issue) => ({ value: String(issue.id), label: issueLabel(issue) })),
        ]} />
        {issues.data?.length === 0 && <p className="text-sm">Ask the storekeeper to issue an approved lot first.</p>}
        {primary && (
          <fieldset className="max-h-48 overflow-y-auto rounded-md border p-3">
            <legend className="px-1 text-sm font-medium">Additional issued lots</legend>
            {issues.data?.filter((issue) => issue.id !== primary.id).map((issue) => (
              <label key={issue.id} className="flex min-h-11 items-center gap-3 text-sm">
                <input type="checkbox" checked={additional.includes(issue.id)}
                  disabled={additional.length >= 30 && !additional.includes(issue.id)}
                  onChange={(event) => setAdditional((selected) => event.target.checked
                    ? [...selected, issue.id] : selected.filter((item) => item !== issue.id))} />
                {issueLabel(issue)}
              </label>
            ))}
          </fieldset>
        )}
        {error && <p role="alert" className="text-sm text-red-700">{error.message}</p>}
        <div className="flex justify-end gap-2">
          <button type="button" onClick={onClose} disabled={startBatch.isPending}
            className="min-h-11 rounded-md border px-4">Cancel</button>
          <button disabled={!primary || !productId || startBatch.isPending}
            className="min-h-11 rounded-md bg-brand-600 px-4 text-white disabled:opacity-60">
            {startBatch.isPending ? 'Starting…' : 'Start batch'}
          </button>
        </div>
      </form>
    </div>
  );
}
