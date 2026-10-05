import react from '@vitejs/plugin-react'
import { defineConfig } from 'vitest/config'

// Component tests only. The older tests in tests/*.test.ts use Node's built-in runner
// (`npm test`), so Vitest is limited to tests/component to avoid picking those up.
export default defineConfig({
  plugins: [react()],
  test: {
    environment: 'jsdom',
    include: ['tests/component/**/*.test.tsx'],
    setupFiles: ['tests/component/setup.ts'],
    css: false,
    restoreMocks: true,
    coverage: {
      provider: 'v8',
      include: ['src/pages/LoginPage.tsx', 'src/pages/BuyerDashboard.tsx', 'src/components/AddEditListingModal.tsx', 'src/components/ProductDetailModal.tsx', 'src/App.tsx'],
      reporter: ['text', 'html'],
    },
  },
})
