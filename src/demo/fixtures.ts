// Generates a consistent synthetic bottling dataset for the public walkthrough.
import type { BatchState, ProductionBatch, RawMaterial, ScrapEntry, ShipmentRecord } from '@/lib/types/api';
import type { Pallet, ProductOption } from '@/features/bottling/api';

const states: BatchState[] = ['Shipped', 'Ready_For_Shipping', 'Waiting_QC', 'In_Production', 'QC_Passed', 'QC_Failed', 'Quarantine', 'Rework_In_Progress', 'Destroyed'];
const ingredients = [
  ['PET bottle caps', 'Delta Closures', 'pcs', 'Solid'],
  ['Cola syrup', 'Atlas Ingredients', 'L', 'Liquid'],
  ['PET preforms', 'Nile Packaging', 'pcs', 'Solid'],
  ['Lemon concentrate', 'Citrus Supply', 'L', 'Liquid'],
  ['Fine sugar', 'Delta Ingredients', 'kg', 'Powder'],
  ['Printed labels', 'Cairo Labels', 'pcs', 'Solid'],
] as const;

export const products: ProductOption[] = [
  { id: 1, productName: 'Cola PET 500 mL', sku: 'COLA-500' },
  { id: 2, productName: 'Lemon Soda PET 330 mL', sku: 'LEMON-330' },
  { id: 3, productName: 'Cola PET 1 L', sku: 'COLA-1000' },
];

export const materials: RawMaterial[] = Array.from({ length: 36 }, (_, index) => {
  const [materialName, supplierName, unit, physicalState] = ingredients[index % ingredients.length];
  return {
    id: index + 1, materialName, supplierName, unit, physicalState,
    lotCode: index === 0 ? 'CAPS-DEMO' : `LOT-DEMO-${String(index + 1).padStart(3, '0')}`,
    supplierLot: `SUP-${9001 + index}`, currentQuantity: 200 + (index * 137) % 1900,
    expiryDate: `2027-${String(3 + index % 8).padStart(2, '0')}-20T00:00:00Z`,
    isQCApproved: index % 5 !== 4,
  };
});

export const batches: ProductionBatch[] = Array.from({ length: 54 }, (_, index) => {
  const currentState = states[index % states.length];
  const producedQty = currentState === 'In_Production' ? 0 : 800 + (index % 9) * 30;
  const returnedMaterialQty = producedQty ? 10 : 0;
  const scrapQty = producedQty ? 8 + index % 7 : 0;
  return {
    id: index + 1, batchNumber: `BATCH-DEMO-${String(index + 1).padStart(3, '0')}`,
    productId: products[index % products.length].id, rawMaterialId: 2 + index % 24,
    issuedMaterialQty: producedQty ? producedQty + returnedMaterialQty + scrapQty : 900,
    producedQty, returnedMaterialQty, scrapQty, currentState,
    isReworked: currentState === 'Rework_In_Progress', expiryDate: '2027-04-20T00:00:00Z',
    createdAt: `2026-11-${String(1 + index % 12).padStart(2, '0')}T08:00:00Z`,
  };
});

const customers = ['Delta Retail', 'Alexandria Distribution', 'Cairo Wholesale'];
const shippedAt = '2026-11-13T10:00:00Z';

export const pallets: Pallet[] = batches.flatMap((batch) => {
  if (batch.currentState !== 'Shipped' && batch.currentState !== 'Ready_For_Shipping') return [];
  return [0, 1].map((index) => {
    const shipped = batch.currentState === 'Shipped' || (batch.id === 2 && index === 0);
    const held = batch.id === 2 && !shipped;
    return {
      id: batch.id * 10 + index, code: `PAL-DEMO-${batch.id}-${index + 1}`,
      batchId: batch.id, batchNumber: batch.batchNumber, quantity: batch.producedQty / 2,
      state: shipped ? 'Shipped' : held ? 'Held' : 'In_Stock',
      clientName: shipped ? customers[(batch.id - 1) % customers.length] : null,
      shippedAt: shipped ? shippedAt : null,
    };
  });
});

export const shipments: ShipmentRecord[] = batches.filter((batch) => batch.currentState === 'Shipped')
  .map((batch) => ({
    id: batch.id, batchId: batch.id, batchNumber: batch.batchNumber,
    clientName: customers[(batch.id - 1) % customers.length], driverName: 'Demo driver',
    truckPlates: `DEMO-${batch.id}`, shippingDate: shippedAt,
  }));

export const scrap: ScrapEntry[] = batches.filter((batch) =>
  ['Quarantine', 'Rework_In_Progress', 'Destroyed'].includes(batch.currentState)
).map((batch) => ({
  id: batch.id, sourceType: 'ManagerRejection', referenceId: batch.id, quantity: batch.producedQty,
  scrapReason: 'Packaging inspection found a cap seal deviation.',
  finalDecision: batch.currentState === 'Destroyed' ? 'Destroyed'
    : batch.currentState === 'Rework_In_Progress' ? 'Sent_To_Rework' : 'Pending',
  createdAt: batch.createdAt,
}));
