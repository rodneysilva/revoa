using Revoa.Catalog.Domain.Aggregates.ListingAggregate;
using Revoa.Community.Domain.Aggregates.CommunityAggregate;

namespace Revoa.Api.Dev;

// Dados de demonstração do seed DEV (padrão static data table). Puro e sem I/O: elenco de usuários
// mock, specs de anúncios/comunidades, textos de posts/reviews e pontos geográficos. A orquestração
// que persiste tudo está no DevSeeder; o DevController só expõe o endpoint.
internal static class DevSeedData
{
    // Marca os listings demo — a limpeza idempotente varre por este prefixo na Descrição.
    public const string DemoPrefix = "[Demo]";

    /// <summary>Data/hora UTC nos últimos 30 dias (variedade temporal p/ o feed e timelines).</summary>
    public static DateTime RandomRecent(Random rnd) =>
        DateTime.SpecifyKind(
            DateTime.UtcNow.Date
                .AddDays(-rnd.Next(0, 30))
                .AddHours(rnd.Next(8, 22))
                .AddMinutes(rnd.Next(0, 60)),
            DateTimeKind.Utc);

    public sealed record SeedListing(
        ListingKind Kind,
        ListingMode Mode,
        string Title,
        string Description,
        string CategorySlug,
        long PriceRvm,
        ProductCondition? Condition,
        ServiceUnitType? UnitType,
        int Duration);

    // Usuário mock (não é o aggregate User — é o "elenco" com avatar embutido usado nos embeds).
    public sealed record MockUser(
        Guid Id,
        string Name,
        string Email,
        string Phone,
        string AvatarUrl,
        string City,
        string Neighborhood);

    public sealed record CommunitySpec(
        string Name,
        string Description,
        CommunityAxis Axis,
        string City,
        string State,
        string Neighborhood,
        double Lat,
        double Lng,
        int CriadorIndex);

    public sealed record Place(double Lat, double Lng, string Neighborhood, string City, string PostalCode);

    // 10 chaves determinísticas (idempotência: mesma seed → mesmos IDs).
    public static readonly Guid[] DemoUserIds =
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
    public static readonly Guid[] DemoCommunityIds =
    {
        new("aabbccdd-1001-4000-8000-000000000001"),
        new("aabbccdd-1002-4000-8000-000000000002"),
        new("aabbccdd-1003-4000-8000-000000000003"),
        new("aabbccdd-1004-4000-8000-000000000004"),
        new("aabbccdd-1005-4000-8000-000000000005")
    };

    public static string AvatarFor(string name) =>
        $"https://api.dicebear.com/7.x/initials/svg?seed={Uri.EscapeDataString(name)}";

