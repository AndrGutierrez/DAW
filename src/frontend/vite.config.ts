import { defineConfig } from 'vitest/config';
import react from '@vitejs/plugin-react';
import { env } from 'node:process';
import tailwindcss from '@tailwindcss/vite';

export default defineConfig({
  plugins: [react(), tailwindcss()],
  server: {
    proxy: {
      '/api': { target: env.API_PROXY_TARGET || 'http://127.0.0.1:8080', changeOrigin: true },
    },
  },
  test: { include: ['src/**/*.test.{ts,tsx}'], environment: 'node', setupFiles: ['./src/test/setup.ts'], coverage: { provider: 'v8', reporter: ['text', 'json-summary', 'lcov', 'html'], include: ['src/**/*.{ts,tsx}'], exclude: ['src/**/*.test.{ts,tsx}', 'src/test/**', 'src/main.tsx', 'src/vite-env.d.ts'] } },
});
