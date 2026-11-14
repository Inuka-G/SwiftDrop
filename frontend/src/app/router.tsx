import { createBrowserRouter } from 'react-router-dom'
import { AIAssistantPage } from '../features/ai-assistant'
import { CourierDashboardPage } from '../features/courier'
import {
  CreateBookingPage,
  CustomerHistoryPage,
  PublicTrackingPage,
} from '../features/parcels'
import { AppLayout } from './layout/AppLayout'
import '../styles/index.css'

export const router = createBrowserRouter([
  {
    element: <AppLayout />,
    children: [
      { index: true, element: <CourierDashboardPage /> },
      { path: 'courier', element: <CourierDashboardPage /> },
      { path: 'book', element: <CreateBookingPage /> },
      { path: 'history', element: <CustomerHistoryPage /> },
      { path: 'track', element: <PublicTrackingPage /> },
      { path: 'assistant', element: <AIAssistantPage /> },
    ],
  },
])