    public static IReadOnlyList<SeedListing> BuildProducts()
    {
        var p = ListingKind.Product;
        var T = ListingMode.Trade;
        var R = ListingMode.Resell;
        var D = ListingMode.Donate;

        return new List<SeedListing>
        {
            new(p, T, "Notebook Dell usado — 8GB SSD240", "Notebook funcional, bateria com boa autonomia. Retirada no neighborhood.", "eletronicos", 35, ProductCondition.Seminovo, null, 0),
            new(p, T, "Celular Moto G usado 64GB", "Funcionando, leves marcas de uso. Tela sem trincos.", "eletronicos", 28, ProductCondition.Usado, null, 0),
            new(p, T, "Smart TV LED 32 polegadas", "TV em ótimo state, controle incluso. Retirada a combinar.", "eletronicos", 40, ProductCondition.Usado, null, 0),
            new(p, R, "Carregador portátil 10000mAh", "Power bank seminovo, carrega dois aparelhos.", "eletronicos", 4, ProductCondition.Seminovo, null, 0),
            new(p, D, "Fone bluetooth (doação)", "Funciona bem, na caixa. Levo para quem precisar.", "eletronicos", 0, ProductCondition.Usado, null, 0),
            new(p, T, "Cadeira de escritório ergonômica", "Cadeira confortável, rodízios ok. Ótima para home office.", "moveis-decoracao", 22, ProductCondition.Seminovo, null, 0),
            new(p, D, "Sofá de 3 lugares (doação)", "Sofá usado em bom state, só retirar. Combinamos horário.", "moveis-decoracao", 0, ProductCondition.Usado, null, 0),
            new(p, T, "Mesa de jantar de madeira maciça", "Mesa espaçosa para 6 pessoas. Retirada no local.", "moveis-decoracao", 30, ProductCondition.Usado, null, 0),
            new(p, R, "Rack de TV pequeno", "Rack em MDF, comporta até 42 polegadas.", "moveis-decoracao", 6, ProductCondition.Usado, null, 0),
            new(p, D, "Abajur decorativo (doação)", "Abajur funcional, perfeito para o canto da sala.", "moveis-decoracao", 0, ProductCondition.Usado, null, 0),
            new(p, T, "Jaqueta jeans masculina tam M", "Pouco uso, sem defeitos. Vai bem em qualquer frio.", "roupas-acessorios", 8, ProductCondition.Seminovo, null, 0),
            new(p, D, "Vestido floral tam G (doação)", "Vestido bonito, usado poucas vezes. Para quem servir.", "roupas-acessorios", 0, ProductCondition.Seminovo, null, 0),
            new(p, R, "Tênis esportivo nº 39", "Tênis usado, mas com solado íntegro.", "roupas-acessorios", 5, ProductCondition.Usado, null, 0),
            new(p, T, "Mochila universitária reforçada", "Mochila espaçosa, com compartimento para notebook.", "roupas-acessorios", 10, ProductCondition.Seminovo, null, 0),
            new(p, D, "Roupas infantis 2-3 anos (doação)", "Várias peças em bom state. Para famílias que precisam.", "roupas-acessorios", 0, ProductCondition.Usado, null, 0),
            new(p, T, "Liquidificador 3 velocidades", "Funciona perfeitamente, copo sem trincos.", "casa-cozinha", 9, ProductCondition.Usado, null, 0),
            new(p, R, "Jogo de panelas antiaderente (4 peças)", "Panelas em state razoável, ótimas para começar.", "casa-cozinha", 7, ProductCondition.Usado, null, 0),
            new(p, D, "Liquidificador manual (doação)", "Minipimer funcionando, para quem está montando casa.", "casa-cozinha", 0, ProductCondition.Usado, null, 0),
            new(p, T, "Cafeteira elétrica", "Cafeteira seminova, faz café rápido.", "casa-cozinha", 6, ProductCondition.Seminovo, null, 0),
            new(p, T, "Air Fryer 3,2 litros", "Fritadeira sem óleo, super conservada.", "casa-cozinha", 18, ProductCondition.Seminovo, null, 0),
            new(p, D, "Livros de literatura (lote, doação)", "6 livros clássicos para circular o conhecimento.", "livros-midia", 0, ProductCondition.Usado, null, 0),
            new(p, D, "Apostilas de ENEM usadas (doação)", "Material para quem está se preparando.", "livros-midia", 0, ProductCondition.Usado, null, 0),
            new(p, R, "HQ Turma da Mônica (coleção)", "Várias edições em bom state, nostalgia garantida.", "livros-midia", 3, ProductCondition.Usado, null, 0),
            new(p, T, "Livro A Arte da Guerra", "Edição de bolso, capa em ótimo state.", "livros-midia", 4, ProductCondition.Seminovo, null, 0),
            new(p, T, "Bicicleta aro 26 revisada", "Bike calibrada e com freios ajustados. Pronta pra rodar.", "esporte-lazer", 32, ProductCondition.Usado, null, 0),
            new(p, T, "Par de halteres 10kg", "Halteres de ferro, ótimos para treino em casa.", "esporte-lazer", 14, ProductCondition.Usado, null, 0),
            new(p, R, "Prancha de surfe usada", "Prancha em state razoável, ótima para iniciantes.", "esporte-lazer", 8, ProductCondition.Usado, null, 0),
            new(p, D, "Bola de futebol society (doação)", "Bola em condição de uso, para a pelada do neighborhood.", "esporte-lazer", 0, ProductCondition.Usado, null, 0),
            new(p, D, "Jogo de damas de madeira (doação)", "Tabuleiro completo para o lazer em família.", "esporte-lazer", 0, ProductCondition.Usado, null, 0),
            new(p, D, "Carrinho de controle remoto (doação)", "Funciona, vai alegrar uma criança.", "brinquedos-infantil", 0, ProductCondition.Usado, null, 0),
            new(p, R, "Blocos de montar (lote grande)", "Várias peças de encaixar, criatividade sem limite.", "brinquedos-infantil", 5, ProductCondition.Usado, null, 0),
            new(p, D, "Boneca de pano artesanal (doação)", "Feita à mão, novinha. Linda para presentear.", "brinquedos-infantil", 0, ProductCondition.Seminovo, null, 0),
            new(p, T, "Triciclo infantil", "Triciclo em bom state, ideal de 2 a 5 anos.", "brinquedos-infantil", 12, ProductCondition.Usado, null, 0),
            new(p, T, "Furadeira de impacto 13mm", "Furadeira potente com brocas, super conservada.", "ferramentas", 20, ProductCondition.Seminovo, null, 0),
            new(p, R, "Caixa de ferramentas com kit", "Kit completo para pequenos reparos domésticos.", "ferramentas", 6, ProductCondition.Usado, null, 0),
            new(p, D, "Martelo e alicate (doação)", "Ferramentas básicas para quem está começando.", "ferramentas", 0, ProductCondition.Usado, null, 0),
            new(p, T, "Escada de alumínio 4 degraus", "Escada leve e segura, ótima manutenção.", "ferramentas", 15, ProductCondition.Usado, null, 0),
            new(p, D, "Muda de costela-de-adão (doação)", "Planta saudável para deixar a casa verde.", "jardim-plantas", 0, ProductCondition.Novo, null, 0),
            new(p, T, "Vaso de cerâmica grande", "Vaso decorativo, combina com qualquer ambiente.", "jardim-plantas", 11, ProductCondition.Usado, null, 0),
            new(p, D, "Mudas de manjericão e salsa (doação)", "Hortaliças para começar sua horta em casa.", "jardim-plantas", 0, ProductCondition.Novo, null, 0),
            new(p, R, "Mangueira de jardim 15m", "Mangueira em bom state, sem vazamentos.", "jardim-plantas", 3, ProductCondition.Usado, null, 0),
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
            new(p, D, "Flauta doce Yamaha (doação)", "Flauta em ótimo state, perfeita para escola.", "instrumentos-musicais", 0, ProductCondition.Seminovo, null, 0),
            new(p, R, "Kit palhetas de saxofone", "Palhetas novas na embalagem.", "instrumentos-musicais", 2, ProductCondition.Novo, null, 0),
            new(p, R, "Webcam HD 720p", "Webcam seminova, ótima para reuniões online.", "eletronicos", 3, ProductCondition.Seminovo, null, 0),
            new(p, D, "Sanduicheira (doação)", "Funciona bem, para quem está montando a cozinha.", "casa-cozinha", 0, ProductCondition.Usado, null, 0),
            new(p, D, "Livros infantis ilustrados (lote, doação)", "Vários livrinhos para despertar a leitura.", "livros-midia", 0, ProductCondition.Usado, null, 0)
        };
    }

