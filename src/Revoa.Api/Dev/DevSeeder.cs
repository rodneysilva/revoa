using MongoDB.Bson;
using MongoDB.Driver;
using Nethereum.Signer;
using Revoa.Abstractions;
using Revoa.Account.Domain.Aggregates.AccountAggregate;
using Revoa.Catalog.Domain.Aggregates.CategoryAggregate;
using Revoa.Catalog.Domain.Aggregates.ListingAggregate;
using Revoa.Catalog.Domain.Repositories;
using Revoa.Community.Domain.Aggregates.CommunityAggregate;
using Revoa.Community.Domain.Aggregates.MembershipAggregate;
using Revoa.Community.Domain.Aggregates.PostAggregate;
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
    int Erros);

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
        var (produtos, servicos, erros) = await SeedListingsAsync(mocks, catBySlug, rnd, ct);

        // 5) Comunidades (5) + memberships + posts.
        var (comunidades, memberships, posts) = await SeedCommunitiesAsync(mocks, rnd, ct);

        // 6) Reviews (~22) + reputações acumuladas por usuário.
        var (reviews, reputacoes) = await SeedReviewsAndReputationAsync(mocks, rnd, ct);

        return new DevSeedResult(
            Categorias: categoriasCriadas,
            Removidos: removidos,
            Usuarios: usuarios,
            Carteiras: carteiras,
            Produtos: produtos,
            Servicos: servicos,
            Listings: produtos + servicos,
            Comunidades: comunidades,
            Memberships: memberships,
            Posts: posts,
            Reviews: reviews,
            Reputacoes: reputacoes,
            Erros: erros);
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

    // --- Catálogo (56 produtos + 57 serviços) vinculado aos mocks: SellerId/Nome/AvatarUrl
    //     rotacionam entre os 10 usuários. CreatedAt espalhado nos últimos 30 dias (feed variado).
    private async Task<(int produtos, int servicos, int erros)> SeedListingsAsync(
        IReadOnlyList<DevSeedData.MockUser> mocks,
        IReadOnlyDictionary<string, Guid> catBySlug,
        Random rnd,
        CancellationToken ct)
    {
        var all = DevSeedData.BuildProducts().Concat(DevSeedData.BuildServices()).ToList();
        var places = DevSeedData.Places();
        var docs = new List<BsonDocument>(all.Count);
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

        return (produtos, servicos, erros);
    }

    // --- 5 comunidades (Tipo=User, Open) com memberships (Criador/Moderador/Membro) e posts.
    //     CommunityId determinístico; memberships/posts limpos por CommunityId.
    private async Task<(int comunidades, int memberships, int posts)> SeedCommunitiesAsync(
        IReadOnlyList<DevSeedData.MockUser> mocks, Random rnd, CancellationToken ct)
    {
        var specs = DevSeedData.CommunitySpecs();
        var contents = DevSeedData.PostContents();
        var commDocs = new List<BsonDocument>(specs.Count);
        var memDocs = new List<BsonDocument>();
        var postDocs = new List<BsonDocument>();

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
                creatorAvatarUrl: creator.AvatarUrl);

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

            // 3-5 posts por comunidade; autor é sempre um dos membros.
            var postCount = rnd.Next(3, 6);
            for (var p = 0; p < postCount; p++)
            {
                var author = mocks[memberIndices[rnd.Next(memberIndices.Count)]];
                var content = contents[rnd.Next(contents.Count)];
                var post = Post.CreateRoot(commId, author.Id, author.Name, author.AvatarUrl, content);
                var postDoc = post.ToBsonDocument();
                postDoc["CreatedAt"] = new BsonDateTime(DevSeedData.RandomRecent(rnd));
                postDocs.Add(postDoc);
            }
        }

        await _db.GetCollection<BsonDocument>("Communities").InsertManyAsync(commDocs, cancellationToken: ct);
        await _db.GetCollection<BsonDocument>("Memberships").InsertManyAsync(memDocs, cancellationToken: ct);
        await _db.GetCollection<BsonDocument>("Posts").InsertManyAsync(postDocs, cancellationToken: ct);
        return (commDocs.Count, memDocs.Count, postDocs.Count);
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
}
