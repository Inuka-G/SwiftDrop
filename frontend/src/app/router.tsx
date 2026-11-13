import { createBrowserRouter } from 'react-router-dom'
import { CourierDashboardPage } from '../features/courier'
import { HomePage } from '../features/home'
import { GoogleCallbackPage, LoginPage, SignupPage } from '../features/identity'
import { AppLayout } from './layout/AppLayout'
import { ComingSoonPage } from './pages/ComingSoonPage'
import '../styles/index.css'
import '../styles/swiftdrop.css'

export const router = createBrowserRouter([
  // Public pages with their own full-page design
  { index: true, element: <HomePage /> },
  { path: 'login', element: <LoginPage /> },
  { path: 'signup', element: <SignupPage /> },
  { path: 'auth/google/callback', element: <GoogleCallbackPage /> },

  // Signed-in workspaces
  {
    element: <AppLayout />,
    children: [{ path: 'courier', element: <CourierDashboardPage /> }],
  },

  // Pages other team members are building: tracking, booking, merchant registration
  { path: '*', element: <ComingSoonPage /> },
])
