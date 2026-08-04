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
  SellerAvatarUrl?: string;
  BuyerId: string;
  BuyerNome: string;
  BuyerAvatarUrl?: string;
  TotalRvm: number;
  State: TradeState;
  FundedAt?: string;
  ReleasedAt?: string;
  VoucherRedeemed: boolean;
  IsDonation: boolean;
  OnChainTradeId?: string;
  LastTxHash?: string;
}

export interface HelpRequest {
  Id: string;
  ListingId: string;
  AuthorId: string;
  AuthorNome: string;
  AuthorAvatarUrl?: string;
  Mensagem: string;
  State: string;
  CreatedAt: string;
  SelectedTradeId?: string;
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
  modo?: Modo;
  precoMin?: number;
  precoMax?: number;
  doarApenas?: boolean;
  sort?: string;
  q?: string;
}

export interface TradesParams {
  buyerId?: string;
  sellerId?: string;
  page?: number;
}

// Comunidades (Fase 2B).

export type TipoComunidade = "Default" | "User";
export type EixoComunidade = "Geo" | "Interesse" | "Causa";
export type VisibilidadeComunidade = "Open" | "Private";
export type PapelMembro = "Membro" | "Moderador" | "Criador";
export type StatusMembro = "Ativa" | "Bloqueada";

export interface Community {
  Id: string;
  Nome: string;
  Descricao: string;
  Tipo: TipoComunidade;
  Eixo: EixoComunidade;
  Visibilidade: VisibilidadeComunidade;
  Lat?: number;
  Lng?: number;
  Bairro?: string;
  Cidade?: string;
  Estado?: string;
  CriadorId: string;
  CriadorNome: string;
  CriadorAvatarUrl?: string;
  MembrosCount: number;
}

export interface Post {
  Id: string;
  ComunidadeId: string;
  AutorId: string;
  AutorNome: string;
  AutorAvatarUrl?: string;
  Conteudo: string;
  ParentId?: string;
  Path: string;
  Depth: number;
  Status: string;
  OcultadoPor?: string;
  CreatedAt: string;
}

// Comentário de anúncio = mesmo shape do Post (+ ListingId). Reaproveita o <PostThread> no detalhe.
export interface Comment extends Post {
  ListingId: string;
}

export interface Membership {
  Id: string;
  UsuarioId: string;
  UsuarioNome: string;
  UsuarioAvatarUrl?: string;
  ComunidadeId: string;
  Papel: PapelMembro;
  Status: StatusMembro;
  JoinedAt: string;
}

export interface ChatMessage {
  Id: string;
  ComunidadeId: string;
  AutorId: string;
  AutorNome: string;
  AutorAvatarUrl?: string;
  Conteudo: string;
  CreatedAt: string;
}

export interface CreateCommunityBody {
  Nome: string;
  Descricao: string;
  Tipo: TipoComunidade;
  Eixo: EixoComunidade;
  Visibilidade: VisibilidadeComunidade;
  Password?: string;
  Lat?: number;
  Lng?: number;
  Bairro?: string;
  Cidade?: string;
  Estado?: string;
}

export interface CommunityFeedParams {
  raio?: number;
  lat?: number;
  lng?: number;
  eixo?: EixoComunidade;
  page?: number;
}
