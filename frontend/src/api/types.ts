// Tipos TS dos DTOs do backend .NET (PascalCase, conforme o driver Mongo).

export type Kind = "Product" | "Service";
export type Mode = "Trade" | "Resell" | "Donate" | "Volunteer";
export type Visibility = "Community" | "Global" | "Both";
export type TradeState =
  | "Offered"
  | "Funded"
  | "Released"
  | "Disputed"
  | "Refunded"
  | "Cancelled";

export interface Category {
  Id: string;
  Name: string;
  Slug: string;
  Description?: string;
}

export interface FeedItem {
  Id: string;
  Kind: Kind;
  Mode: Mode;
  Title: string;
  PriceRvm: number;
  PrimeiraImagem?: string;
  SellerName: string;
  SellerAvatarUrl?: string;
  City?: string;
  Neighborhood?: string;
  CategoryId: string;
  DistanciaKm?: number;
  Condition?: string;
  UnitType?: string;
  Duration?: number;
  CreatedAt?: string;
}

export interface ProductDetails {
  Condition: string;
  Stock?: number;
}

export interface ServiceDetails {
  UnitType: string;
  Duration?: number;
  VoucherExpiryDays?: number;
}

export interface Listing {
  Id: string;
  Kind: Kind;
  Mode: Mode;
  Title: string;
  Description: string;
  Imagens: string[];
  PriceRvm: number;
  SellerId: string;
  SellerName: string;
  SellerAvatarUrl?: string;
  Lat?: number;
  Lng?: number;
  Neighborhood?: string;
  City?: string;
  PostalCode?: string;
  CategoryId: string;
  CommunityId?: string;
  Visibility: Visibility;
  NftTokenId?: string;
  Status: string;
  CreatedAt?: string;
  ProductDetails?: ProductDetails;
  ServiceDetails?: ServiceDetails;
}

export interface Trade {
  Id: string;
  ListingId: string;
  Mode: Mode;
  Kind: Kind;
  SellerId: string;
  SellerName: string;
  SellerAvatarUrl?: string;
  BuyerId: string;
  BuyerName: string;
  BuyerAvatarUrl?: string;
  TotalRvm: number;
  State: TradeState;
  FundedAt?: string;
  ReleasedAt?: string;
  VoucherRedeemed: boolean;
  IsDonation: boolean;
}

export interface HelpRequest {
  Id: string;
  ListingId: string;
  AuthorId: string;
  AuthorName: string;
  AuthorAvatarUrl?: string;
  Message: string;
  State: string;
  CreatedAt: string;
  SelectedTradeId?: string;
}

// Avaliação pós-troca recebida por um usuário (UF-23).
export interface Review {
  Id: string;
  TradeId: string;
  ReviewerId: string;
  ReviewerName: string;
  Rating: number;
  Comment?: string;
  CreatedAt: string;
}

// Score de reputação público (GET /api/users/{id}/reputation — UF-23).
export interface Reputation {
  UserId: string;
  Points: number;
  Level: string;
  HelpPoints: number;
  DonationsCount: number;
  VolunteerCount: number;
  ReviewsCount: number;
  AvgRating: number;
}

export interface RegisterResult {
  UserId: string;
  NeedsEmailVerification: boolean;
  NeedsPhoneVerification: boolean;
}

export interface LoginResult {
  Token: string;
  UserId: string;
  Name: string;
  Email: string;
  Roles: string[];
}

// Corpos de requisição.

export interface RegisterBody {
  Name: string;
  Email: string;
  Phone: string;
  BirthDate: string; // YYYY-MM-DD
  CouponCode?: string;
}

export interface CreateListingBody {
  Kind: Kind;
  Mode: Mode;
  Title: string;
  Description: string;
  Imagens: string[];
  PriceRvm: number;
  Lat?: number;
  Lng?: number;
  Neighborhood?: string;
  City?: string;
  PostalCode?: string;
  CategoryId: string;
  CommunityId?: string;
  Visibility: Visibility;
  Condition?: string;
  Stock?: number;
  UnitType?: string;
  Duration?: number;
  VoucherExpiryDays?: number;
}

export interface FeedParams {
  radius?: number;
  lat?: number;
  lng?: number;
  kind?: Kind;
  categoryId?: string;
  communityId?: string;
  page?: number;
  mode?: Mode;
  priceMin?: number;
  priceMax?: number;
  donationOnly?: boolean;
  sort?: string;
  q?: string;
  sellerIds?: string;
}

export interface TradesParams {
  buyerId?: string;
  sellerId?: string;
  page?: number;
}

// Comunidades (Fase 2B).

export type CommunityType = "Default" | "User";
export type CommunityAxis = "Geo" | "Interest" | "Cause";
export type CommunityVisibility = "Open" | "Private";
export type MembershipRole = "Member" | "Moderator" | "Creator";
export type MembershipStatus = "Active" | "Blocked";

export interface Community {
  Id: string;
  Name: string;
  Description: string;
  Type: CommunityType;
  Axis: CommunityAxis;
  Visibility: CommunityVisibility;
  Lat?: number;
  Lng?: number;
  Neighborhood?: string;
  City?: string;
  State?: string;
  CreatorId: string;
  CreatorName: string;
  CreatorAvatarUrl?: string;
  MembersCount: number;
}

