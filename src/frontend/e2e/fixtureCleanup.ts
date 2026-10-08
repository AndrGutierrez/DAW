import { execFileSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';

// The API correctly preserves operational history. Remove only test-owned movement
// rows inside the guarded disposable database before the usual API fixture teardown.
export function clearFixtureMovements(animalIds: string[]) {
  if (!animalIds.length) return;
  if (process.env.E2E_ISOLATED_DATABASE !== '1') throw new Error('Fixture cleanup requires the named disposable E2E database.');
  const bash = process.env.E2E_BASH || (process.platform === 'win32' ? 'C:/Program Files/Git/bin/bash.exe' : '/bin/bash');
  const script = fileURLToPath(new URL('../../../scripts/phase4/clear-e2e-movements.sh', import.meta.url)).replaceAll('\\', '/');
  execFileSync(bash, [script, ...animalIds], { env: process.env, windowsHide: true, stdio: 'pipe', timeout: 15000 });
}
