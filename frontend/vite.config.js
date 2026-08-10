import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {     
    port: 5173,
    proxy: {
      '/api': {
            target: 'http://localhost:50672', //this port where the backend is running
        changeOrigin: true,
        secure: false,
      }
    }
  },
  base: "/", 
})
