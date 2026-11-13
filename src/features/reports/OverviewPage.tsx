import { Link } from 'react-router-dom';
import { isDemoMode } from '@/lib/api-client';
import { PageHeader } from '@/components/Spinner';
import { useDashboardStats } from './hooks';

function StatCard({ label, value, tone }: { label: string; value: number; tone: string }) {
  return (
    <div className={`rounded-lg border-l-4 border border-gray-200 bg-white p-4 ${tone}`}>
      <p className="text-xs font-medium text-gray-500">{label}</p>
      <p className="mt-1 text-2xl font-semibold text-gray-900">{value.toLocaleString()}</p>
    </div>
  );
}

export function OverviewPage() {
  const { data: stats, isLoading, error } = useDashboardStats();

  return (
    <div>
      <PageHeader title="Overview" description="A snapshot of the whole operation." />

      {isDemoMode && <p className="mb-4 text-sm text-gray-700">Start with the{' '}
        <Link className="font-medium text-brand-700 underline" to="/recall">cap lot recall</Link>{' '}
        to see affected shipments and try a quality hold.</p>}
      {error && <p role="alert" className="text-sm text-red-700">{error.message}</p>}
      {isLoading ? (
        <p className="text-sm text-gray-400">Loading...</p>
      ) : stats ? (
        <>
          <div className="mb-4 grid grid-cols-2 gap-4 md:grid-cols-3">
            <StatCard label="In Production" value={stats.inProductionCount} tone="border-l-blue-600" />
            <StatCard label="Waiting for Lab" value={stats.waitingQcCount} tone="border-l-amber-500" />
            <StatCard label="Passed, Awaiting Decision" value={stats.pendingApprovalCount} tone="border-l-emerald-600" />
            <StatCard label="In Quarantine" value={stats.quarantineCount} tone="border-l-orange-500" />
            <StatCard label="Shipped" value={stats.shippedCount} tone="border-l-gray-400" />
            <StatCard label="Total Batches" value={stats.totalBatches} tone="border-l-gray-300" />
          </div>

          <div className="mb-6 grid grid-cols-3 gap-4">
            <StatCard label="Total Scrap Qty" value={stats.totalScrapQty} tone="border-l-red-400" />
            <StatCard label="Total Destroyed Qty" value={stats.totalDestroyedQty} tone="border-l-gray-800" />
            <StatCard label="Reworked Batches" value={stats.totalReworkedBatches} tone="border-l-purple-500" />
          </div>
        </>
      ) : null}
    </div>
  );
}
