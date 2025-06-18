import path from 'path';
import react from '@vitejs/plugin-react';
import { defineConfig } from 'vite'; 
import license from 'vite-plugin-license';

export default defineConfig({
    plugins: [react(),
        license({
            thirdParty: {
                output: {
                    file: './dist/licenses.txt',
                },
            },
        })    ],
    server: {
        hmr: false
    },
  resolve: {
    alias: {
      '@': path.resolve(__dirname, './src'),
    },
  },
  build: {
    sourcemap: true,
    reportCompressedSize: false,
    minify: 'esbuild', // Faster and less memory intensive than terser
    rollupOptions: {
      maxParallelFileOps: 2, // Reduce concurrent operations
    }
  },
});
