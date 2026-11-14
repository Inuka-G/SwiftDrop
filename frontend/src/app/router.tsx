import { createBrowserRouter } from 'react-router-dom'
import { CourierDashboardPage } from '../features/courier'
import { AppLayout } from './layout/AppLayout'
import { AdminLayout } from './layout/AdminLayout'
import { AdminDashboardPage, DispatchBoardPage } from '../features/dispatch'
import '../styles/index.css'

export const router = createBrowserRouter([
  {
    element: <AppLayout />,
    children: [
      { index: true, element: <CourierDashboardPage /> },
      { path: 'courier', element: <CourierDashboardPage /> },
    ],
  },
  // Admin routes
  {
    path: 'admin',
    element: <AdminLayout />,
    children: [
      { index: true, element: <AdminDashboardPage /> },
      { path: 'dispatch', element: <DispatchBoardPage /> },
    ],
  },
])