    public static IReadOnlyList<SeedListing> BuildServices()
    {
        var s = ListingKind.Service;
        var T = ListingMode.Trade;
        var V = ListingMode.Volunteer;

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
            new(s, T, "Frete de móvel dentro da city", "Transporto seu móvel com cuidado, na mesma city.", "transporte-fretes", 25, null, ServiceUnitType.PerService, 0),
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
            new(s, V, "DJ para festa comunitária (voluntariado)", "Coloco música numa festa do neighborhood.", "eventos-festas", 0, null, ServiceUnitType.Hours, 240),
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
            new(s, V, "Passeio com cachorro no neighborhood (voluntariado)", "Passeio com seu pet quando você não puder.", "ajuda-voluntariado", 0, null, ServiceUnitType.Hours, 60),
            new(s, V, "Mutirão de limpeza de praça (voluntariado)", "Junta gente pra limpar a praça do neighborhood.", "ajuda-voluntariado", 0, null, ServiceUnitType.Hours, 180),
            new(s, V, "Distribuição de cestas básicas (voluntariado)", "Ajudo a levar cestas para famílias.", "ajuda-voluntariado", 0, null, ServiceUnitType.PerService, 0),
            new(s, V, "Tradução solidária de documentos (voluntariado)", "Traduzo documentos curtos para quem precisa.", "ajuda-voluntariado", 0, null, ServiceUnitType.PerService, 0),
            new(s, V, "Alongamento guiado para grupo de idosos (voluntariado)", "Alongamento leve para manter a mobilidade.", "saude-bem-estar", 0, null, ServiceUnitType.Hours, 60),
            new(s, V, "Oficina digital para ONG (voluntariado)", "Ensino noções de informática numa ONG.", "tecnologia-suporte", 0, null, ServiceUnitType.Hours, 120)
        };
    }

    // Elenco de 10 usuários mock determinísticos (mesmos IDs a cada seed). Avatar é usado nos
    // embeds (SellerAvatarUrl, CreatorAvatarUrl, etc.) — o aggregate User não tem AvatarUrl.
    public static IReadOnlyList<MockUser> MockUsers()
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
            var (name, ddd, city, neighborhood) = nomes[i];
            var n = i + 1;
            list.Add(new MockUser(
                Id: DemoUserIds[i],
                Name: name,
                Email: $"mock{n:00}@revoa.dev",
                Phone: $"+55{ddd}9{10000000 + n}",
                AvatarUrl: AvatarFor(name),
                City: city,
                Neighborhood: neighborhood));
        }

        return list;
    }

    public static IReadOnlyList<CommunitySpec> CommunitySpecs()
    {
        return new List<CommunitySpec>
        {
            new("Trocas no Centro", "Grupo para trocar e doar coisas no centro da city.",
                CommunityAxis.Geo, "São Paulo", "SP", "Pinheiros", -23.5641, -46.6361, 0),
            new("Doações Vila Mariana", "Solidariedade de quem mora na Vila Mariana e arredores.",
                CommunityAxis.Geo, "São Paulo", "SP", "Vila Mariana", -23.5868, -46.6353, 4),
            new("Reparos e Ajuda", "Conecta quem precisa de um reparo a quem sabe fazer.",
                CommunityAxis.Interest, "Rio de Janeiro", "RJ", "Tijuca", -22.9230, -43.2340, 1),
            new("Mães da Comunidade", "Acolhimento, troca de roupinhas e dicas entre mães.",
                CommunityAxis.Cause, "Belo Horizonte", "MG", "Savassi", -19.9386, -43.9362, 2),
            new("Tech Solidário", "Voluntariado em tecnologia: informática, formatação e dicas.",
                CommunityAxis.Interest, "Porto Alegre", "RS", "Moinhos de Vento", -30.0277, -51.2058, 9)
        };
    }

    public static IReadOnlyList<string> PostContents()
    {
        return new List<string>
        {
            "Alguém sabe onde descarto eletrônicos velhos aqui no neighborhood?",
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

    public static IReadOnlyList<string> ReviewComments()
    {
        return new List<string>
        {
            "Ótimo vendedor, entregou rápido!",
            "Produto conforme descrito, recomendo!",
            "Pessoa muito atenciosa, troca super tranquila.",
            "Combinamos tudo certinho, recomendo demais.",
            "Demorou um pouquinho pra responder, mas fechou tudo ok.",
            "Produto em state melhor do que eu esperava!",
            "Super prestativo, ajudou com a entrega.",
            "Ótima experiência, voltarei a negociar."
        };
    }

    public static IReadOnlyList<Place> Places()
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
