/// <reference types="vitest/config" />
import { defineConfig, loadEnv } from 'vite';
import react from '@vitejs/plugin-react';
import { fileURLToPath, URL } from 'node:url';

export default defineConfig(({ command, mode }) => {
  if (command === 'build' && mode === 'production') {
    const apiUrl = loadEnv(mode, process.cwd(), 'VITE_').VITE_API_BASE_URL?.trim();
    if (apiUrl) {
      const url = new URL(apiUrl);
      const isLoopback = ['localhost', '127.0.0.1', '[::1]'].includes(url.hostname);
      const localPreview = isLoopback && url.protocol === 'http:' && !process.env.VERCEL;
      if ((!localPreview && (url.protocol !== 'https:' || (isLoopback && process.env.VERCEL))) ||
          !url.pathname.replace(/\/$/, '').endsWith('/api/v1') || url.username || url.password || url.search || url.hash) {
        throw new Error('VITE_API_BASE_URL must be an HTTPS API URL ending in /api/v1 for Vercel.');
      }
    }
  }
  return {
    plugins: [react()],
    resolve: {
      alias: {
        '@': fileURLToPath(new URL('./src', import.meta.url)),
      },
    },
    server: {
      port: 5173,
    },
    test: {
      environment: 'jsdom',
      globals: true,
      setupFiles: ['./src/test/setup.ts'],
    },
  };
});
