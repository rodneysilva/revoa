// Tipos TS dos DTOs do backend .NET (PascalCase, conforme o driver Mongo).

export type Kind = "Product" | "Service";
export type Modo = "Trocar" | "Repassar" | "Doar" | "Voluntariar";
export type Visibilidade = "Comunidade" | "Global" | "Ambos";
export type TradeState =
  | "Ofertada"
  | "Financiada"
  | "Liberada"
  | "Disputada"
  | "Reembolsada"
  | "Cancelada";

export interface Category {
  Id: string;
  Nome: string;
  Slug: string;
  Descricao?: string;
}

export interface FeedItem {
  Id: string;
  Kind: Kind;
  Modo: Modo;
  Titulo: string;
  PrecoRvm: number;
  PrimeiraImagem?: string;
  VendedorNome: string;
  VendedorAvatarUrl?: string;
  Cidade?: string;
  Bairro?: string;
  CategoriaId: string;
  DistanciaKm?: number;
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
  Modo: Modo;
  Titulo: string;
  Descricao: string;
  Imagens: string[];
  PrecoRvm: number;
  VendedorId: string;
  VendedorNome: string;
  VendedorAvatarUrl?: string;
  Lat?: number;
  Lng?: number;
  Bairro?: string;
  Cidade?: string;
  Cep?: string;
  CategoriaId: string;
  ComunidadeId?: string;
  Visibilidade: Visibilidade;
  NftTokenId?: string;
  Status: string;
  ProductDetails?: ProductDetails;
  ServiceDetails?: ServiceDetails;
}

export interface Trade {
  Id: string;
  ListingId: string;
  Modo: Modo;
  Kind: Kind;
  SellerId: string;
  SellerNome: string;
  BuyerId: string;
  BuyerNome: string;
  TotalRvm: number;
  State: TradeState;
  FundedAt?: string;
  ReleasedAt?: string;
  IsDonation: boolean;
}

export interface RegisterResult {
  UserId: string;
  NeedsEmailVerification: boolean;
  NeedsPhoneVerification: boolean;
}

export interface LoginResult {
  Token: string;
  UserId: string;
  Nome: string;
  Email: string;
}

// Corpos de requisição.

export interface RegisterBody {
  Nome: string;
  Email: string;
  Telefone: string;
  BirthDate: string; // YYYY-MM-DD
  CouponCode?: string;
}

export interface CreateListingBody {
  Kind: Kind;
  Modo: Modo;
  Titulo: string;
  Descricao: string;
  Imagens: string[];
  PrecoRvm: number;
  Lat?: number;
  Lng?: number;
  Bairro?: string;
  Cidade?: string;
  Cep?: string;
  CategoriaId: string;
  ComunidadeId?: string;
  Visibilidade: Visibilidade;
  Condition?: string;
  Stock?: number;
  UnitType?: string;
  Duration?: number;
  VoucherExpiryDays?: number;
}

export interface FeedParams {
  raio?: number;
  lat?: number;
  lng?: number;
  kind?: Kind;
  categoriaId?: string;
  comunidadeId?: string;
  page?: number;
}

export interface TradesParams {
  buyerId?: string;
  sellerId?: string;
  page?: number;
}
