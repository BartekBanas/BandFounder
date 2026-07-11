import {defineConfig} from 'vitest/config'
import react from '@vitejs/plugin-react'

export default defineConfig({
    plugins: [react()],
    test: {
        environment: 'jsdom',
        setupFiles: './src/test/setup.ts',
    },
    server: {
        port: 3000,
        host: '127.0.0.1',
        watch: {
            usePolling: true,
        },
    },
})
