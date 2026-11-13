// Connects the bottling screens to the role-scoped API.
import { apiClient, unwrap } from '@/lib/api-client';
import type { ApiResponse, BatchSummary } from '@/lib/types/api';

export interface MaterialIssue {
  id: number;
  materialId: number;
  materialName: string;
  lotCode: string | null;
  unit: string;
  quantity: number;
}

export interface ProductOption {
  id: number;
  productName: string;
  sku: string | null;
}

export interface Pallet {
  id: number;
  code: string;
  batchId: number;
  batchNumber: string;
  quantity: number;
  state: string;
  clientName: string | null;
  shippedAt: string | null;
}

export interface RecallBatch {
  id: number;
  batchNumber: string;
  state: string;
  isHeld: boolean;
  producedQuantity: number;
  legacyClient: string | null;
  legacyShippedAt: string | null;
  pallets: Pallet[];
}

export interface RecallReport {
  materialId: number;
  lotCode: string;
  supplierLot: string | null;
  materialName: string;
  supplierName: string;
  unit: string;
  stockQuantity: number;
  batches: RecallBatch[];
}

export interface DispatchRequest {
  requestId: string;
  clientName: string;
  truckPlates: string;
  palletCodes: string[];
}

export function getProducts(): Promise<ProductOption[]> {
  return unwrap(apiClient.get<ApiResponse<ProductOption[]>>('/bottling/products'));
}

export function getIssues(): Promise<MaterialIssue[]> {
  return unwrap(apiClient.get<ApiResponse<MaterialIssue[]>>('/bottling/issues'));
}

export function getPackableBatches(): Promise<BatchSummary[]> {
  return unwrap(apiClient.get<ApiResponse<BatchSummary[]>>('/bottling/packable-batches'));
}

export function getPallets(): Promise<Pallet[]> {
  return unwrap(apiClient.get<ApiResponse<Pallet[]>>('/bottling/pallets'));
}

export function packPallet(batchId: number, code: string, quantity: number): Promise<Pallet> {
  return unwrap(apiClient.post<ApiResponse<Pallet>>(`/bottling/batches/${batchId}/pallets`, { code, quantity }));
}

export function dispatchPallets(request: DispatchRequest): Promise<Pallet[]> {
  return unwrap(apiClient.post<ApiResponse<Pallet[]>>('/bottling/dispatch', request));
}

export function getRecall(lotCode: string): Promise<RecallReport> {
  return unwrap(apiClient.get<ApiResponse<RecallReport>>('/bottling/recall', { params: { lotCode } }));
}

export function setBatchHold(batchId: number, isHeld: boolean, reason: string): Promise<boolean> {
  return unwrap(apiClient.put<ApiResponse<boolean>>(`/bottling/batches/${batchId}/hold`, { isHeld, reason }));
}
