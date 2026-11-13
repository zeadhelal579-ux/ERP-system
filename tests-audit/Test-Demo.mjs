// Rehearses the recall and hold screens in Chromium using explicitly mocked API fixtures.
import assert from 'node:assert/strict';
import { fileURLToPath } from 'node:url';
import { mkdir, writeFile } from 'node:fs/promises';
import { chromium } from 'playwright';
import AxeBuilder from '@axe-core/playwright';

const output = new URL('../audit-report/logs/', import.meta.url);
await mkdir(output, { recursive: true });
const browser = await chromium.launch({ executablePath: process.env.ERP_CHROMIUM_PATH });
const results = [];
try {
  for (const [name, url, viewport] of [
    ['source-desktop', 'http://127.0.0.1:5193', { width: 1440, height: 900 }],
    ['demo-mobile', 'http://127.0.0.1:4194', { width: 390, height: 844 }],
  ]) {
    const context = await browser.newContext({ viewport });
    const page = await context.newPage();
    const errors = [];
    page.on('pageerror', (error) => errors.push(error.message));
    let isHeld = false;
    if (!process.env.ERP_BROWSER_PASSWORD) await page.route('**/api/v1/**', async (route) => {
      const path = new URL(route.request().url()).pathname;
      let data;
      if (path.endsWith('/auth/login')) data = { token: 'fixture-token', fullName: 'Demo Quality', role: 'QualityManager' };
      if (path.endsWith('/manager/pending-approvals')) data = [];
      if (path.endsWith('/hold')) {
        isHeld = route.request().postDataJSON().isHeld;
        data = isHeld;
      }
      if (path.endsWith('/recall')) data = {
        lotCode: 'CAPS-DEMO', materialName: 'Bottle caps', supplierLot: 'CAPS-01',
        supplierName: 'Demo supplier', unit: 'pcs', stockQuantity: 200,
        batches: [{ id: 1, batchNumber: 'BATCH-DEMO', state: 'Ready_For_Shipping',
          isHeld, producedQuantity: 45, pallets: [
            { id: 1, code: 'PALLET-1-A', quantity: 20, state: 'Shipped', clientName: 'Demo distributor' },
            { id: 2, code: 'PALLET-1-B', quantity: 25, state: isHeld ? 'Held' : 'In_Stock' },
          ] }],
      };
      if (data === undefined) { await route.abort(); return; }
      await route.fulfill({ json: { success: true, data, message: null, errorCode: null } });
    });
    await page.goto(url);
    await page.getByLabel('Username').fill(process.env.ERP_BROWSER_PASSWORD ? 'QualityManager' : 'quality-demo');
    await page.getByLabel('Password', { exact: true }).fill(process.env.ERP_BROWSER_PASSWORD ?? 'fixture-password');
    await page.getByRole('button', { name: 'Sign In' }).click();
    await page.getByRole('link', { name: 'Lot recall' }).click();
    await page.getByLabel('Material lot barcode').fill(process.env.ERP_BROWSER_LOT ?? 'CAPS-DEMO');
    await page.getByRole('button', { name: 'Trace lot' }).click();
    await page.getByText('Demo distributor', { exact: false }).first().waitFor();
    const batchCard = page.getByRole('article').first();
    await batchCard.getByLabel('Quality decision reason').fill('Review cap defect');
    if (process.env.ERP_BROWSER_PASSWORD) {
      await batchCard.getByRole('button', { name: 'Release hold' }).click();
      await batchCard.getByRole('button', { name: 'Place quality hold' }).waitFor();
      await batchCard.getByLabel('Quality decision reason').fill('Review cap defect');
    }
    await batchCard.getByRole('button', { name: 'Place quality hold' }).click();
    await page.getByText('On quality hold', { exact: false }).waitFor();
    assert.equal(await page.evaluate(() => document.documentElement.scrollWidth > innerWidth), false);
    assert.deepEqual(errors, []);
    const { violations } = await new AxeBuilder({ page }).analyze();
    await page.screenshot({ path: fileURLToPath(new URL(`${name}-recall.png`, output)), fullPage: true });
    results.push({ name, api: process.env.ERP_BROWSER_PASSWORD ? 'real' : 'mocked', holdRehearsal: 'passed', noHorizontalOverflow: true,
      axe: violations.map(({ id, impact, nodes }) => ({ id, impact, targets: nodes.flatMap((node) => node.target) })) });
    await context.close();
  }
  await writeFile(new URL('browser-results.json', output), JSON.stringify(results, null, 2));
  console.log(JSON.stringify(results));
} finally {
  await browser.close();
}
