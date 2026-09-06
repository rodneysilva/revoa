// ═══════════════════════════════════════════════════════════════
// migrate-rename-fields.js — migração BSON PT→EN (ADR-0017)
// ═══════════════════════════════════════════════════════════════
//
// Renomeia os campos persistidos em PascalCase-PT para os equivalentes
// EN do contrato público (identificadores em inglês — ver ADR-0017).
// Enums persistem int32 (só os NOMES C# mudaram) — nada a fazer por eles.
//
// IDEMPOTENTE: $rename é no-op para campos de origem ausentes; rodar duas
// vezes não altera nada (modifiedCount = 0 na segunda execução).
//
// Uso (mongosh, apontando para o banco do ambiente alvo):
//   mongosh "mongodb://localhost:27017/revoa?replicaSet=rs0" scripts/migrate-rename-fields.js
//
// Rodar ANTES de subir a app com o código renomeado (docs legados passam a
// ser lidos pelos nomes EN). Faça backup/snapshot antes, como em qualquer
// migração de esquema.

const RENAMES = {
  Listings: {
    Titulo: "Title",
    Descricao: "Description",
    PrecoRvm: "PriceRvm",
    VendedorId: "SellerId",
    VendedorNome: "SellerName",
    VendedorAvatarUrl: "SellerAvatarUrl",
    CategoriaId: "CategoryId",
    ComunidadeId: "CommunityId",
    Modo: "Mode",
    Visibilidade: "Visibility",
    // Location embutido (notação de caminho).
    "Location.Bairro": "Location.Neighborhood",
    "Location.Cidade": "Location.City",
    "Location.Cep": "Location.PostalCode",
  },
  Categories: {
    Nome: "Name",
    Descricao: "Description",
  },
  Comments: {
    AutorNome: "AuthorName",
    Conteudo: "Content",
  },
  Communities: {
    Nome: "Name",
    Descricao: "Description",
    Tipo: "Type",
    Eixo: "Axis",
    Visibilidade: "Visibility",
    Bairro: "Neighborhood",
    Cidade: "City",
    Estado: "State",
    CriadorId: "CreatorId",
    CriadorNome: "CreatorName",
    CriadorAvatarUrl: "CreatorAvatarUrl",
  },
  Memberships: {
    UsuarioNome: "UserName",
    UsuarioId: "UserId",
    UsuarioAvatarUrl: "UserAvatarUrl",
    ComunidadeId: "CommunityId",
    Papel: "Role",
  },
  Posts: {
    ComunidadeId: "CommunityId",
    AutorNome: "AuthorName",
    Conteudo: "Content",
  },
  ChatMessages: {
    ComunidadeId: "CommunityId",
    AutorNome: "AuthorName",
    Conteudo: "Content",
  },
  Trades: {
    Modo: "Mode",
    SellerNome: "SellerName",
    BuyerNome: "BuyerName",
  },
  HelpRequests: {
    AuthorNome: "AuthorName",
    Mensagem: "Message",
  },
  Users: {
    Nome: "Name",
    Telefone: "Phone",
  },
  Reports: {
    ReporterNome: "ReporterName",
  },
  Notifications: {
    Titulo: "Title",
  },
  PriceReferences: {
    CategoriaId: "CategoryId",
    CategoriaSlug: "CategorySlug",
  },
  Reviews: {
    ReviewerNome: "ReviewerName",
  },
  // Sem renomeações: Accounts, Coupons, DemurrageRuns, Reputations,
  // SystemParameters, PushSubscriptions.
};

let totalModified = 0;
for (const [coll, fields] of Object.entries(RENAMES)) {
  const res = db.getCollection(coll).updateMany({}, { $rename: fields });
  totalModified += res.modifiedCount;
  print(`${coll}: matched=${res.matchedCount} modified=${res.modifiedCount} (${Object.keys(fields).length} campos)`);
}
print(`\nConcluído — ${totalModified} documento(s) modificados no total.`);
