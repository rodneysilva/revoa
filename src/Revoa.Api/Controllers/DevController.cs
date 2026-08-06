using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

namespace Revoa.Api.Controllers;

// Endpoints DEV-ONLY (em produção retornam 404). Populam o ambiente de desenvolvimento com um
// ecossistema de demonstração VIVO e interconectado: usuários mock reais (com carteira), catálogo
// vinculado a esses usuários, comunidades com membros/posts e interações (reviews + reputação).
[ApiController]
[Route("api/dev")]
public class DevController : ControllerBase
{
    private const string DemoPrefix = "[Demo]";

    private readonly ICategoryRepository _categories;
    private readonly IMongoDatabase _db;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<DevController> _logger;

    public DevController(
        ICategoryRepository categories,
        IMongoDatabase db,
        IWebHostEnvironment env,
        ILogger<DevController> logger)
    {
        _categories = categories;
        _db = db;
        _env = env;
        _logger = logger;
    }

    /// <summary>
    /// (Re)semeia o ambiente DEV completo: 10 usuários mock (+carteiras), catálogo vinculado aos
    /// mocks, 5 comunidades com membros e posts, e ~22 reviews (+reputação acumulada). Idempotente
    /// (limpa tudo por chaves determinísticas antes de re-inserir). Em produção retorna 404.
    /// </summary>
    [HttpPost("seed-catalog")]
    [AllowAnonymous]
    public async Task<ActionResult> SeedCatalog(CancellationToken ct)
    {
        if (!_env.IsDevelopment())
        {
            return NotFound();
        }

        // Seed fixa → variedade temporal entre itens + dados reproduzíveis a cada execução do seed.
        var rnd = new Random(20260806);

        // 1) Garante categorias (produto + serviço) — idempotente.
        var categoriasCriadas = 0;
        foreach (var (nome, slug, descricao) in AllCategories())
        {
            if (await _categories.GetBySlugAsync(slug, ct) is null)
            {
                await _categories.AddAsync(Category.Create(nome, slug, descricao), ct);
                categoriasCriadas++;
            }
        }

        // Mapeia slug → CategoriaId uma única vez (evita N queries no loop de listings).
        var catBySlug = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        foreach (var c in AllCategories())
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
        var mocks = MockUsers();
        var usuarios = await SeedUsersAsync(mocks, ct);
        var carteiras = await SeedAccountsAsync(mocks, ct);

        // 4) Catálogo vinculado aos mocks (VendedorId rotaciona entre os 10 mocks).
        var (produtos, servicos, erros) = await SeedListingsAsync(mocks, catBySlug, rnd, ct);

        // 5) Comunidades (5) + memberships + posts.
        var (comunidades, memberships, posts) = await SeedCommunitiesAsync(mocks, rnd, ct);

        // 6) Reviews (~22) + reputações acumuladas por usuário.
        var (reviews, reputacoes) = await SeedReviewsAndReputationAsync(mocks, rnd, ct);

        return Ok(new
        {
            categorias = categoriasCriadas,
            removidos,
            usuarios,
            carteiras,
            produtos,
            servicos,
            listings = produtos + servicos,
            comunidades,
            memberships,
            posts,
            reviews,
            reputacoes,
            erros
        });
    }

    // --- Limpeza idempotente: remove todos os mocks pelas chaves determinísticas (10 usuários +
    //     5 comunidades). Listings continuam pelo prefixo literal "[Demo]" na Descrição.
    //     BsonBinaryData explícito é obrigatório no GuidRepresentationMode V3 (BsonDocument sem
    //     representação implícita de Guid quebraria o match do filtro).
    private async Task<long> CleanDemoDataAsync(CancellationToken ct)
    {
        var userBin = DemoUserIds.Select(g => new BsonBinaryData(g, GuidRepresentation.Standard)).ToList();
        var commBin = DemoCommunityIds.Select(g => new BsonBinaryData(g, GuidRepresentation.Standard)).ToList();
        var bf = Builders<BsonDocument>.Filter;

        var listings = _db.GetCollection<BsonDocument>("Listings");
        var demoFilter = bf.Regex(
            "Descricao", new BsonRegularExpression("^" + System.Text.RegularExpressions.Regex.Escape(DemoPrefix)));
        var removed = (await listings.DeleteManyAsync(demoFilter, ct)).DeletedCount;

        removed += await DeleteByAsync("Users", bf.In("_id", userBin));
        removed += await DeleteByAsync("Accounts", bf.In("UserId", userBin));
        removed += await DeleteByAsync("Communities", bf.In("_id", commBin));
        removed += await DeleteByAsync("Memberships", bf.In("ComunidadeId", commBin));
        removed += await DeleteByAsync("Posts", bf.In("ComunidadeId", commBin));
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
    private async Task<int> SeedUsersAsync(IReadOnlyList<MockUser> mocks, CancellationToken ct)
    {
        var docs = new List<BsonDocument>(mocks.Count);
        foreach (var m in mocks)
        {
            var user = AppUser.Create(m.Nome, m.Email, m.Telefone, idadeOk: true);
            // DEV only: bypassa a dupla verificação (e-mail + telefone) e ativa o usuário.
            user.DevActivate();
            var doc = user.ToBsonDocument();
            doc["_id"] = new BsonBinaryData(m.Id, GuidRepresentation.Standard);
            docs.Add(doc);
        }

        await _db.GetCollection<BsonDocument>("Users").InsertManyAsync(docs, cancellationToken: ct);
        return docs.Count;
    }

    // --- Carteiras (Accounts): gera EOA Nethereum aleatória por usuário (off-chain, perfil DEV).
    //     Random é OK — a Account é limpa por UserId antes de re-inserir (idempotente).
    private async Task<int> SeedAccountsAsync(IReadOnlyList<MockUser> mocks, CancellationToken ct)
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

    // --- Catálogo (56 produtos + 57 serviços) vinculado aos mocks: VendedorId/Nome/AvatarUrl
    //     rotacionam entre os 10 usuários. CreatedAt espalhado nos últimos 30 dias (feed variado).
    private async Task<(int produtos, int servicos, int erros)> SeedListingsAsync(
        IReadOnlyList<MockUser> mocks,
        IReadOnlyDictionary<string, Guid> catBySlug,
        Random rnd,
        CancellationToken ct)
    {
        var all = BuildProducts().Concat(BuildServices()).ToList();
        var places = Places();
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
                var local = Location.Create(place.Lat, place.Lng, place.Bairro, place.Cidade, place.Cep);

                ProductDetails? pd = seed.Condition is null ? null : ProductDetails.Create(seed.Condition.Value, 1);
                ServiceDetails? sd = seed.UnitType is null
                    ? null
                    : ServiceDetails.Create(seed.UnitType.Value, seed.Duration, 30);

                var listing = Listing.Create(
                    kind: seed.Kind,
                    modo: seed.Modo,
                    titulo: seed.Titulo,
                    descricao: DemoPrefix + " " + seed.Descricao,
                    imagens: new List<string> { $"https://picsum.photos/seed/revoa-{i}/600/400" },
                    precoRvm: seed.PrecoRvm,
                    vendedorId: seller.Id,
                    vendedorNome: seller.Nome,
                    vendedorAvatarUrl: seller.AvatarUrl,
                    localizacao: local,
                    categoriaId: catId,
                    comunidadeId: null,
                    visibilidade: ListingVisibilidade.Global,
                    productDetails: pd,
                    serviceDetails: sd);

                var doc = listing.ToBsonDocument();
                doc["CreatedAt"] = new BsonDateTime(RandomRecent(rnd));
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
                _logger.LogWarning(ex, "Seed pulou listing por violação de domínio: {Titulo}", seed.Titulo);
            }
        }

