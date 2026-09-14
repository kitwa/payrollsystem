// Copies the production Angular build into the API's wwwroot so a single `dotnet run`/deployment
// serves both the API and the SPA. Runs automatically after `npm run build` (see package.json "postbuild").
import { existsSync, rmSync, cpSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { dirname, resolve } from 'node:path';

const __dirname = dirname(fileURLToPath(import.meta.url));
const distDir = resolve(__dirname, '../dist/client');
const wwwrootDir = resolve(__dirname, '../../Backend/src/Payroll.Api/wwwroot');

if (!existsSync(distDir)) {
  console.error(`Build output not found at ${distDir}. Run "npm run build" first.`);
  process.exit(1);
}

rmSync(wwwrootDir, { recursive: true, force: true });
cpSync(distDir, wwwrootDir, { recursive: true });

console.log(`Copied ${distDir} -> ${wwwrootDir}`);
