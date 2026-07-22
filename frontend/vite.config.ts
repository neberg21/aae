import { fileURLToPath } from 'node:url'
import { defineConfig } from 'vitest/config'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react(), tailwindcss()],
  server: {
    proxy: {
      //'/api': 'http://localhost:5296',
      '/api': 'https://normal-coreen-neberg-a84da4bd.koyeb.app',
      '/webhook': 'https://convenient-nonie-neberg-ad5744ad.koyeb.app',
    },
  },
  test: {
    // Normalize root path casing on Windows so Vitest suite context resolves
    root: fileURLToPath(new URL('./', import.meta.url)),
    environment: 'jsdom',
    setupFiles: ['./src/test/setup.ts'],
    include: ['src/__tests__/**/*.{test,spec}.{ts,tsx}'],
  },
})
