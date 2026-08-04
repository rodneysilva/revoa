import { Route, Routes } from "react-router-dom";
import { Layout } from "./components/Layout";
import { ProtectedRoute } from "./components/ProtectedRoute";
import { ErrorBoundary } from "./components/ErrorBoundary";
import { LandingPage } from "./pages/LandingPage";
import { FeedPage } from "./pages/FeedPage";
import { ListingDetailPage } from "./pages/ListingDetailPage";
import { LoginPage } from "./pages/LoginPage";
import { RegisterPage } from "./pages/RegisterPage";
import { CreateListingPage } from "./pages/CreateListingPage";
import { TradeTrackerPage } from "./pages/TradeTrackerPage";
import { CommunityPage } from "./pages/CommunityPage";
import { WalletPage } from "./pages/WalletPage";
import { NotificationsPage } from "./pages/NotificationsPage";
import { ProfilePage } from "./pages/ProfilePage";
import { NotFoundPage } from "./pages/NotFoundPage";

export default function App() {
  return (
    <ErrorBoundary>
      <Routes>
        <Route element={<Layout />}>
          <Route path="/" element={<LandingPage />} />
          <Route path="/feed" element={<FeedPage />} />
          <Route path="/listings/:id" element={<ListingDetailPage />} />
          <Route path="/login" element={<LoginPage />} />
          <Route path="/register" element={<RegisterPage />} />
          <Route path="/trades" element={<TradeTrackerPage />} />
          <Route path="/community" element={<CommunityPage />} />
          {/* Ações exigem login + verificação */}
          <Route element={<ProtectedRoute />}>
            <Route path="/listings/new" element={<CreateListingPage />} />
          </Route>
          <Route path="/wallet" element={<WalletPage />} />
          <Route path="/notifications" element={<NotificationsPage />} />
          <Route path="/profile" element={<ProfilePage />} />
          <Route path="*" element={<NotFoundPage />} />
        </Route>
      </Routes>
    </ErrorBoundary>
  );
}
