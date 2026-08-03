# ADR-0004: RVM como utility token (não ativo financeiro) — Lei 14.478

## Status
Accepted — baseline travado no Plano (§1, decisão #9). Análise jurídica formal deferida (pré-público).

## Contexto
O revoa emite RVM. A **Lei 14.478/2022** regula "ativos virtuais" e VASPs (autorização prévia, PLD,
crimes). Se o RVM for "ativo virtual" e o revoa for "VASP", o MVP privado fica inviável
(burocracia/autorização). Precisamos de um design que **mantenha o RVM fora do regime** no MVP.

## Decisão
Desenhar o RVM como **utility token / crédito de troca**, não ativo financeiro:
- **Não conversível em BRL** (sem on-ramp/off-ramp).
- **Sem propósito de investimento** (anti-mensagem; demurrage **queima**, não rende).
- **Emissão** só por faucet/cupom/admin (não compra com BRL).
- **Self-custody** (Safe) — revoa **não custodia** em nome de terceiros (não-VASP, Art. 5).
- UX: "crédito de troca", nunca "cripto/token/investimento".

**Tese de conformidade (Art. 3, III):** RVM dá acesso a produtos/serviços especificados na plataforma
— análogo a pontos/recompensas de fidelidade, **excluído** da definição de "ativo virtual".

## Alternativas consideradas
- **RVM atrelado ao BRL (stablecoin interna):** vira "pagamento/investimento" → ativo virtual → VASP.
- **RVM conversível (on/off-ramp):** idem + exige provedor de pagamento/KYC pesado.
- **Sem moeda (troca pura "sem dinheiro", trocadeira):** paralisa no desejo duplo coincidente (motivo do pivot).

## Consequências
- **+:** perfil regulatório baixo no MVP privado (utility + self-custody + sem on-ramp).
- **+:** CDC (Art. 13) + transparência (comparativo de preço) já cumprem boa-fé.
- **−:** sem conversibilidade = barreira para quem quer "sair" com BRL (aceita no MVP).
- **−:** parecer jurídico formal necessário antes do público; se adotar stablecoin depois, **reentra** no escopo VASP.
