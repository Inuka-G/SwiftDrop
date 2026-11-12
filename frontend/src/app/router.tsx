import { createBrowserRouter, Navigate } from 'react-router-dom'
import {
  CourierLayout,
  DeliveryDetailPage,
  ProofOfDeliveryPage,
  RunsPage,
  ShiftPage,
} from '../features/courier'
import { LoginPage } from '../features/identity'
import { ROLES } from '../lib/constants'
import { AppLayout } from './layout/AppLayout'
import { RequireRole } from './RequireRole'
import '../styles/index.css'

export const router = createBrowserRouter([
  {
    element: <AppLayout />,
    children: [
      { index: true, element: <Navigate to="/courier" replace /> },
      { path: 'login', element: <LoginPage /> },
    ],
  },
  {
    // The courier app has its own phone-style frame with a bottom tab bar.
    path: 'courier',
    element: (
      <RequireRole roles={[ROLES.courier]}>
        <CourierLayout />
      </RequireRole>
    ),
    children: [
      { index: true, element: <RunsPage view="active" /> },
      { path: 'completed', element: <RunsPage view="completed" /> },
      { path: 'shift', element: <ShiftPage /> },
      { path: 'deliveries/:id', element: <DeliveryDetailPage /> },
      { path: 'deliveries/:id/proof', element: <ProofOfDeliveryPage /> },
    ],
  },
])
