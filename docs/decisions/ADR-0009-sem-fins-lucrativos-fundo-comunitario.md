# ADR-0009: Sem fins lucrativos — taxa 2% vira Fundo Comunitário

## Status
Accepted — decisão v3.0 (rodada sem fins lucrativos).

## Contexto
O revoa é uma iniciativa **sem fins lucrativos** (economia solidária + ajuda mútua). O modelo anterior
(v2.0) tinha uma taxa de 2% creditada a uma "Tesouraria", o que carregava conotação de lucro/acionistas.
Precisávamos de um modelo que (a) sustente a infra, (b) não seja lucro, (c) seja transparente e (d)
reforce a narrativa de moeda social comunitária.

## Decisão
- **Manter a taxa de 2%** por troca finalizada (do vendedor), mas creditá-la ao **Fundo Comunitário**
  (antes "Tesouraria") — **sem fins lucrativos**, reinvestida na operação (infra/servidores).
- **Prestação de contas pública** no `revoa.org` (transparência/impacto).
- Doação/voluntariado **não pagam taxa** (preço 0).
- Contrato on-chain mantém o nome técnico `Treasury`, mas representa o **Fundo Comunitário**.

## Alternativas consideradas
- **Taxa zero (0%):** sustentação só por doações/apoio externo — arriscado sem base instalada. (Rejeitado.)
- **Taxa voluntária:** imprevisível, não sustenta infra estavelmente. (Rejeitado.)
- **Queimar a taxa (deflacionário):** não custeia a operação. (Rejeitado.)

## Consequências
- **+:** sustentação da infra sem lucro; narrativa reforçada (comunidade, não empresa).
- **+:** transparência (`revoa.org`) gera confiança e habilita doações institucionis pós-formalização.
- **−:** sem receita de lucro, depende de escala + apoio externo para crescer.
- **−:** formalizar associação/OSC (CNPJ) antes do público habilita captação formal (doações, editais).
