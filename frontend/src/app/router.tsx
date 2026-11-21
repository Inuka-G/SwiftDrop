import { createBrowserRouter } from "react-router-dom";
import { AIAssistantPage } from "../features/ai-assistant";
import {
  CourierLayout,
  DeliveryDetailPage,
  ProofOfDeliveryPage,
  RunsPage,
  ShiftPage,
} from "../features/courier";
import { HomePage } from "../features/home";
import {
  GoogleCallbackPage,
  LoginPage,
  SignupPage,
  UserManagementPage,
} from "../features/identity";
import {
  CreateBookingPage,
  CustomerDashboardPage,
  CustomerHistoryPage,
  PublicTrackingPage,
} from "../features/parcels";
import { ROLES } from "../lib/constants";
import { AppLayout } from "./layout/AppLayout";
import { AdminLayout } from "./layout/AdminLayout";
import { ReportsPage } from "../features/reporting";
import { ZonesPricingPage } from "../features/pricing";
import { RequireRole } from "./RequireRole";
import {
  AdminDashboardPage,
  CourierWorkloadPage,
  DispatchBoardPage,
} from "../features/dispatch";
import "../styles/index.css";
import "../styles/swiftdrop.css";

export const router = createBrowserRouter([
  // Public pages with their own full-page design
  { index: true, element: <HomePage /> },
  { path: "login", element: <LoginPage /> },
  { path: "signup", element: <SignupPage /> },
  { path: "auth/google/callback", element: <GoogleCallbackPage /> },

  // Signed-in workspaces
  {
    element: <AppLayout />,
    children: [
      { path: "customer", element: import.meta.env.DEV ? (<CustomerDashboardPage />) : (<RequireRole roles={[ROLES.customer]}><CustomerDashboardPage /></RequireRole>), },
      { path: "book", element: <CreateBookingPage /> },
      { path: "history", element: <CustomerHistoryPage /> },
      { path: "track", element: <PublicTrackingPage /> }, // Public Tracking (No login required)
      { path: "track/:trackingNumber", element: <PublicTrackingPage /> }, // Public Tracking (No login required)
      { path: "assistant", element: <AIAssistantPage /> },
    ],
  },

  // Courier app: its own phone-style frame with a bottom tab bar
  {
    path: "courier",
    element: (
      <RequireRole roles={[ROLES.courier]}>
        <CourierLayout />
      </RequireRole>
    ),
    children: [
      { index: true, element: <RunsPage view="active" /> },
      { path: "completed", element: <RunsPage view="completed" /> },
      { path: "shift", element: <ShiftPage /> },
      { path: "deliveries/:id", element: <DeliveryDetailPage /> },
      { path: "deliveries/:id/proof", element: <ProofOfDeliveryPage /> },
    ],
  },

  // Admin routes
  {
    path: "admin",
    element: <AdminLayout />,
    children: [
      { index: true, element: <AdminDashboardPage /> },
      { path: "dispatch", element: <DispatchBoardPage /> },
      { path: "couriers", element: <CourierWorkloadPage /> },
      { path: "reports", element: <ReportsPage /> },
      { path: "users", element: <UserManagementPage /> },
      { path: "pricing", element: <ZonesPricingPage /> },
    ],
  },
]);
