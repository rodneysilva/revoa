# ADR-0017: Contrato HTTP em PascalCase com envelope único ApiError

## Status
Accepted — implementado na refactoração de coerência (Fase 3), FE+BE atômicos.

## Contexto
O contrato HTTP tinha 3 formatos de erro diferentes (anonymous `new { error }` lowercase em ~90
sites, ProblemDetails do model binding, e o middleware inline `{ error, errors[] }` lowercase),
DTOs PascalCase convivendo com resultados anônimos lowercase (`new { id }`, `new { message }`,
`new { ok = true }`), e identificadores PT/EN misturados no mesmo tipo (`SellerId` +
`SellerNome`, `TradeState.Ofertada` vs `TradeKind.Product`). O frontend mantinha parsing
defensivo (`client.ts`) para absorver a inconsistência — e o casing já tinha quebrado o chat
uma vez (commit `4ecbbc6`).

## Decisão
1. **PascalCase em todo o corpo de resposta** (política ativa do projeto aplicada ao fim):
   - **UM formato de erro**: `ApiError(string Error, FieldError[]? Errors = null)` em
     `Revoa.Abstractions`, com `FieldError(string Field, string Message)`. `Errors` serializa
     só quando existe (JsonIgnore WhenWritingNull).
   - Sucessos: **DTO tipado ou 204** — `ResourceId(Guid Id)` para `new { id }`, `ApiMessage`
     para mensagens, DTOs por domínio (ex.: `BrlRateDto`). Nada de anonymous lowercase.
2. **`ApiExceptionMiddleware`** (substitui o bloco inline do `Program.cs`):
   `ValidationException`→400 com `Errors[]` (pipeline FluentValidation), `DomainException`→400,
   `Concurrency`/`DuplicateKey`→409, não mapeadas→500 logado sem vazar stack.
3. **Identificadores do contrato em inglês** (regra "inglês no código"): `SellerNome`→
   `SellerName`, `Modo`→`Mode`, `TradeState.Ofertada`→`Offered` etc. O **frontend renderiza
   rótulos PT via mapa código→label** (chave EN, valor pt-BR). Campos BSON renomeados migram
   via `scripts/migrate-rename-fields.js` (`$rename` idempotente); enums renomeados mantêm a
   ordem numérica (storage-safe, só a string da wire muda).
4. Query params também em EN (`radius`, `categoryId`, `sellerIds`, ...) — binding é
   case-insensitive, o consumidor único é o próprio FE.

## Alternativas consideradas
- **camelCase (convenção REST/JS):** contradiria a política MongoDB PascalCase do projeto
  (ADR-0003) e duplicaria a superfície de convenções.
- **Manter lowercase + tolerância no FE:** o parsing defensivo era sintoma; cada endpoint novo
  reinventava o formato.

## Consequências
- **+:** um só contrato de erro; typecheck do FE (`tsc`) valida o contrato inteiro por
  construção; validação do pipeline deixa de virar 500.
- **−:** renomeações de propriedade = migração de campo BSON (script obrigatório na entrega).
- **−:** breaking change para qualquer consumidor externo (nenhum existe hoje — FE é o único).
