import { createBrowserRouter } from 'react-router-dom'
import { AIAssistantPage } from '../features/ai-assistant'
import { CourierDashboardPage } from '../features/courier'
import { HomePage } from '../features/home'
import { GoogleCallbackPage, LoginPage, SignupPage } from '../features/identity'
import {
  AdminMerchantQueuePage,
  MerchantPortalPage,
  MerchantRegistrationPage,
  MerchantStatusPage,
} from '../features/merchants'
import {
  CreateBookingPage,
  CustomerHistoryPage,
  PublicTrackingPage,
} from '../features/parcels'
import { AppLayout } from './layout/AppLayout'
import { AdminLayout } from './layout/AdminLayout'
import { AdminDashboardPage } from '../features/dispatch'
import '../styles/index.css'
import '../styles/swiftdrop.css'

export const router = createBrowserRouter([
  // Public pages with their own full-page design
  { index: true, element: <HomePage /> },
  { path: 'login', element: <LoginPage /> },
  { path: 'signup', element: <SignupPage /> },
  { path: 'merchant/register', element: <MerchantRegistrationPage /> },
  { path: 'merchant/status', element: <MerchantStatusPage /> },
  { path: 'merchant/portal', element: <MerchantPortalPage /> },
  { path: 'auth/google/callback', element: <GoogleCallbackPage /> },

  // Signed-in workspaces
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
  
  // Admin routes
  {
    path: 'admin',
    element: <AdminLayout />,
    children: [
      { index: true, element: <AdminDashboardPage /> },
      { path: 'merchants', element: <AdminMerchantQueuePage /> },
    ],
  },

  
])
