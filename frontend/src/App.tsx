import { Route, Routes } from "react-router-dom";
import { Layout } from "./components/Layout";
import { ProtectedRoute } from "./components/ProtectedRoute";
import { ErrorBoundary } from "./components/ErrorBoundary";
import { LandingPage } from "./pages/LandingPage";
import { FeedPage } from "./pages/FeedPage";
import { ExplorePage } from "./pages/ExplorePage";
import { ListingDetailPage } from "./pages/ListingDetailPage";
import { LoginPage } from "./pages/LoginPage";
import { RegisterPage } from "./pages/RegisterPage";
import { CreateListingPage } from "./pages/CreateListingPage";
import { TradeTrackerPage } from "./pages/TradeTrackerPage";
import { CommunityPage } from "./pages/CommunityPage";
import { CommunityDetailPage } from "./pages/CommunityDetailPage";
import { SavedPage } from "./pages/SavedPage";
import { AdminReportsPage } from "./pages/AdminReportsPage";
import { AdminCouponsPage } from "./pages/AdminCouponsPage";
import { AdminCommunitiesPage } from "./pages/AdminCommunitiesPage";
import { AdminUsersPage } from "./pages/AdminUsersPage";
import { AdminDashboardPage } from "./pages/AdminDashboardPage";
import { WalletPage } from "./pages/WalletPage";
import { NotificationsPage } from "./pages/NotificationsPage";
import { ProfilePage } from "./pages/ProfilePage";
import { UserPage } from "./pages/UserPage";
import { TransparencyPage } from "./pages/TransparencyPage";
import { TermsPage } from "./pages/TermsPage";
import { NotFoundPage } from "./pages/NotFoundPage";

export default function App() {
  return (
    <ErrorBoundary>
      <Routes>
        <Route element={<Layout />}>
          <Route path="/" element={<LandingPage />} />
          <Route path="/feed" element={<FeedPage />} />
          <Route path="/explore" element={<ExplorePage />} />
          <Route path="/listings/:id" element={<ListingDetailPage />} />
          <Route path="/users/:id" element={<UserPage />} />
          <Route path="/login" element={<LoginPage />} />
          <Route path="/register" element={<RegisterPage />} />
          <Route path="/community" element={<CommunityPage />} />
          <Route path="/community/:id" element={<CommunityDetailPage />} />
          <Route path="/admin/reports" element={<AdminReportsPage />} />
          <Route path="/admin/coupons" element={<AdminCouponsPage />} />
          <Route path="/admin/users" element={<AdminUsersPage />} />
          <Route path="/admin/communities" element={<AdminCommunitiesPage />} />
          <Route path="/admin" element={<AdminDashboardPage />} />
          {/* Páginas que exigem login */}
          <Route element={<ProtectedRoute />}>
            <Route path="/listings/new" element={<CreateListingPage />} />
            <Route path="/wallet" element={<WalletPage />} />
            <Route path="/notifications" element={<NotificationsPage />} />
            <Route path="/profile" element={<ProfilePage />} />
            <Route path="/trades" element={<TradeTrackerPage />} />
            <Route path="/saved" element={<SavedPage />} />
          </Route>
          <Route path="/transparency" element={<TransparencyPage />} />
          <Route path="/terms" element={<TermsPage />} />
          <Route path="*" element={<NotFoundPage />} />
        </Route>
      </Routes>
    </ErrorBoundary>
  );
}
