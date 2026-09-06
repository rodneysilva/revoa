namespace Revoa.Catalog.Domain.Aggregates.CategoryAggregate;

/// <summary>
/// Seed canônico de categorias (12 de produto + 10 de serviço) — fonte ÚNICA da verdade,
/// consumida pelo CategoriesRepository.EnsureSeedAsync (startup) e pelo DevController (seed dev).
/// </summary>
public static class CategorySeed
{
    public sealed record Item(string Name, string Slug, string? Description);

    public static readonly IReadOnlyList<Item> All = new Item[]
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
}
