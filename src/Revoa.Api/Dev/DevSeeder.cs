using MongoDB.Bson;
using MongoDB.Driver;
using Nethereum.Signer;
using Revoa.Abstractions;
using Revoa.Account.Domain.Aggregates.AccountAggregate;
using Revoa.Catalog.Domain.Aggregates.CategoryAggregate;
using Revoa.Catalog.Domain.Aggregates.CommentAggregate;
using Revoa.Catalog.Domain.Aggregates.ListingAggregate;
using Revoa.Catalog.Domain.Aggregates.ListingLikeAggregate;
using Revoa.Catalog.Domain.Aggregates.SavedListingAggregate;
using Revoa.Catalog.Domain.Repositories;
using Revoa.Community.Domain.Aggregates.CommunityAggregate;
using Revoa.Community.Domain.Aggregates.MembershipAggregate;
using Revoa.Community.Domain.Aggregates.PostAggregate;
using Revoa.Community.Domain.Aggregates.PostLikeAggregate;
using Revoa.Community.Domain.Aggregates.SavedPostAggregate;
using Revoa.Exchange.Domain.Aggregates.TradeAggregate;
using Revoa.Identity.Domain.Aggregates.UserAggregate;
using Revoa.Reputation.Domain.Aggregates.ReputationAggregate;
using Revoa.Reputation.Domain.Aggregates.ReviewAggregate;

// Aliases p/ desambiguar tipos de domínio que colidem: 'User' com a propriedade ControllerBase.User,
// 'Reputation' com o namespace Revoa.Reputation. 'Account' é qualificado na chamada (Revoa.Account).
using AppUser = Revoa.Identity.Domain.Aggregates.UserAggregate.User;
using ReputationScore = Revoa.Reputation.Domain.Aggregates.ReputationAggregate.Reputation;

namespace Revoa.Api.Dev;

// Resultado do seed (PascalCase — serializa igual ao corpo que o FE já consome).
public sealed record DevSeedResult(
    int Categorias,
    long Removidos,
    int Usuarios,
    int Carteiras,
    int Produtos,
    int Servicos,
    int Listings,
    int Comunidades,
    int Memberships,
    int Posts,
    int Reviews,
    int Reputacoes,
    int Erros,
    int CurtidasPosts = 0,
    int CurtidasAnuncios = 0,
    int ComentariosAnuncios = 0,
    int Salvos = 0,
    int Trocas = 0);

// Popula o ambiente de desenvolvimento com um ecossistema de demonstração VIVO e interconectado:
// usuários mock reais (com carteira), catálogo vinculado a esses usuários, comunidades com
// membros/posts e interações (reviews + reputação). Os dados vêm do DevSeedData; o DevController
// só expõe o endpoint (gate de ambiente + Policy=Admin).
public sealed class DevSeeder
{
    private readonly ICategoryRepository _categories;
    private readonly IMongoDatabase _db;
    private readonly ILogger<DevSeeder> _logger;

