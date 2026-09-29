import react from '@vitejs/plugin-react'
import { defineConfig } from 'vitest/config'

const proxyDaApi = { '/api': 'http://localhost:5290' }

export default defineConfig({
  plugins: [react()],
  server: { proxy: proxyDaApi },
  preview: { proxy: proxyDaApi },
  test: {
    environment: 'jsdom',
    setupFiles: './src/testes/setup.ts',
    unstubGlobals: true,
  },
})
