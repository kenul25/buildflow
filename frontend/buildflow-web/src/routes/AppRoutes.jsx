import { Navigate, Route, Routes } from 'react-router-dom'
import DashboardLayout from '../layouts/DashboardLayout.jsx'
import PublicLayout from '../layouts/PublicLayout.jsx'
import DashboardPage from '../pages/DashboardPage.jsx'
import AdminDashboardPage from '../pages/AdminDashboardPage.jsx'
import LandingPage from '../pages/LandingPage.jsx'
import LoginPage from '../pages/LoginPage.jsx'
import NotFoundPage from '../pages/NotFoundPage.jsx'
import RegisterPage from '../pages/RegisterPage.jsx'
import UnauthorizedPage from '../pages/UnauthorizedPage.jsx'
import ProtectedRoute from './ProtectedRoute.jsx'
import { ConstructionListPage, ConstructionDetailsPage, ConstructionFormPage } from '../features/construction/ConstructionPage.jsx'
import InventoryPage from '../features/inventory/InventoryPage.jsx'
import SuppliersPage from '../features/procurement/SuppliersPage.jsx'
import SupplierMaterialsPage from '../features/procurement/SupplierMaterialsPage.jsx'

import ProcurementComparisonPage from '../features/procurement/ProcurementComparisonPage.jsx'




import ProcurementRecordsPage from '../features/procurement/ProcurementRecordsPage.jsx'
import SchedulingPage from '../features/scheduling/SchedulingPage.jsx'
import WorkflowsPage from '../features/scheduling/WorkflowsPage.jsx'

export default function AppRoutes() {
  return (
    <Routes>
      <Route element={<PublicLayout />}>
        <Route index element={<LandingPage />} />
        <Route path="login" element={<LoginPage />} />
        <Route path="register" element={<RegisterPage />} />
        <Route path="unauthorized" element={<UnauthorizedPage />} />
      </Route>

      <Route element={<ProtectedRoute />}>
        <Route element={<DashboardLayout />}>
          <Route path="dashboard" element={<DashboardPage />} />
          <Route path="inventory" element={<InventoryPage />} /><Route path="scheduling" element={<SchedulingPage />} /><Route path="workflows" element={<WorkflowsPage />} />

          <Route path="suppliers" element={<ProtectedRoute roles={['ProcurementOfficer', 'ProjectManager', 'Administrator']} />}><Route index element={<SuppliersPage />} /></Route>
          <Route path="supplier-materials" element={<ProtectedRoute roles={['ProcurementOfficer', 'ProjectManager', 'Administrator']} />}><Route index element={<SupplierMaterialsPage />} /></Route>
          <Route path="quotations" element={<ProtectedRoute roles={['ProcurementOfficer', 'ProjectManager', 'Administrator']} />}><Route index element={<ProcurementRecordsPage kind="Quotations" />} /></Route>
          <Route path="procurement/compare" element={<ProtectedRoute roles={['ProcurementOfficer', 'ProjectManager', 'Administrator']} />}><Route index element={<ProcurementComparisonPage />} /></Route>
          <Route path="purchase-requests" element={<ProtectedRoute roles={['ProcurementOfficer', 'ProjectManager', 'Administrator']} />}><Route index element={<ProcurementRecordsPage kind="PurchaseRequests" />} /></Route>
          <Route path="purchase-orders" element={<ProtectedRoute roles={['ProcurementOfficer', 'ProjectManager', 'Administrator']} />}><Route index element={<ProcurementRecordsPage kind="PurchaseOrders" />} /></Route>
          <Route path="deliveries" element={<ProtectedRoute roles={['ProcurementOfficer', 'ProjectManager', 'Administrator']} />}><Route index element={<ProcurementRecordsPage kind="Deliveries" />} /></Route>

          <Route path="construction/:kind" element={<ConstructionListPage />} />
          <Route path="construction/:kind/:id" element={<ConstructionDetailsPage />} />

          <Route element={<ProtectedRoute roles={['Administrator', 'ProjectManager']} />}>
            <Route path="construction/:kind/new" element={<ConstructionFormPage />} />
            <Route path="construction/:kind/:id/edit" element={<ConstructionFormPage />} />
          </Route>

          <Route element={<ProtectedRoute roles={['Administrator']} />}>
            <Route path="admin" element={<AdminDashboardPage />} />
          </Route>
        </Route>
      </Route>

      <Route path="home" element={<Navigate to="/dashboard" replace />} />
      <Route path="*" element={<NotFoundPage />} />
    </Routes>
  )
 }
