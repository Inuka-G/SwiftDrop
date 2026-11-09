import { createBrowserRouter } from 'react-router-dom'
import { CourierDashboardPage } from '../features/courier'
import { AppLayout } from './layout/AppLayout'
import '../styles/index.css'

export const router = createBrowserRouter([
  {
    element: <AppLayout />,
    children: [
      { index: true, element: <CourierDashboardPage /> },
      { path: 'courier', element: <CourierDashboardPage /> },
    ],
  },
])
