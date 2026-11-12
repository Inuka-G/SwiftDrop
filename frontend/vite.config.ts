import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    // Same-origin requests in dev, so the API needs no CORS setup.
    proxy: {
      '/api': 'http://localhost:5017',
      '/hubs': { target: 'http://localhost:5017', ws: true },
    },
  },
})
