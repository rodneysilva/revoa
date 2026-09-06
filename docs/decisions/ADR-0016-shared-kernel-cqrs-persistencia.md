# ADR-0016: Shared kernel de CQRS e persistência (matar a duplicação sistêmica)

## Status
Accepted — implementado na refactoração de coerência (Fase 2).

## Contexto
A auditoria de coerência mostrou ~600–800 linhas de cola idêntica copiada à mão em cada módulo:
`ValidationBehavior<>` ×5 cópias (e 8 módulos **sem** pipeline de validação — validators
adicionados depois nunca rodariam), o bloco de optimistic locking ~10 linhas ×11 repositórios,
142 linhas de wiring manual de índices no `Program.cs`, `ReadUser()` de claims ×7 controllers,
e o seed de categorias definido 2× com conteúdo divergente (9 no startup vs 22 no dev).

Todo módulo novo re-copiava ~200 linhas — a sensação de "projeto incoerente" vinha daqui.

## Decisão
Estender o que já existia em `src/shared/` (não inventar estrutura nova):
- **`Revoa.Application`** (novo): `ValidationBehavior<,>` canônico + `AddRevoaCQRS(params
  Assembly[])` (MediatR + validators + behavior abertos em uma linha por módulo).
- **`Revoa.Infrastructure`**: `MongoRepositoryBase<T>` com o optimistic locking
  (`Version` filter + `IncrementVersion` + `ConcurrencyException`) **uma única vez**; repositórios
  herdam e mantêm só queries customizadas. `IMongoIndexEnsurer`: cada repo se registra no DI e o
  `Program.cs` faz um loop (com try/catch **por ensurer** — um índice quebrado não aborta os outros).
- **`ClaimsPrincipalExtensions.GetRevoaUser()`** (Revoa.Api): `sub`/`name`/`nickname`/`avatar`
  em um record tipado — mata os helpers duplicados nos controllers.
- **`CategorySeed`** no módulo Catalog: lista canônica única (22 categorias com descrição)
  consumida pelo startup **e** pelo DevController.

## Alternativas consideradas
- **Continuar com cópias por módulo:** consistência decorreria de disciplina manual; já tinha
  divergido (8 módulos sem validação).
- **Framework externo de modularidade (ex. Autofac modules):** over-engineering; o DI nativo
  + extension methods resolve.

## Consequências
- **+:** regra muda em um lugar (ex.: política de concorrência); módulo novo nasce com pipeline
  completo; `Program.cs` deixou de conhecer repositórios concretos.
- **+:** −460 linhas líquidas na entrega inicial.
- **−:** shared kernel é acoplamento estrutural — mudanças nele afetam os 13 módulos (compensado
  pelos gates de build/teste).