    public DevSeeder(
        ICategoryRepository categories,
        IMongoDatabase db,
        ILogger<DevSeeder> logger)
    {
        _categories = categories;
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// (Re)semeia o ambiente DEV completo: 10 usuários mock (+carteiras), catálogo vinculado aos
    /// mocks, 5 comunidades com membros e posts, e ~22 reviews (+reputação acumulada). Idempotente
    /// (limpa tudo por chaves determinísticas antes de re-inserir).
    /// </summary>
    public async Task<DevSeedResult> SeedAsync(CancellationToken ct)
    {
        // Seed fixa → variedade temporal entre itens + dados reproduzíveis a cada execução do seed.
        var rnd = new Random(20260806);

        // 1) Garante categorias (produto + serviço) — lista canônica do módulo Catalog. Idempotente.
        var categoriasCriadas = 0;
        foreach (var (name, slug, description) in CategorySeed.All)
        {
            if (await _categories.GetBySlugAsync(slug, ct) is null)
            {
                await _categories.AddAsync(Category.Create(name, slug, description), ct);
                categoriasCriadas++;
            }
        }

        // Mapeia slug → CategoryId uma única vez (evita N queries no loop de listings).
        var catBySlug = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        foreach (var c in CategorySeed.All)
        {
            var found = await _categories.GetBySlugAsync(c.Slug, ct);
            if (found is not null)
            {
                catBySlug[c.Slug] = found.Id;
            }
        }

        // 2) Limpa mocks antigos por chaves determinísticas (idempotência total).
        var removidos = await CleanDemoDataAsync(ct);

        // 3) Usuários mock (10) + carteiras (Accounts).
        var mocks = DevSeedData.MockUsers();
        var usuarios = await SeedUsersAsync(mocks, ct);
        var carteiras = await SeedAccountsAsync(mocks, ct);

        // 3.b) Garante o usuário REAL do owner (admin): se a conta já existe (registro
        // normal), preserva o _id e só completa a carteira se faltar — nunca apaga.
        await SeedAdminAsync(ct);

        // 4) Catálogo vinculado aos mocks (SellerId rotaciona entre os 10 mocks).
        var (produtos, servicos, erros, listingIds) = await SeedListingsAsync(mocks, catBySlug, rnd, ct);

        // 5) Comunidades (5, com capa) + memberships + posts (com respostas,
        //    curtidas e salvos) + anúncios da comunidade.
        var comm = await SeedCommunitiesAsync(mocks, catBySlug, rnd, listingIds, ct);

        // 6) Interações nos anúncios: curtidas, comentários (+ resposta do
        //    vendedor) e salvos — o feed nasce "vivo", não zerado.
        var (curtAnuncios, comentarios, salvAnuncios) = await SeedInteractionsAsync(mocks, listingIds, rnd, ct);

        // 7) Reviews (~22) + reputações acumuladas por usuário.
        var (reviews, reputacoes) = await SeedReviewsAndReputationAsync(mocks, rnd, ct);

        // 8) Trocas em vários estados (o coração do produto não nasce zerado):
        //    concluídas, em andamento, doação e disputa, sobre anúncios do seed.
        var trocas = await SeedTradesAsync(mocks, listingIds, rnd, ct);

        return new DevSeedResult(
            Categorias: categoriasCriadas,
            Removidos: removidos,
            Usuarios: usuarios,
            Carteiras: carteiras,
            Produtos: produtos,
            Servicos: servicos,
            Listings: produtos + servicos,
            Comunidades: comm.Comunidades,
            Memberships: comm.Memberships,
            Posts: comm.Posts,
            Reviews: reviews,
            Reputacoes: reputacoes,
            Erros: erros,
            CurtidasPosts: comm.Curtidas,
            CurtidasAnuncios: curtAnuncios,
            ComentariosAnuncios: comentarios,
            Salvos: comm.Salvos + salvAnuncios,
            Trocas: trocas);
    }

    // --- Limpeza idempotente: remove todos os mocks pelas chaves determinísticas (10 usuários +
    //     5 comunidades). Listings continuam pelo prefixo literal "[Demo]" na Descrição.
    //     BsonBinaryData explícito é obrigatório no GuidRepresentationMode V3 (BsonDocument sem
    //     representação implícita de Guid quebraria o match do filtro).
    private async Task<long> CleanDemoDataAsync(CancellationToken ct)
    {
        var userBin = DevSeedData.DemoUserIds.Select(g => new BsonBinaryData(g, GuidRepresentation.Standard)).ToList();
        var commBin = DevSeedData.DemoCommunityIds.Select(g => new BsonBinaryData(g, GuidRepresentation.Standard)).ToList();
        var bf = Builders<BsonDocument>.Filter;

        var listings = _db.GetCollection<BsonDocument>("Listings");
        var demoFilter = bf.Regex(
            "Description", new BsonRegularExpression("^" + System.Text.RegularExpressions.Regex.Escape(DevSeedData.DemoPrefix)));
        var removed = (await listings.DeleteManyAsync(demoFilter, ct)).DeletedCount;

        removed += await DeleteByAsync("Users", bf.In("_id", userBin));
        removed += await DeleteByAsync("Accounts", bf.In("UserId", userBin));
        removed += await DeleteByAsync("Communities", bf.In("_id", commBin));
        removed += await DeleteByAsync("Memberships", bf.In("CommunityId", commBin));
        removed += await DeleteByAsync("Posts", bf.In("CommunityId", commBin));
        removed += await DeleteByAsync("Reviews", bf.In("ReviewerId", userBin));
        removed += await DeleteByAsync("Reputations", bf.In("UserId", userBin));

        // Interações dos mocks: curtidas/salvos têm UserId; comentários do seed
        // são autorados SÓ por mocks (AutorId) — comentários de pessoas reais em
        // anúncios demo são preservados.
        removed += await DeleteByAsync("PostLikes", bf.In("UserId", userBin));
        removed += await DeleteByAsync("ListingLikes", bf.In("UserId", userBin));
        removed += await DeleteByAsync("Comments", bf.In("AutorId", userBin));
        removed += await DeleteByAsync("SavedPosts", bf.In("UserId", userBin));
        removed += await DeleteByAsync("SavedListings", bf.In("UserId", userBin));
        removed += await DeleteByAsync("Trades", bf.Or(bf.In("SellerId", userBin), bf.In("BuyerId", userBin)));
        return removed;

        async Task<long> DeleteByAsync(string coll, FilterDefinition<BsonDocument> filter)
        {
            var r = await _db.GetCollection<BsonDocument>(coll).DeleteManyAsync(filter, ct);
            return r.DeletedCount;
        }
    }

    // --- Usuários mock: 10 docs em Users, Status=Active, e-mail+telefone verificados, _id
    //     determinístico. Cria via factory + DevActivate (estrutura idêntica à dos repositórios),
    //     serializa p/ BsonDocument e sobrescreve o _id pela chave determinística (idempotência).
    private async Task<int> SeedUsersAsync(IReadOnlyList<DevSeedData.MockUser> mocks, CancellationToken ct)
    {
        var docs = new List<BsonDocument>(mocks.Count);
        foreach (var m in mocks)
        {
            var user = AppUser.Create(m.Name, m.Email, m.Phone, idadeOk: true);
            // DEV only: bypassa a dupla verificação (e-mail + telefone) e ativa o usuário.
            user.DevActivate();
            var doc = user.ToBsonDocument();
            doc["_id"] = new BsonBinaryData(m.Id, GuidRepresentation.Standard);
            docs.Add(doc);
        }

        await _db.GetCollection<BsonDocument>("Users").InsertManyAsync(docs, cancellationToken: ct);
        return docs.Count;
    }

    // --- Carteiras (Accounts): gera EOA Nethereum aleatória por usuário (off-chain, perfil DEV).    //     Random é OK — a Account é limpa por UserId antes de re-inserir (idempotente).
    private async Task<int> SeedAccountsAsync(IReadOnlyList<DevSeedData.MockUser> mocks, CancellationToken ct)
    {
        var docs = new List<BsonDocument>(mocks.Count);
        foreach (var m in mocks)
        {
            var ecKey = EthECKey.GenerateKey();
            var privateKey = ecKey.GetPrivateKey(); // já vem com prefixo 0x
            var walletAddress = new Nethereum.Web3.Accounts.Account(privateKey).Address;

            var account = UserAccount.Create(m.Id, walletAddress, privateKey);
            docs.Add(account.ToBsonDocument());
        }

        await _db.GetCollection<BsonDocument>("Accounts").InsertManyAsync(docs, cancellationToken: ct);
        return docs.Count;
    }

    // --- Usuário REAL do owner (admin): email é o allowlist ADMIN_EMAILS, então qualquer
    //     login já recebe a role Admin. Upsert por e-mail — a conta registrada normalmente
    //     (com LoginCodes usados etc.) é preservada; só ganha carteira se não tiver.
    private const string AdminEmail = "rodneydocarmo@gmail.com";
    private static readonly Guid AdminSeedId = new("aabbccdd-00aa-4000-8000-0000000000aa");

    private async Task SeedAdminAsync(CancellationToken ct)
    {
        var users = _db.GetCollection<BsonDocument>("Users");
        var bf = Builders<BsonDocument>.Filter;

        var existing = await users.Find(bf.Eq("Email", AdminEmail)).FirstOrDefaultAsync(ct);
        Guid adminId;
        if (existing is null)
        {
            var admin = AppUser.Create("Rodney do Carmo Silva", AdminEmail, "+5511999990000", idadeOk: true);
            admin.DevActivate();
            var doc = admin.ToBsonDocument();
            doc["_id"] = new BsonBinaryData(AdminSeedId, GuidRepresentation.Standard);
            await users.InsertOneAsync(doc, cancellationToken: ct);
            adminId = AdminSeedId;
            _logger.LogInformation("Seed: usuário admin {Email} criado", AdminEmail);
        }
        else
        {
            adminId = existing["_id"].AsBsonBinaryData.ToGuid();
        }

        var accounts = _db.GetCollection<BsonDocument>("Accounts");
        var temCarteira = await accounts.Find(bf.Eq("UserId", adminId)).AnyAsync(ct);
        if (!temCarteira)
        {
            var ecKey = EthECKey.GenerateKey();
            var privateKey = ecKey.GetPrivateKey();
            var walletAddress = new Nethereum.Web3.Accounts.Account(privateKey).Address;
            await accounts.InsertOneAsync(
                UserAccount.Create(adminId, walletAddress, privateKey).ToBsonDocument(),
                cancellationToken: ct);
            _logger.LogInformation("Seed: carteira criada para o admin");
        }
    }

    // --- Catálogo (~100 produtos + 57 serviços) vinculado aos mocks: SellerId/Nome/AvatarUrl
    //     rotacionam entre os 10 usuários. CreatedAt espalhado nos últimos 30 dias (feed variado).
    //     Retorna os anúncios criados (Id + vendedor) para a etapa de interações.
    private async Task<(int produtos, int servicos, int erros, List<(Guid Id, Guid SellerId)> criados)> SeedListingsAsync(
        IReadOnlyList<DevSeedData.MockUser> mocks,
        IReadOnlyDictionary<string, Guid> catBySlug,
        Random rnd,
        CancellationToken ct)
    {
        var all = DevSeedData.BuildProducts().Concat(DevSeedData.BuildServices()).ToList();
        var places = DevSeedData.Places();
        var docs = new List<BsonDocument>(all.Count);
        var criados = new List<(Guid Id, Guid SellerId)>(all.Count);
        var produtos = 0;
        var servicos = 0;
        var erros = 0;

        for (var i = 0; i < all.Count; i++)
        {
            var seed = all[i];
            if (!catBySlug.TryGetValue(seed.CategorySlug, out var catId))
            {
                erros++;
                _logger.LogWarning("Seed pulou listing (categoria ausente): {Slug}", seed.CategorySlug);
                continue;
            }

            try
            {
                var seller = mocks[i % mocks.Count];
                var place = places[i % places.Count];
                var local = Location.Create(place.Lat, place.Lng, place.Neighborhood, place.City, place.PostalCode);

                ProductDetails? pd = seed.Condition is null ? null : ProductDetails.Create(seed.Condition.Value, 1);
                ServiceDetails? sd = seed.UnitType is null
                    ? null
                    : ServiceDetails.Create(seed.UnitType.Value, seed.Duration, 30);

                var listing = Listing.Create(
                    kind: seed.Kind,
                    mode: seed.Mode,
                    title: seed.Title,
                    description: DevSeedData.DemoPrefix + " " + seed.Description,
                    images: new List<string> { $"https://picsum.photos/seed/revoa-{i}/600/400" },
                    priceRvm: seed.PriceRvm,
                    sellerId: seller.Id,
                    sellerName: seller.Name,
                    sellerAvatarUrl: seller.AvatarUrl,
                    location: local,
                    categoryId: catId,
                    communityId: null,
                    visibility: ListingVisibility.Global,
                    productDetails: pd,
                    serviceDetails: sd);

                var doc = listing.ToBsonDocument();
                doc["CreatedAt"] = new BsonDateTime(DevSeedData.RandomRecent(rnd));
                docs.Add(doc);
                criados.Add((listing.Id, seller.Id));

                if (seed.Kind == ListingKind.Product)
                {
                    produtos++;
                }
                else
                {
                    servicos++;
                }
            }
            catch (DomainException ex)
            {
                erros++;
                _logger.LogWarning(ex, "Seed pulou listing por violação de domínio: {Title}", seed.Title);
            }
        }

        if (docs.Count > 0)
        {
            await _db.GetCollection<BsonDocument>("Listings").InsertManyAsync(docs, cancellationToken: ct);
        }

        return (produtos, servicos, erros, criados);
    }

    // --- 5 comunidades (Tipo=User, Open, todas com capa) com memberships
    //     (Criador/Moderador/Membro), posts COM respostas/curtidas/salvos e
    //     anúncios DA comunidade (um "só aqui" + um público feito na comunidade).
    //     CommunityId determinístico; memberships/posts limpos por CommunityId; listings
    //     pelo prefixo "[Demo]" da descrição.
    private sealed record CommunitySeed(
        int Comunidades, int Memberships, int Posts, int Curtidas, int Salvos);

    private async Task<CommunitySeed> SeedCommunitiesAsync(
        IReadOnlyList<DevSeedData.MockUser> mocks,
        IReadOnlyDictionary<string, Guid> catBySlug,
        Random rnd,
        List<(Guid Id, Guid SellerId)> listingIds,
        CancellationToken ct)
    {
        var specs = DevSeedData.CommunitySpecs();
        var contents = DevSeedData.PostContents();
        var replies = DevSeedData.ReplyContents();
        var commDocs = new List<BsonDocument>(specs.Count);
        var memDocs = new List<BsonDocument>();
        var postDocs = new List<BsonDocument>();
        var postLikeDocs = new List<BsonDocument>();
        var savedPostDocs = new List<BsonDocument>();
        var memberIndicesByComm = new List<List<int>>(specs.Count);

        for (var i = 0; i < specs.Count; i++)
        {
            var sp = specs[i];
            var commId = DevSeedData.DemoCommunityIds[i];
            var creator = mocks[sp.CriadorIndex];

            var comm = CommunityGroup.Create(
                name: sp.Name,
                description: sp.Description,
                type: CommunityType.User,
                axis: sp.Axis,
                visibility: CommunityVisibility.Open,
                password: null,
                lat: sp.Lat,
                lng: sp.Lng,
                neighborhood: sp.Neighborhood,
                city: sp.City,
                state: sp.State,
                creatorId: creator.Id,
                creatorName: creator.Name,
                creatorAvatarUrl: creator.AvatarUrl,
                coverImageUrl: DevSeedData.CoverUrlFor(i));

            var commDoc = comm.ToBsonDocument();
            commDoc["_id"] = new BsonBinaryData(commId, GuidRepresentation.Standard);
            commDocs.Add(commDoc);

            // 3-5 membros: criador (Criador) sempre presente + outros do elenco; 1 Moderador.
            var memberCount = rnd.Next(3, 6);
            var memberIndices = Enumerable.Range(0, mocks.Count)
                .OrderBy(_ => rnd.NextDouble())
                .Take(memberCount)
                .ToList();
            if (!memberIndices.Contains(sp.CriadorIndex))
            {
                memberIndices.Add(sp.CriadorIndex);
            }
            memberIndicesByComm.Add(memberIndices);

            var assignedModerador = false;
            foreach (var idx in memberIndices)
            {
                var u = mocks[idx];
                MembershipRole papel;
                if (idx == sp.CriadorIndex)
                {
                    papel = MembershipRole.Creator;
                }
                else if (!assignedModerador)
                {
                    assignedModerador = true;
                    papel = MembershipRole.Moderator;
                }
                else
                {
                    papel = MembershipRole.Member;
                }

                var mem = Membership.Create(u.Id, u.Name, u.AvatarUrl, commId, papel);
                var memDoc = mem.ToBsonDocument();
                memDoc["JoinedAt"] = new BsonDateTime(DevSeedData.RandomRecent(rnd));
                memDocs.Add(memDoc);
            }

            // 3-5 posts por comunidade; autor é sempre um dos membros. Cada raiz
            // ganha curtidas/salvos de outros membros e 0-2 respostas (a resposta
            // sempre vem DEPOIS da raiz na timeline).
            var members = memberIndices.Select(idx => mocks[idx]).ToList();
            var postCount = rnd.Next(3, 6);
            for (var p = 0; p < postCount; p++)
            {
                var author = members[rnd.Next(members.Count)];
                var content = contents[rnd.Next(contents.Count)];
                var post = Post.CreateRoot(commId, author.Id, author.Name, author.AvatarUrl, content);
                var postDoc = post.ToBsonDocument();
                var rootAt = DevSeedData.RandomRecent(rnd);
                postDoc["CreatedAt"] = new BsonDateTime(rootAt);
                postDocs.Add(postDoc);

                var outros = members.Where(u => u.Id != author.Id).ToList();
                foreach (var u in outros.OrderBy(_ => rnd.NextDouble()).Take(rnd.Next(0, 5)))
                {
                    postLikeDocs.Add(PostLike.Create(post.Id, u.Id).ToBsonDocument());
                }
                foreach (var u in outros.OrderBy(_ => rnd.NextDouble()).Take(rnd.Next(0, 2)))
                {
                    savedPostDocs.Add(SavedPost.Create(u.Id, post.Id, commId).ToBsonDocument());
                }

                var replyCount = rnd.Next(0, 3);
                for (var r = 0; r < replyCount; r++)
                {
                    var responder = outros[rnd.Next(outros.Count)];
                    var reply = Post.CreateReply(
                        post, responder.Id, responder.Name, responder.AvatarUrl,
                        replies[rnd.Next(replies.Count)]);
                    var replyDoc = reply.ToBsonDocument();
                    replyDoc["CreatedAt"] = new BsonDateTime(rootAt.AddMinutes(rnd.Next(5, 600)));
                    postDocs.Add(replyDoc);
                }
            }
        }

        await _db.GetCollection<BsonDocument>("Communities").InsertManyAsync(commDocs, cancellationToken: ct);
        await _db.GetCollection<BsonDocument>("Memberships").InsertManyAsync(memDocs, cancellationToken: ct);
        await _db.GetCollection<BsonDocument>("Posts").InsertManyAsync(postDocs, cancellationToken: ct);
        if (postLikeDocs.Count > 0)
        {
            await _db.GetCollection<BsonDocument>("PostLikes").InsertManyAsync(postLikeDocs, cancellationToken: ct);
        }
        if (savedPostDocs.Count > 0)
        {
            await _db.GetCollection<BsonDocument>("SavedPosts").InsertManyAsync(savedPostDocs, cancellationToken: ct);
        }

        // Anúncios DA comunidade por comunidade: um escopado (Visibility=
        // Community — só aparece aqui) e um público feito na comunidade
        // (Visibility=Both — aparece aqui e no feed da rede). Vendedor é
        // sempre um membro; categoria é a primeira disponível.
        var primeiraCategoria = catBySlug.Values.FirstOrDefault();
        if (primeiraCategoria != Guid.Empty)
        {
            var listingDocs = new List<BsonDocument>(specs.Count * 2);
            for (var i = 0; i < specs.Count; i++)
            {
                var sp = specs[i];
                var commId = DevSeedData.DemoCommunityIds[i];
                var membroIdx = memberIndicesByComm[i];
                var vendedor = mocks[membroIdx[^1]];

                var escopado = Listing.Create(
                    kind: ListingKind.Service,
                    mode: ListingMode.Trade,
                    title: $"Mutirão de consertos de {sp.Name}",
                    description: DevSeedData.DemoPrefix + " Ferramentas e mão de obra da comunidade — só para membros.",
                    images: new List<string> { $"https://picsum.photos/seed/revoa-comm-{i}-escopo/600/400" },
                    priceRvm: 3L,
                    sellerId: vendedor.Id,
                    sellerName: vendedor.Name,
                    sellerAvatarUrl: vendedor.AvatarUrl,
                    location: Location.Create(sp.Lat, sp.Lng, sp.Neighborhood, sp.City, null),
                    categoryId: primeiraCategoria,
                    communityId: commId,
                    visibility: ListingVisibility.Community,
                    productDetails: null,
                    serviceDetails: ServiceDetails.Create(ServiceUnitType.PerService, 2, 30));

                var publico = Listing.Create(
                    kind: ListingKind.Product,
                    mode: ListingMode.Donate,
                    title: $"Alimentos arrecadados em {sp.Name}",
                    description: DevSeedData.DemoPrefix + " Cesta básica montada pela comunidade — aparece no feed da rede também.",
                    images: new List<string> { $"https://picsum.photos/seed/revoa-comm-{i}-publico/600/400" },
                    priceRvm: 0L,
                    sellerId: vendedor.Id,
                    sellerName: vendedor.Name,
                    sellerAvatarUrl: vendedor.AvatarUrl,
                    location: Location.Create(sp.Lat, sp.Lng, sp.Neighborhood, sp.City, null),
                    categoryId: primeiraCategoria,
                    communityId: commId,
                    visibility: ListingVisibility.Both,
                    productDetails: ProductDetails.Create(ProductCondition.Seminovo, 1),
                    serviceDetails: null);

                foreach (var l in new[] { escopado, publico })
                {
                    var d = l.ToBsonDocument();
                    d["CreatedAt"] = new BsonDateTime(DevSeedData.RandomRecent(rnd));
                    listingDocs.Add(d);
                    listingIds.Add((l.Id, vendedor.Id));
                }
            }

            if (listingDocs.Count > 0)
            {
                await _db.GetCollection<BsonDocument>("Listings").InsertManyAsync(listingDocs, cancellationToken: ct);
            }
        }

        return new CommunitySeed(commDocs.Count, memDocs.Count, postDocs.Count, postLikeDocs.Count, savedPostDocs.Count);
    }

    // --- Interações nos anúncios: curtidas (60% dos anúncios, 1-5 mocks),
    //     comentários (45%, 1-2 perguntas + resposta do vendedor em ~1/3) e
    //     salvos (25%, 1-2 mocks). Autor nunca é o vendedor; a resposta do
    //     vendedor vem depois da pergunta.
    private async Task<(int curtidas, int comentarios, int salvos)> SeedInteractionsAsync(
        IReadOnlyList<DevSeedData.MockUser> mocks,
        IReadOnlyList<(Guid Id, Guid SellerId)> listings,
        Random rnd,
        CancellationToken ct)
    {
        var perguntas = DevSeedData.ListingCommentTexts();
        var respostas = DevSeedData.ListingReplyTexts();
        var likeDocs = new List<BsonDocument>();
        var commentDocs = new List<BsonDocument>();
        var saveDocs = new List<BsonDocument>();

        foreach (var (listingId, sellerId) in listings)
        {
            var vendedor = mocks.First(m => m.Id == sellerId);
            var outros = mocks.Where(m => m.Id != sellerId).ToList();

            if (rnd.NextDouble() < 0.6)
            {
                foreach (var u in outros.OrderBy(_ => rnd.NextDouble()).Take(rnd.Next(1, 6)))
                {
                    likeDocs.Add(ListingLike.Create(listingId, u.Id).ToBsonDocument());
                }
            }

            if (rnd.NextDouble() < 0.45)
            {
                var count = rnd.Next(1, 3);
                for (var c = 0; c < count; c++)
                {
                    var leitor = outros[rnd.Next(outros.Count)];
                    var root = Comment.CreateRoot(
                        listingId, leitor.Id, leitor.Name, leitor.AvatarUrl,
                        perguntas[rnd.Next(perguntas.Count)]);
                    var rootDoc = root.ToBsonDocument();
                    var rootAt = DevSeedData.RandomRecent(rnd);
                    rootDoc["CreatedAt"] = new BsonDateTime(rootAt);
                    commentDocs.Add(rootDoc);

                    if (rnd.NextDouble() < 0.35)
                    {
                        var reply = Comment.CreateReply(
                            root, vendedor.Id, vendedor.Name, vendedor.AvatarUrl,
                            respostas[rnd.Next(respostas.Count)]);
                        var replyDoc = reply.ToBsonDocument();
                        replyDoc["CreatedAt"] = new BsonDateTime(rootAt.AddMinutes(rnd.Next(2, 240)));
                        commentDocs.Add(replyDoc);
                    }
                }
            }

            if (rnd.NextDouble() < 0.25)
            {
                foreach (var u in outros.OrderBy(_ => rnd.NextDouble()).Take(rnd.Next(1, 3)))
                {
                    saveDocs.Add(SavedListing.Create(u.Id, listingId).ToBsonDocument());
                }
            }
        }

        if (likeDocs.Count > 0)
        {
            await _db.GetCollection<BsonDocument>("ListingLikes").InsertManyAsync(likeDocs, cancellationToken: ct);
        }
        if (commentDocs.Count > 0)
        {
            await _db.GetCollection<BsonDocument>("Comments").InsertManyAsync(commentDocs, cancellationToken: ct);
        }
        if (saveDocs.Count > 0)
        {
            await _db.GetCollection<BsonDocument>("SavedListings").InsertManyAsync(saveDocs, cancellationToken: ct);
        }

        return (likeDocs.Count, commentDocs.Count, saveDocs.Count);
    }

    // --- ~22 reviews (reviewer e reviewee sempre mocks) + reputação acumulada por usuário.
    //     Reviewee é o vendedor de um listing aleatório — como todo listing agora tem vendedor
    //     mock, o reviewee sempre cai num mock. TradeId é mock (não há trade real off-chain).
    private async Task<(int reviews, int reputacoes)> SeedReviewsAndReputationAsync(
        IReadOnlyList<DevSeedData.MockUser> mocks, Random rnd, CancellationToken ct)
    {
        var comments = DevSeedData.ReviewComments();
        var docs = new List<BsonDocument>(22);
        var received = new Dictionary<Guid, List<int>>(); // ratings acumulados por reviewee

        for (var n = 0; n < 22; n++)
        {
            var reviewer = mocks[rnd.Next(mocks.Count)];
            DevSeedData.MockUser reviewee;
            do
            {
                reviewee = mocks[rnd.Next(mocks.Count)];
            }
            while (reviewee.Id == reviewer.Id);

            // Rating ponderado: ~55% 5★, ~30% 4★, ~15% 3★.
            var roll = rnd.NextDouble();
            var rating = roll < 0.55 ? 5 : roll < 0.85 ? 4 : 3;
            var comment = comments[rnd.Next(comments.Count)];

            var review = Review.Create(
                tradeId: Guid.NewGuid(),
                reviewerId: reviewer.Id,
                reviewerNome: reviewer.Name,
                revieweeId: reviewee.Id,
                rating: rating,
                comment: comment);

            var doc = review.ToBsonDocument();
            doc["CreatedAt"] = new BsonDateTime(DevSeedData.RandomRecent(rnd));
            docs.Add(doc);

            if (!received.TryGetValue(reviewee.Id, out var list))
            {
                received[reviewee.Id] = list = new List<int>();
            }
            list.Add(rating);
        }

        // Reputação: 1 doc por usuário mock, acumulando reviews recebidas + doações/ajuda aleatórias.
        // Level/AvgRating são propriedades computadas (getter-only) — não serializadas, derivadas na leitura.
        var repDocs = new List<BsonDocument>(mocks.Count);
        foreach (var u in mocks)
        {
            var rep = ReputationScore.Create(u.Id);
            if (received.TryGetValue(u.Id, out var ratings))
            {
                foreach (var rt in ratings)
                {
                    rep.ApplyReview(rt);
                }
            }

            // Doações/voluntariado aleatório p/ enriquecer perfis (DonationsCount/VolunteerCount 0-3, HelpPoints 1-5).
            var doacoes = rnd.Next(0, 4);
            for (var d = 0; d < doacoes; d++)
            {
                rep.ApplyDonationReward(reputationPoints: 15, helpPoints: rnd.Next(1, 6), isVolunteer: d % 2 == 0);
            }

            repDocs.Add(rep.ToBsonDocument());
        }

        await _db.GetCollection<BsonDocument>("Reviews").InsertManyAsync(docs, cancellationToken: ct);
        await _db.GetCollection<BsonDocument>("Reputations").InsertManyAsync(repDocs, cancellationToken: ct);
        return (docs.Count, repDocs.Count);
    }

    // --- 5 trocas de demonstração sobre anúncios do seed, cobrindo os estados que a
    //     página /trades trata: 2 liberadas (produto e serviço c/ voucher redeemado),
    //     1 financiada (em andamento), 1 doação liberada (total 0) e 1 disputada.
    //     Carteiras/tx são placeholders — a leitura não toca a chain. Buyer nunca é o
    //     vendedor; FundedAt/ReleasedAt espalhados nos últimos 20 dias.
    private async Task<int> SeedTradesAsync(
        IReadOnlyList<DevSeedData.MockUser> mocks,
        List<(Guid Id, Guid SellerId)> listings,
        Random rnd,
        CancellationToken ct)
    {
        if (listings.Count < 5)
        {
            return 0;
        }

        var docs = new List<BsonDocument>(5);
        for (var n = 0; n < 5; n++)
        {
            var (listingId, sellerId) = listings[(n * 17) % listings.Count];
            var seller = mocks.First(m => m.Id == sellerId);
            var buyer = mocks.Where(m => m.Id != sellerId).OrderBy(_ => rnd.NextDouble()).First();

            var kind = n == 0 || n == 4 ? TradeKind.Service : TradeKind.Product;
            var mode = n == 3 ? TradeMode.Donate : TradeMode.Trade;
            var total = mode == TradeMode.Donate ? 0L : new[] { 5L, 10L, 8L, 0L, 12L }[n];

            var trade = Trade.Create(
                listingId,
                mode,
                kind,
                sellerId, "0xSeedSellerWallet", seller.Name, seller.AvatarUrl,
                buyer.Id, "0xSeedBuyerWallet", buyer.Name, buyer.AvatarUrl,
                total, "0xSeedAssetContract", n + 1, n + 1,
                DevSeedData.RandomRecent(rnd), "0xSeedTx" + n);

            if (n == 0 || n == 1 || n == 3)
            {
                // Liberadas (concluídas) — serviço confirma o voucher antes da liberação.
                if (kind == TradeKind.Service)
                {
                    trade.MarkRedeemed("0xSeedRedeemTx" + n);
                }
                trade.MarkLiberada("0xSeedReleaseTx" + n);
            }
            else if (n == 4)
            {
                trade.MarkDisputada(buyer.Email);
            }
            // n == 2 permanece Financiada (troca em andamento).

            var doc = trade.ToBsonDocument();
            docs.Add(doc);
        }

        await _db.GetCollection<BsonDocument>("Trades").InsertManyAsync(docs, cancellationToken: ct);
        return docs.Count;
    }
}
