import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react(), tailwindcss()],
  optimizeDeps: {
    // dagre ships as CJS; Vite needs to pre-bundle it or the ESM import resolves to undefined
    include: ['dagre'],
  },
})