        if (docs.Count > 0)
        {
            await _db.GetCollection<BsonDocument>("Listings").InsertManyAsync(docs, cancellationToken: ct);
        }

        return (produtos, servicos, erros);
    }

    // --- 5 comunidades (Tipo=User, Open) com memberships (Criador/Moderador/Membro) e posts.
    //     ComunidadeId determinístico; memberships/posts limpos por ComunidadeId.
    private async Task<(int comunidades, int memberships, int posts)> SeedCommunitiesAsync(
        IReadOnlyList<MockUser> mocks, Random rnd, CancellationToken ct)
    {
        var specs = CommunitySpecs();
        var contents = PostContents();
        var commDocs = new List<BsonDocument>(specs.Count);
        var memDocs = new List<BsonDocument>();
        var postDocs = new List<BsonDocument>();

        for (var i = 0; i < specs.Count; i++)
        {
            var sp = specs[i];
            var commId = DemoCommunityIds[i];
            var creator = mocks[sp.CriadorIndex];

            var comm = CommunityGroup.Create(
                nome: sp.Nome,
                descricao: sp.Descricao,
                tipo: CommunityTipo.User,
                eixo: sp.Eixo,
                visibilidade: CommunityVisibilidade.Open,
                password: null,
                lat: sp.Lat,
                lng: sp.Lng,
                bairro: sp.Bairro,
                cidade: sp.Cidade,
                estado: sp.Estado,
                criadorId: creator.Id,
                criadorNome: creator.Nome,
                criadorAvatarUrl: creator.AvatarUrl);

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
                MembershipPapel papel;
                if (idx == sp.CriadorIndex)
                {
                    papel = MembershipPapel.Criador;
                }
                else if (!assignedModerador)
                {
                    assignedModerador = true;
                    papel = MembershipPapel.Moderador;
                }
                else
                {
                    papel = MembershipPapel.Membro;
                }

                var mem = Membership.Create(u.Id, u.Nome, u.AvatarUrl, commId, papel);
                var memDoc = mem.ToBsonDocument();
                memDoc["JoinedAt"] = new BsonDateTime(RandomRecent(rnd));
                memDocs.Add(memDoc);
            }

            // 3-5 posts por comunidade; autor é sempre um dos membros.
            var postCount = rnd.Next(3, 6);
            for (var p = 0; p < postCount; p++)
            {
                var author = mocks[memberIndices[rnd.Next(memberIndices.Count)]];
                var content = contents[rnd.Next(contents.Count)];
                var post = Post.CreateRoot(commId, author.Id, author.Nome, author.AvatarUrl, content);
                var postDoc = post.ToBsonDocument();
                postDoc["CreatedAt"] = new BsonDateTime(RandomRecent(rnd));
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
        IReadOnlyList<MockUser> mocks, Random rnd, CancellationToken ct)
    {
        var comments = ReviewComments();
        var docs = new List<BsonDocument>(22);
        var received = new Dictionary<Guid, List<int>>(); // ratings acumulados por reviewee

        for (var n = 0; n < 22; n++)
        {
            var reviewer = mocks[rnd.Next(mocks.Count)];
            MockUser reviewee;
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
                reviewerNome: reviewer.Nome,
                revieweeId: reviewee.Id,
                rating: rating,
                comment: comment);

            var doc = review.ToBsonDocument();
            doc["CreatedAt"] = new BsonDateTime(RandomRecent(rnd));
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

    // Data/hora UTC nos últimos 30 dias (variedade temporal p/ o feed e timelines).
    private static DateTime RandomRecent(Random rnd) =>
        DateTime.SpecifyKind(
            DateTime.UtcNow.Date
                .AddDays(-rnd.Next(0, 30))
                .AddHours(rnd.Next(8, 22))
                .AddMinutes(rnd.Next(0, 60)),
            DateTimeKind.Utc);

    private sealed record SeedCategory(string Nome, string Slug, string? Descricao);
    private sealed record SeedListing(
        ListingKind Kind,
        ListingModo Modo,
        string Titulo,
        string Descricao,
        string CategorySlug,
        long PrecoRvm,
        ProductCondition? Condition,
        ServiceUnitType? UnitType,
        int Duration);

    // Usuário mock (não é o aggregate User — é o "elenco" com avatar embutido usado nos embeds).
    private sealed record MockUser(
        Guid Id,
        string Nome,
        string Email,
        string Telefone,
        string AvatarUrl,
        string Cidade,
        string Bairro);

    private sealed record CommunitySpec(
        string Nome,
        string Descricao,
        CommunityEixo Eixo,
        string Cidade,
        string Estado,
        string Bairro,
        double Lat,
        double Lng,
        int CriadorIndex);

    private sealed record Place(double Lat, double Lng, string Bairro, string Cidade, string Cep);

    // 10 chaves determinísticas (idempotência: mesma seed → mesmos IDs).
    private static readonly Guid[] DemoUserIds =
    {
        new("aabbccdd-0001-4000-8000-000000000001"),
        new("aabbccdd-0002-4000-8000-000000000002"),
        new("aabbccdd-0003-4000-8000-000000000003"),
        new("aabbccdd-0004-4000-8000-000000000004"),
        new("aabbccdd-0005-4000-8000-000000000005"),
        new("aabbccdd-0006-4000-8000-000000000006"),
        new("aabbccdd-0007-4000-8000-000000000007"),
        new("aabbccdd-0008-4000-8000-000000000008"),
        new("aabbccdd-0009-4000-8000-000000000009"),
        new("aabbccdd-000a-4000-8000-00000000000a")
    };

    // 5 chaves determinísticas de comunidades (mesma idempotência).
    private static readonly Guid[] DemoCommunityIds =
    {
        new("aabbccdd-1001-4000-8000-000000000001"),
        new("aabbccdd-1002-4000-8000-000000000002"),
        new("aabbccdd-1003-4000-8000-000000000003"),
        new("aabbccdd-1004-4000-8000-000000000004"),
        new("aabbccdd-1005-4000-8000-000000000005")
    };

    private static string AvatarFor(string nome) =>
        $"https://api.dicebear.com/7.x/initials/svg?seed={Uri.EscapeDataString(nome)}";

    private static IReadOnlyList<SeedCategory> AllCategories()
    {
        var cats = new List<SeedCategory>
        {
            // Produtos.
            new("Eletrônicos", "eletronicos", "Celulares, notebooks, TVs e acessórios"),
            new("Móveis e Decoração", "moveis-decoracao", "Móveis e itens de decoração para casa"),
            new("Roupas e Acessórios", "roupas-acessorios", "Vestuário, calçados e acessórios"),
            new("Casa e Cozinha", "casa-cozinha", "Eletrodomésticos, utensílios e itens de cozinha"),
            new("Livros e Mídia", "livros-midia", "Livros, HQs, revistas e mídia física"),
            new("Esporte e Lazer", "esporte-lazer", "Bicicletas, equipamentos esportivos e lazer"),
            new("Brinquedos e Infantil", "brinquedos-infantil", "Brinquedos, roupas e itens infantis"),
            new("Ferramentas", "ferramentas", "Ferramentas manuais e elétricas"),
            new("Jardim e Plantas", "jardim-plantas", "Plantas, vasos e itens de jardinagem"),
            new("Pet", "pet", "Ração, camas e acessórios para pets"),
            new("Beleza e Cuidados", "beleza-cuidados", "Produtos de beleza e cuidados pessoais"),
            new("Instrumentos Musicais", "instrumentos-musicais", "Violões, teclados e acessórios musicais"),
            // Serviços.
            new("Aulas e Reforço", "aulas-reforco", "Aulas particulares, reforço e mentoria"),
            new("Reparos e Reformas", "reparos-reformas", "Reparos elétricos, hidráulicos e reformas"),
            new("Beleza e Estética", "beleza-estetica", "Cabelo, unhas, estética e cuidados"),
            new("Tecnologia e Suporte", "tecnologia-suporte", "Suporte de informática, formatação e configs"),
            new("Transporte e Fretes", "transporte-fretes", "Fretes, caronas e mudanças"),
            new("Saúde e Bem-estar", "saude-bem-estar", "Massagem, yoga, nutrição e bem-estar"),
            new("Eventos e Festas", "eventos-festas", "Decoração, DJ, fotografia e animação"),
            new("Culinária e Confeitaria", "culinaria-confeitaria", "Bolos, salgados e marmitas sob encomenda"),
            new("Design e Criação", "design-criacao", "Logos, cartões e materiais visuais"),
            new("Administração e Contabilidade", "administracao-contabilidade", "IR, MEI e organização financeira"),
            new("Ajuda e Voluntariado", "ajuda-voluntariado", "Apoio a idosos, acompanhamentos e ajuda comunitária")
        };
        return cats;
    }

    private static IReadOnlyList<SeedListing> BuildProducts()
    {
        var p = ListingKind.Product;
        var T = ListingModo.Trocar;
        var R = ListingModo.Repassar;
        var D = ListingModo.Doar;

        return new List<SeedListing>
        {
            new(p, T, "Notebook Dell usado — 8GB SSD240", "Notebook funcional, bateria com boa autonomia. Retirada no bairro.", "eletronicos", 35, ProductCondition.Seminovo, null, 0),
            new(p, T, "Celular Moto G usado 64GB", "Funcionando, leves marcas de uso. Tela sem trincos.", "eletronicos", 28, ProductCondition.Usado, null, 0),
            new(p, T, "Smart TV LED 32 polegadas", "TV em ótimo estado, controle incluso. Retirada a combinar.", "eletronicos", 40, ProductCondition.Usado, null, 0),
            new(p, R, "Carregador portátil 10000mAh", "Power bank seminovo, carrega dois aparelhos.", "eletronicos", 4, ProductCondition.Seminovo, null, 0),
            new(p, D, "Fone bluetooth (doação)", "Funciona bem, na caixa. Levo para quem precisar.", "eletronicos", 0, ProductCondition.Usado, null, 0),
            new(p, T, "Cadeira de escritório ergonômica", "Cadeira confortável, rodízios ok. Ótima para home office.", "moveis-decoracao", 22, ProductCondition.Seminovo, null, 0),
            new(p, D, "Sofá de 3 lugares (doação)", "Sofá usado em bom estado, só retirar. Combinamos horário.", "moveis-decoracao", 0, ProductCondition.Usado, null, 0),
            new(p, T, "Mesa de jantar de madeira maciça", "Mesa espaçosa para 6 pessoas. Retirada no local.", "moveis-decoracao", 30, ProductCondition.Usado, null, 0),
            new(p, R, "Rack de TV pequeno", "Rack em MDF, comporta até 42 polegadas.", "moveis-decoracao", 6, ProductCondition.Usado, null, 0),
            new(p, D, "Abajur decorativo (doação)", "Abajur funcional, perfeito para o canto da sala.", "moveis-decoracao", 0, ProductCondition.Usado, null, 0),
            new(p, T, "Jaqueta jeans masculina tam M", "Pouco uso, sem defeitos. Vai bem em qualquer frio.", "roupas-acessorios", 8, ProductCondition.Seminovo, null, 0),
            new(p, D, "Vestido floral tam G (doação)", "Vestido bonito, usado poucas vezes. Para quem servir.", "roupas-acessorios", 0, ProductCondition.Seminovo, null, 0),
            new(p, R, "Tênis esportivo nº 39", "Tênis usado, mas com solado íntegro.", "roupas-acessorios", 5, ProductCondition.Usado, null, 0),
            new(p, T, "Mochila universitária reforçada", "Mochila espaçosa, com compartimento para notebook.", "roupas-acessorios", 10, ProductCondition.Seminovo, null, 0),
            new(p, D, "Roupas infantis 2-3 anos (doação)", "Várias peças em bom estado. Para famílias que precisam.", "roupas-acessorios", 0, ProductCondition.Usado, null, 0),
            new(p, T, "Liquidificador 3 velocidades", "Funciona perfeitamente, copo sem trincos.", "casa-cozinha", 9, ProductCondition.Usado, null, 0),
            new(p, R, "Jogo de panelas antiaderente (4 peças)", "Panelas em estado razoável, ótimas para começar.", "casa-cozinha", 7, ProductCondition.Usado, null, 0),
            new(p, D, "Liquidificador manual (doação)", "Minipimer funcionando, para quem está montando casa.", "casa-cozinha", 0, ProductCondition.Usado, null, 0),
            new(p, T, "Cafeteira elétrica", "Cafeteira seminova, faz café rápido.", "casa-cozinha", 6, ProductCondition.Seminovo, null, 0),
            new(p, T, "Air Fryer 3,2 litros", "Fritadeira sem óleo, super conservada.", "casa-cozinha", 18, ProductCondition.Seminovo, null, 0),
            new(p, D, "Livros de literatura (lote, doação)", "6 livros clássicos para circular o conhecimento.", "livros-midia", 0, ProductCondition.Usado, null, 0),
            new(p, D, "Apostilas de ENEM usadas (doação)", "Material para quem está se preparando.", "livros-midia", 0, ProductCondition.Usado, null, 0),
            new(p, R, "HQ Turma da Mônica (coleção)", "Várias edições em bom estado, nostalgia garantida.", "livros-midia", 3, ProductCondition.Usado, null, 0),
            new(p, T, "Livro A Arte da Guerra", "Edição de bolso, capa em ótimo estado.", "livros-midia", 4, ProductCondition.Seminovo, null, 0),
            new(p, T, "Bicicleta aro 26 revisada", "Bike calibrada e com freios ajustados. Pronta pra rodar.", "esporte-lazer", 32, ProductCondition.Usado, null, 0),
            new(p, T, "Par de halteres 10kg", "Halteres de ferro, ótimos para treino em casa.", "esporte-lazer", 14, ProductCondition.Usado, null, 0),
            new(p, R, "Prancha de surfe usada", "Prancha em estado razoável, ótima para iniciantes.", "esporte-lazer", 8, ProductCondition.Usado, null, 0),
            new(p, D, "Bola de futebol society (doação)", "Bola em condição de uso, para a pelada do bairro.", "esporte-lazer", 0, ProductCondition.Usado, null, 0),
            new(p, D, "Jogo de damas de madeira (doação)", "Tabuleiro completo para o lazer em família.", "esporte-lazer", 0, ProductCondition.Usado, null, 0),
            new(p, D, "Carrinho de controle remoto (doação)", "Funciona, vai alegrar uma criança.", "brinquedos-infantil", 0, ProductCondition.Usado, null, 0),
            new(p, R, "Blocos de montar (lote grande)", "Várias peças de encaixar, criatividade sem limite.", "brinquedos-infantil", 5, ProductCondition.Usado, null, 0),
            new(p, D, "Boneca de pano artesanal (doação)", "Feita à mão, novinha. Linda para presentear.", "brinquedos-infantil", 0, ProductCondition.Seminovo, null, 0),
            new(p, T, "Triciclo infantil", "Triciclo em bom estado, ideal de 2 a 5 anos.", "brinquedos-infantil", 12, ProductCondition.Usado, null, 0),
            new(p, T, "Furadeira de impacto 13mm", "Furadeira potente com brocas, super conservada.", "ferramentas", 20, ProductCondition.Seminovo, null, 0),
            new(p, R, "Caixa de ferramentas com kit", "Kit completo para pequenos reparos domésticos.", "ferramentas", 6, ProductCondition.Usado, null, 0),
            new(p, D, "Martelo e alicate (doação)", "Ferramentas básicas para quem está começando.", "ferramentas", 0, ProductCondition.Usado, null, 0),
            new(p, T, "Escada de alumínio 4 degraus", "Escada leve e segura, ótima manutenção.", "ferramentas", 15, ProductCondition.Usado, null, 0),
            new(p, D, "Muda de costela-de-adão (doação)", "Planta saudável para deixar a casa verde.", "jardim-plantas", 0, ProductCondition.Novo, null, 0),
            new(p, T, "Vaso de cerâmica grande", "Vaso decorativo, combina com qualquer ambiente.", "jardim-plantas", 11, ProductCondition.Usado, null, 0),
            new(p, D, "Mudas de manjericão e salsa (doação)", "Hortaliças para começar sua horta em casa.", "jardim-plantas", 0, ProductCondition.Novo, null, 0),
            new(p, R, "Mangueira de jardim 15m", "Mangueira em bom estado, sem vazamentos.", "jardim-plantas", 3, ProductCondition.Usado, null, 0),
            new(p, T, "Ração para cães 3kg (fechada)", "Saco lacrado, marca de qualidade. Para o seu pet.", "pet", 16, ProductCondition.Novo, null, 0),
            new(p, D, "Caminha pet tam M (doação)", "Caminha limpa e confortável para o bichinho.", "pet", 0, ProductCondition.Usado, null, 0),
            new(p, R, "Arranhador de gato", "Arranhador seminovo, salva seu sofá.", "pet", 4, ProductCondition.Seminovo, null, 0),
            new(p, T, "Aquário 40L com filtro", "Aquário completo, peixes não inclusos.", "pet", 19, ProductCondition.Usado, null, 0),
            new(p, T, "Secador de cabelo 2200W", "Secador potente, super conservado.", "beleza-cuidados", 13, ProductCondition.Seminovo, null, 0),
            new(p, R, "Kit de produtos capilares (selados)", "Produtos novos, lacrados. Para cabelos cacheados.", "beleza-cuidados", 5, ProductCondition.Novo, null, 0),
            new(p, D, "Prancha alisadora (doação)", "Prancha funcionando, para quem precisa.", "beleza-cuidados", 0, ProductCondition.Usado, null, 0),
            new(p, R, "Estojo de maquiagem (usado 1x)", "Maquiagem seminova, várias tonalidades.", "beleza-cuidados", 4, ProductCondition.Seminovo, null, 0),
            new(p, T, "Violão popular acústico", "Violão com som macio, cordas novas.", "instrumentos-musicais", 25, ProductCondition.Usado, null, 0),
            new(p, T, "Teclado musical 61 teclas", "Teclado com fonte, ótimo para estudar.", "instrumentos-musicais", 26, ProductCondition.Seminovo, null, 0),
            new(p, D, "Flauta doce Yamaha (doação)", "Flauta em ótimo estado, perfeita para escola.", "instrumentos-musicais", 0, ProductCondition.Seminovo, null, 0),
            new(p, R, "Kit palhetas de saxofone", "Palhetas novas na embalagem.", "instrumentos-musicais", 2, ProductCondition.Novo, null, 0),
            new(p, R, "Webcam HD 720p", "Webcam seminova, ótima para reuniões online.", "eletronicos", 3, ProductCondition.Seminovo, null, 0),
            new(p, D, "Sanduicheira (doação)", "Funciona bem, para quem está montando a cozinha.", "casa-cozinha", 0, ProductCondition.Usado, null, 0),
            new(p, D, "Livros infantis ilustrados (lote, doação)", "Vários livrinhos para despertar a leitura.", "livros-midia", 0, ProductCondition.Usado, null, 0)
        };
    }

    private static IReadOnlyList<SeedListing> BuildServices()
    {
        var s = ListingKind.Service;
        var T = ListingModo.Trocar;
        var V = ListingModo.Voluntariar;

        return new List<SeedListing>
        {
            new(s, T, "Aula de matemática (ensino médio)", "Aula particular para destravar a matéria. Material incluso.", "aulas-reforco", 15, null, ServiceUnitType.Hours, 60),
            new(s, T, "Reforço de português e redação", "Foco em escrita e interpretação para o ENEM.", "aulas-reforco", 12, null, ServiceUnitType.Hours, 60),
            new(s, V, "Aula de inglês conversação (voluntariado)", "Bate-papo em inglês para praticar, sem custo.", "aulas-reforco", 0, null, ServiceUnitType.Hours, 60),
            new(s, T, "Aulas de violão para iniciante", "Do básico às primeiras músicas, passo a passo.", "aulas-reforco", 14, null, ServiceUnitType.Hours, 60),
            new(s, V, "Aula de física para o ENEM (voluntariado)", "Resolução de questões e teoria, de graça.", "aulas-reforco", 0, null, ServiceUnitType.Hours, 90),
            new(s, T, "Reforço em programação básica (Python)", "Lógica e primeiros passos na programação.", "aulas-reforco", 18, null, ServiceUnitType.Hours, 90),
            new(s, T, "Reforma de pia de cozinha", "Troca de pia e ajustes de instalação.", "reparos-reformas", 35, null, ServiceUnitType.PerService, 0),
            new(s, T, "Reparo elétrico (troca de tomadas)", "Substituição de tomadas e disjuntores.", "reparos-reformas", 20, null, ServiceUnitType.PerService, 0),
            new(s, V, "Pintura de quarto (voluntariado)", "Mãos à obra para pintar um quarto, em colaboração.", "reparos-reformas", 0, null, ServiceUnitType.Hours, 240),
            new(s, T, "Reparo de vazamento (encanamento)", "Diagnóstico e reparo de pequenos vazamentos.", "reparos-reformas", 22, null, ServiceUnitType.PerService, 0),
            new(s, V, "Montagem de móveis (voluntariado)", "Ajudo a montar aquele móvel que veio desmontado.", "reparos-reformas", 0, null, ServiceUnitType.Hours, 120),
            new(s, T, "Corte de cabelo masculino", "Corte na tesoura e máquina, bem caprichado.", "beleza-estetica", 8, null, ServiceUnitType.PerService, 0),
            new(s, T, "Escova modeladora", "Escova para arrasar no dia a dia.", "beleza-estetica", 7, null, ServiceUnitType.Hours, 60),
            new(s, T, "Manicure e pedicure", "Unhas feitas com capricho e cuidado.", "beleza-estetica", 10, null, ServiceUnitType.PerService, 0),
            new(s, V, "Design de sobrancelhas (voluntariado)", "Para quem está apertada, de coração.", "beleza-estetica", 0, null, ServiceUnitType.PerService, 0),
            new(s, T, "Unhas decoradas para festa", "Nail art para eventos especiais.", "beleza-estetica", 12, null, ServiceUnitType.Hours, 90),
            new(s, T, "Formatação de PC e backup", "Backup + formatação + instalação de programas.", "tecnologia-suporte", 12, null, ServiceUnitType.PerService, 0),
            new(s, V, "Instalação de Linux em notebook velho (voluntariado)", "Reciclo seu PC antigo com sistema leve.", "tecnologia-suporte", 0, null, ServiceUnitType.PerService, 0),
            new(s, V, "Backup de fotos do celular (voluntariado)", "Salvo suas fotos na nuvem para não perder memórias.", "tecnologia-suporte", 0, null, ServiceUnitType.PerService, 0),
            new(s, T, "Manutenção de roteador Wi-Fi", "Configuração e otimização da sua rede.", "tecnologia-suporte", 8, null, ServiceUnitType.PerService, 0),
            new(s, V, "Configuração de celular para idosos (voluntariado)", "Deixo o celular fácil de usar para os mais velhos.", "tecnologia-suporte", 0, null, ServiceUnitType.Hours, 60),
            new(s, T, "Frete de móvel dentro da cidade", "Transporto seu móvel com cuidado, na mesma cidade.", "transporte-fretes", 25, null, ServiceUnitType.PerService, 0),
            new(s, V, "Carona para o aeroporto (voluntariado)", "Levo na hora do voo, sem custo, ajuda mútua.", "transporte-fretes", 0, null, ServiceUnitType.PerService, 0),
            new(s, V, "Transporte de compras para idoso (voluntariado)", "Levo as compras até em casa para quem precisa.", "transporte-fretes", 0, null, ServiceUnitType.PerService, 0),
            new(s, T, "Mudança pequena (carro + ajuda)", "Mudança de poucos móveis, com meu apoio.", "transporte-fretes", 30, null, ServiceUnitType.PerService, 0),
            new(s, V, "Buscar encomenda nos correios (voluntariado)", "Retiro e entrego suas encomendas, de boa.", "transporte-fretes", 0, null, ServiceUnitType.PerService, 0),
            new(s, T, "Massagem relaxante", "Massagem para aliviar o estresse do dia.", "saude-bem-estar", 16, null, ServiceUnitType.Hours, 60),
            new(s, V, "Aula de yoga ao ar livre (voluntariado)", "Yoga coletiva no parque, todos bem-vindos.", "saude-bem-estar", 0, null, ServiceUnitType.Hours, 60),
            new(s, T, "Orientação nutricional inicial", "Primeira consulta para entender seus objetivos.", "saude-bem-estar", 14, null, ServiceUnitType.Hours, 45),
            new(s, V, "Acompanhante em consulta (apoio, voluntariado)", "Acompanho você numa consulta, apoio emocional.", "saude-bem-estar", 0, null, ServiceUnitType.PerService, 0),
            new(s, V, "Caminhada guiada em grupo (voluntariado)", "Caminhada saudável e conversa boa.", "saude-bem-estar", 0, null, ServiceUnitType.Hours, 60),
            new(s, T, "Decoração de festa infantil", "Decoro festas com balões e temas.", "eventos-festas", 28, null, ServiceUnitType.PerService, 0),
            new(s, V, "DJ para festa comunitária (voluntariado)", "Coloco música numa festa do bairro.", "eventos-festas", 0, null, ServiceUnitType.Hours, 240),
            new(s, T, "Garçom e bartender para evento", "Atendo seu evento com drinks e serviço.", "eventos-festas", 20, null, ServiceUnitType.Hours, 240),
            new(s, V, "Fotografia de aniversário (voluntariado)", "Registro seu aniversário com boas fotos.", "eventos-festas", 0, null, ServiceUnitType.Hours, 180),
            new(s, V, "Animação infantil — palhaço (voluntariado)", "Animo a festinha das crianças, de coração.", "eventos-festas", 0, null, ServiceUnitType.Hours, 120),
            new(s, T, "Bolos de pote (10 unidades)", "Bolos deliciosos para a sua festa.", "culinaria-confeitaria", 18, null, ServiceUnitType.PerService, 0),
            new(s, T, "Salgados para festa (cento)", "Coxinha, pastel e rissoles sob encomenda.", "culinaria-confeitaria", 22, null, ServiceUnitType.PerService, 0),
            new(s, V, "Marmitex saudável para a semana (voluntariado)", "Preparo marmitas para quem está precisando.", "culinaria-confeitaria", 0, null, ServiceUnitType.PerService, 0),
            new(s, T, "Bolo de aniversário decorado", "Bolo personalizado, sabor a combinar.", "culinaria-confeitaria", 30, null, ServiceUnitType.PerService, 0),
            new(s, V, "Sopa quente para a comunidade (voluntariado)", "Distribuo sopas para quem tem fome.", "culinaria-confeitaria", 0, null, ServiceUnitType.PerService, 0),
            new(s, T, "Criação de logo simples", "Logo limpa e profissional para o seu projeto.", "design-criacao", 25, null, ServiceUnitType.PerService, 0),
            new(s, T, "Cartão de visita digital", "Cartão interativo para compartilhar por link.", "design-criacao", 12, null, ServiceUnitType.PerService, 0),
            new(s, V, "Convite virtual para festa (voluntariado)", "Crio convites digitais para o seu evento.", "design-criacao", 0, null, ServiceUnitType.PerService, 0),
            new(s, V, "Edição de foto para perfil (voluntariado)", "Trato sua foto para um perfil caprichado.", "design-criacao", 0, null, ServiceUnitType.PerService, 0),
            new(s, T, "Declaração de Imposto de Renda (simples)", "Declaro seu IR sem complicação.", "administracao-contabilidade", 20, null, ServiceUnitType.PerService, 0),
            new(s, T, "Orientação para abertura de MEI", "Te guio na abertura do MEI, passo a passo.", "administracao-contabilidade", 15, null, ServiceUnitType.PerService, 0),
            new(s, V, "Organização de planilha financeira (voluntariado)", "Organizo suas contas numa planilha simples.", "administracao-contabilidade", 0, null, ServiceUnitType.Hours, 90),
            new(s, V, "Auxílio em currículo e LinkedIn (voluntariado)", "Ajudando você a se posicionar bem.", "administracao-contabilidade", 0, null, ServiceUnitType.PerService, 0),
            new(s, V, "Acompanhamento de idoso (compras, voluntariado)", "Acompanho e ajudo idosos nas tarefas do dia.", "ajuda-voluntariado", 0, null, ServiceUnitType.Hours, 120),
            new(s, V, "Visita e conversa a pessoa acamada (voluntariado)", "Visito e faço companhia a quem está de cama.", "ajuda-voluntariado", 0, null, ServiceUnitType.Hours, 60),
            new(s, V, "Passeio com cachorro no bairro (voluntariado)", "Passeio com seu pet quando você não puder.", "ajuda-voluntariado", 0, null, ServiceUnitType.Hours, 60),
            new(s, V, "Mutirão de limpeza de praça (voluntariado)", "Junta gente pra limpar a praça do bairro.", "ajuda-voluntariado", 0, null, ServiceUnitType.Hours, 180),
            new(s, V, "Distribuição de cestas básicas (voluntariado)", "Ajudo a levar cestas para famílias.", "ajuda-voluntariado", 0, null, ServiceUnitType.PerService, 0),
            new(s, V, "Tradução solidária de documentos (voluntariado)", "Traduzo documentos curtos para quem precisa.", "ajuda-voluntariado", 0, null, ServiceUnitType.PerService, 0),
            new(s, V, "Alongamento guiado para grupo de idosos (voluntariado)", "Alongamento leve para manter a mobilidade.", "saude-bem-estar", 0, null, ServiceUnitType.Hours, 60),
            new(s, V, "Oficina digital para ONG (voluntariado)", "Ensino noções de informática numa ONG.", "tecnologia-suporte", 0, null, ServiceUnitType.Hours, 120)
        };
    }

    // Elenco de 10 usuários mock determinísticos (mesmos IDs a cada seed). Avatar é usado nos
    // embeds (VendedorAvatarUrl, CriadorAvatarUrl, etc.) — o aggregate User não tem AvatarUrl.
    private static IReadOnlyList<MockUser> MockUsers()
    {
        var nomes = new[]
        {
            ("Marina Costa", "11", "São Paulo", "Pinheiros"),
            ("João Pereira", "21", "Rio de Janeiro", "Tijuca"),
            ("Ana Beatriz Rocha", "31", "Belo Horizonte", "Savassi"),
            ("Carlos Eduardo Lima", "81", "Recife", "Boa Viagem"),
            ("Fernanda Souza", "51", "Porto Alegre", "Moinhos de Vento"),
            ("Rafael Mendes", "11", "São Paulo", "Vila Mariana"),
            ("Juliana Almeida", "21", "Rio de Janeiro", "Copacabana"),
            ("Bruno Carvalho", "31", "Belo Horizonte", "Pampulha"),
            ("Patrícia Gomes", "71", "Salvador", "Barra"),
            ("Lucas Ferreira", "41", "Curitiba", "Batel")
        };

        var list = new List<MockUser>(nomes.Length);
        for (var i = 0; i < nomes.Length; i++)
        {
            var (nome, ddd, cidade, bairro) = nomes[i];
            var n = i + 1;
            list.Add(new MockUser(
                Id: DemoUserIds[i],
                Nome: nome,
                Email: $"mock{n:00}@revoa.dev",
                Telefone: $"+55{ddd}9{10000000 + n}",
                AvatarUrl: AvatarFor(nome),
                Cidade: cidade,
                Bairro: bairro));
        }

        return list;
    }

    private static IReadOnlyList<CommunitySpec> CommunitySpecs()
    {
        return new List<CommunitySpec>
        {
            new("Trocas no Centro", "Grupo para trocar e doar coisas no centro da cidade.",
                CommunityEixo.Geo, "São Paulo", "SP", "Pinheiros", -23.5641, -46.6361, 0),
            new("Doações Vila Mariana", "Solidariedade de quem mora na Vila Mariana e arredores.",
                CommunityEixo.Geo, "São Paulo", "SP", "Vila Mariana", -23.5868, -46.6353, 4),
            new("Reparos e Ajuda", "Conecta quem precisa de um reparo a quem sabe fazer.",
                CommunityEixo.Interesse, "Rio de Janeiro", "RJ", "Tijuca", -22.9230, -43.2340, 1),
            new("Mães da Comunidade", "Acolhimento, troca de roupinhas e dicas entre mães.",
                CommunityEixo.Causa, "Belo Horizonte", "MG", "Savassi", -19.9386, -43.9362, 2),
            new("Tech Solidário", "Voluntariado em tecnologia: informática, formatação e dicas.",
                CommunityEixo.Interesse, "Porto Alegre", "RS", "Moinhos de Vento", -30.0277, -51.2058, 9)
        };
    }

    private static IReadOnlyList<string> PostContents()
    {
        return new List<string>
        {
            "Alguém sabe onde descarto eletrônicos velhos aqui no bairro?",
            "Tenho roupas infantis G3-G4 para doar, alguém indica quem precisa?",
            "Ofereço aula de Excel aos sábados de manhã, é só chamar!",
            "Achei um gatinho na rua, alguém pode abrigar? Não dá pra ficar com ele.",
            "Mutirão de limpeza na praça sábado às 9h, quem topa?",
            "Preciso de uma carona pro centro amanhã cedo, divido a gasolina.",
            "Sobrou bastante comida da festa, alguém conhece uma instituição?",
            "Reparo de eletrodoméstico de graça pra quem tá apertado, me chamem.",
            "Troco livros de receita por HQs da Turma da Mônica!",
            "Procuro costureira para ajustes rápidos, pago em RVM ou troco por algo."
        };
    }

    private static IReadOnlyList<string> ReviewComments()
    {
        return new List<string>
        {
            "Ótimo vendedor, entregou rápido!",
            "Produto conforme descrito, recomendo!",
            "Pessoa muito atenciosa, troca super tranquila.",
            "Combinamos tudo certinho, recomendo demais.",
            "Demorou um pouquinho pra responder, mas fechou tudo ok.",
            "Produto em estado melhor do que eu esperava!",
            "Super prestativo, ajudou com a entrega.",
            "Ótima experiência, voltarei a negociar."
        };
    }

    private static IReadOnlyList<Place> Places()
    {
        return new List<Place>
        {
            new(-23.5586, -46.6315, "Pinheiros", "São Paulo", "05421-000"),
            new(-23.5868, -46.6353, "Vila Mariana", "São Paulo", "04027-000"),
            new(-23.5036, -46.6235, "Santana", "São Paulo", "02019-000"),
            new(-23.5641, -46.6361, "Liberdade", "São Paulo", "01505-000"),
            new(-22.9230, -43.2340, "Tijuca", "Rio de Janeiro", "20511-000"),
            new(-22.9711, -43.1822, "Copacabana", "Rio de Janeiro", "22010-000"),
            new(-22.8742, -43.3356, "Madureira", "Rio de Janeiro", "21351-000"),
            new(-22.9839, -43.2094, "Ipanema", "Rio de Janeiro", "22410-000"),
            new(-19.9386, -43.9362, "Savassi", "Belo Horizonte", "30130-000"),
            new(-19.8470, -43.9700, "Pampulha", "Belo Horizonte", "31275-000"),
            new(-19.9205, -43.9400, "Centro", "Belo Horizonte", "30160-000"),
            new(-8.1100, -34.9230, "Boa Viagem", "Recife", "51020-000"),
            new(-8.0380, -34.9180, "Casa Forte", "Recife", "52061-000"),
            new(-8.0615, -34.8730, "Recife Antigo", "Recife", "50030-000"),
            new(-30.0277, -51.2058, "Moinhos de Vento", "Porto Alegre", "90035-000"),
            new(-30.0350, -51.2270, "Cidade Baixa", "Porto Alegre", "90050-000"),
            new(-30.0330, -51.2140, "Bom Fim", "Porto Alegre", "90040-000"),
            new(-12.9780, -38.4650, "Barra", "Salvador", "40140-000"),
            new(-12.9980, -38.4580, "Pituba", "Salvador", "41810-000"),
            new(-25.4420, -49.2700, "Batel", "Curitiba", "80250-000"),
            new(-3.7350, -38.4920, "Aldeota", "Fortaleza", "60120-000")
        };
    }
}