export interface Post {
  Id: string;
  CommunityId: string;
  AutorId: string;
  AuthorName: string;
  AutorAvatarUrl?: string;
  Content: string;
  ParentId?: string;
  Path: string;
  Depth: number;
  Status: string;
  OcultadoPor?: string;
  CreatedAt: string;
  ChildrenCount?: number;
}

// Comentário de anúncio = mesmo shape do Post (+ ListingId). Reaproveita o <PostThread> no detalhe.
export interface Comment extends Post {
  ListingId: string;
}

export interface Membership {
  Id: string;
  UserId: string;
  UserName: string;
  UserAvatarUrl?: string;
  CommunityId: string;
  Role: MembershipRole;
  Status: MembershipStatus;
  JoinedAt: string;
}

// GET /api/communities/mine — vínculo do usuário com cada comunidade dele.
export interface MyCommunity {
  Community: Community;
  Role: MembershipRole;
  JoinedAt: string;
}

export interface ChatMessage {
  Id: string;
  CommunityId: string;
  AutorId: string;
  AuthorName: string;
  AutorAvatarUrl?: string;
  Content: string;
  CreatedAt: string;
}

export interface CreateCommunityBody {
  Name: string;
  Description: string;
  Type: CommunityType;
  Axis: CommunityAxis;
  Visibility: CommunityVisibility;
  Password?: string;
  Lat?: number;
  Lng?: number;
  Neighborhood?: string;
  City?: string;
  State?: string;
}

export interface CommunityFeedParams {
  radius?: number;
  lat?: number;
  lng?: number;
  axis?: CommunityAxis;
  page?: number;
}

// Moderação (UF-24/25).

export type ReportTarget = "Listing" | "Post" | "User" | "Comment";
export type ReportReason = "Spam" | "Inappropriate" | "Scam" | "Other";
export type ReportStatus = "Open" | "Resolved";
export type ResolutionAction = "Dismissed" | "Warned" | "Banned";

export interface Report {
  Id: string;
  ReporterId: string;
  ReporterName: string;
  TargetType: ReportTarget;
  TargetId: string;
  Reason: ReportReason;
  Details?: string;
  Status: ReportStatus;
  Action?: ResolutionAction;
  ResolvedBy?: string;
  ResolutionNote?: string;
  ResolvedAt?: string;
  CreatedAt: string;
}

// Cupom on-chain (UF-29).

export type CouponStatus = "Active" | "Revoked";

export interface Coupon {
  Id: string;
  Code: string;
  AmountRvm: number;
  MaxUses: number;
  Expiry?: string;
  Status: CouponStatus;
  CreatedBy: string;
  CreatedAt: string;
}

export interface CreateCouponBody {
  AmountRvm: number;
  MaxUses: number;
  Expiry?: string;
  Code?: string;
}

// Admin — parâmetros runtime (UF-30).

export type ParameterType = "long" | "int" | "decimal" | "bool";

export interface AdminParameter {
  Key: string;
  Label: string;
  Type: ParameterType;
  Value: number | boolean | string;
}

// Admin — demurrage (preview/run/histórico).

export interface DemurragePreview {
  RateBps: number;
  FloorRvm: number;
  AccountsAffected: number;
  Skipped: number;
  TotalBurnedRaw: string;
  TotalBurnedRvm: number;
}

export interface DemurrageRun {
  Id: string;
  RunAt: string;
  RateBps: number;
  FloorRvm: number;
  AccountsAffected: number;
  TotalBurnedRaw: string;
  TotalBurnedRvm: number;
  Skipped: number;
  ExecutedBy: string;
  Preview: boolean;
}

// Admin — seed do catálogo demo (POST /api/dev/seed-catalog, PascalCase).

export interface SeedCatalogResult {
  Categorias: number;
  Produtos: number;
  Servicos: number;
  Listings: number;
  Erros: number;
}

// Taxa de estimativa BRL simbólica (PricingIntelligence) — PascalCase como /api/pricing/*.
export interface BrlRate {
  BrlRate: number;
  Currency: "BRL";
  Disclaimer: string;
}

// Referência de preço justo por categoria (PricingIntelligence — GET /api/pricing, leitura anônima).
// PascalCase como os demais DTOs. Mediana comunitária + semente BRL + IPCA + Ollama.
export interface PriceReference {
  CategoryId: string;
  CategorySlug?: string;
  RvmMedian: number;
  SampleCount: number;
  BrlRate: number;
  BrlReference?: number;
  FairSuggestionRvm: number;
  LastIpcRate?: number;
  LastIpcMonth?: string;
  SourcesUsed: string;
  UpdatedAt: string;
}

// Notificações pessoais (UF-32; GET /api/notifications, gate Verified, ownership via token).
export type NotificationType =
  | "EscrowUpdate"
  | "Offer"
  | "Transfer"
  | "Post"
  | "Chat"
  | "Donation"
  | "Price"
  | "Help"
  | "System";

export interface AppNotification {
  Id: string;
  Type: NotificationType | string;
  Title: string;
  Body: string;
  Payload?: string | null;
  Read: boolean;
  ReadAt?: string | null;
  CreatedAt: string;
}
