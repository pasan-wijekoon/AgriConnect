import fs from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const counters = new Map();

async function step(driver, testId, description) {
  const next = (counters.get(testId) || 0) + 1;
  counters.set(testId, next);

  const folder = path.resolve(__dirname, '../../evidence/appium', testId);
  await fs.mkdir(folder, { recursive: true });

  const safeDescription = description
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, '_')
    .replace(/^_|_$/g, '');
  const file = path.join(
    folder,
    `${String(next).padStart(2, '0')}_${safeDescription}.png`,
  );
  await driver.saveScreenshot(file);
  console.log(`[${testId}] ${next}: ${description} -> ${file}`);
  return file;
}

export { step };
