// Serves synthetic data in the browser without sending demo requests to a live API.
import { AxiosError, type AxiosAdapter, type InternalAxiosRequestConfig } from 'axios';
import type { DashboardStats, LoginResponse, PagedResult, Traceability } from '@/lib/types/api';
import type { RecallReport } from '@/features/bottling/api';
import { batches, materials, pallets, products, scrap, shipments } from './fixtures';

function page<T>(items: T[], params: URLSearchParams): PagedResult<T> {
  const page = Math.max(1, Number(params.get('page')) || 1);
  const pageSize = Math.max(1, Number(params.get('pageSize')) || 10);
  return {
    items: items.slice((page - 1) * pageSize, page * pageSize),
    totalCount: items.length, page, pageSize, totalPages: Math.ceil(items.length / pageSize),
  };
}

const stats: DashboardStats = {
  totalBatches: batches.length,
  inProductionCount: batches.filter((batch) => batch.currentState === 'In_Production').length,
  waitingQcCount: batches.filter((batch) => batch.currentState === 'Waiting_QC').length,
  pendingApprovalCount: batches.filter((batch) => batch.currentState === 'QC_Passed').length,
  shippedCount: shipments.length,
  quarantineCount: batches.filter((batch) => batch.currentState === 'Quarantine').length,
  totalScrapQty: batches.reduce((sum, batch) => sum + batch.scrapQty, 0),
  totalDestroyedQty: scrap.filter((entry) => entry.finalDecision === 'Destroyed').reduce((sum, entry) => sum + entry.quantity, 0),
  totalReworkedBatches: batches.filter((batch) => batch.isReworked).length,
};

const heldBatches = new Set([2]);

function currentPallets() {
  return pallets.map((pallet) => pallet.state === 'Shipped' ? pallet : {
    ...pallet, state: heldBatches.has(pallet.batchId) ? 'Held' : 'In_Stock',
  });
}

function recall(code: string | null): RecallReport | undefined {
  const material = materials.find((entry) => entry.lotCode === code?.trim().toUpperCase());
  if (!material?.lotCode) return undefined;
  const affected = batches.filter((batch) =>
    batch.rawMaterialId === material.id || (material.id === 1 && batch.id <= 2)
  );
  return {
    materialId: material.id, lotCode: material.lotCode, supplierLot: material.supplierLot ?? null,
    materialName: material.materialName, supplierName: material.supplierName,
    unit: material.unit ?? 'kg', stockQuantity: material.currentQuantity,
    batches: affected.map((batch) => ({
      id: batch.id, batchNumber: batch.batchNumber, state: batch.currentState,
      isHeld: heldBatches.has(batch.id), producedQuantity: batch.producedQty,
      legacyClient: null, legacyShippedAt: null,
      pallets: currentPallets().filter((pallet) => pallet.batchId === batch.id),
    })),
  };
}

function trace(code: string): Traceability | undefined {
  const batch = batches.find((entry) => entry.batchNumber === code.toUpperCase());
  if (!batch) return undefined;
  const shipped = shipments.find((entry) => entry.batchId === batch.id);
  const waste = scrap.find((entry) => entry.referenceId === batch.id);
  const passed = ['QC_Passed', 'Ready_For_Shipping', 'Shipped'].includes(batch.currentState);
  return {
    ...batch, batchId: batch.id, rawMaterialName: materials.find((item) => item.id === batch.rawMaterialId)?.materialName ?? null,
    createdByUser: 'Demo shift supervisor',
    labResultsSummary: passed ? ['Finished batch inspection passed'] :
      batch.currentState === 'QC_Failed' ? ['Cap seal inspection failed'] : [],
    managerDecisionSummary: waste ? 'Quality review recorded' :
      ['Ready_For_Shipping', 'Shipped'].includes(batch.currentState) ? 'Released by quality' : null,
    shippingSummary: shipped ? `Delivered to ${shipped.clientName}` : null,
    scrapSummary: waste?.scrapReason ?? null,
  };
}

function read(path: string, params: URLSearchParams): unknown {
  switch (path) {
    case '/store/materials': return page(materials, params);
    case '/store/materials/approved': return materials.filter((material) => material.isQCApproved);
    case '/production/batches': return page(batches, params);
    case '/lab/pending-materials': return materials.filter((material) => !material.isQCApproved);
    case '/lab/pending-batches': return batches.filter((batch) => batch.currentState === 'Waiting_QC');
    case '/manager/pending-approvals': return batches.filter((batch) => ['QC_Passed', 'QC_Failed'].includes(batch.currentState));
    case '/scrap/pending': return scrap.filter((entry) => entry.finalDecision !== 'Destroyed');
    case '/shipping/ready': return batches.filter((batch) => batch.currentState === 'Ready_For_Shipping' && !heldBatches.has(batch.id));
    case '/shipping/all': return page(shipments, params);
    case '/reports/dashboard-stats': return stats;
    case '/bottling/products': return products;
    case '/bottling/issues': return [];
    case '/bottling/packable-batches': return batches.filter((batch) => batch.currentState === 'Ready_For_Shipping' && !heldBatches.has(batch.id));
    case '/bottling/pallets': return currentPallets().filter((pallet) => pallet.state !== 'Shipped');
    case '/bottling/recall': return recall(params.get('lotCode'));
    default: {
      const prefix = '/reports/traceability/by-number/';
      return path.startsWith(prefix) ? trace(decodeURIComponent(path.slice(prefix.length))) : undefined;
    }
  }
}

function failure(config: InternalAxiosRequestConfig, status: number, errorCode: string, message: string): never {
  throw new AxiosError(message, AxiosError.ERR_BAD_REQUEST, config, undefined, {
    config, status, statusText: message, headers: {}, data: { success: false, data: null, errorCode, message },
  });
}

export const demoAdapter: AxiosAdapter = async (config) => {
  const path = config.url ?? '';
  const method = config.method?.toLowerCase();
  let data: unknown;

  if (path === '/auth/login' && method === 'post') {
    data = { token: 'demo-session', fullName: 'Demo Operator', role: 'SuperAdmin',
      expiresAt: new Date(Date.now() + 3_600_000).toISOString() } satisfies LoginResponse;
  } else if (method === 'put' && /^\/bottling\/batches\/\d+\/hold$/.test(path)) {
    const id = Number(path.split('/')[3]);
    const batch = batches.find((entry) => entry.id === id);
    const input: unknown = JSON.parse(typeof config.data === 'string' ? config.data : '{}');
    if (!batch || batch.currentState === 'Shipped' || batch.currentState === 'Destroyed')
      failure(config, 409, 'BATCH_CLOSED', 'This batch is already closed.');
    if (!input || typeof input !== 'object' || !('isHeld' in input) || typeof input.isHeld !== 'boolean' ||
        !('reason' in input) || typeof input.reason !== 'string' || !input.reason.trim())
      failure(config, 400, 'HOLD_REASON_REQUIRED', 'Enter a quality decision reason.');
    if (input.isHeld) heldBatches.add(id);
    else heldBatches.delete(id);
    data = input.isHeld;
  } else if (method === 'get') {
    data = read(path, new URLSearchParams(config.params));
    if (data === undefined) failure(config, 404, 'DEMO_NOT_FOUND', 'No matching demo record. Try CAPS-DEMO or BATCH-DEMO-001.');
  } else {
    failure(config, 409, 'DEMO_READ_ONLY', 'This public demo contains sample data. Changes require a connected workspace.');
  }

  return { config, status: 200, statusText: 'OK', headers: {}, data: { success: true, data, errorCode: null, message: null } };
};
