# ADR-0010: Doação/Voluntariado com recompensa multi-eixo (bônus RVM admin-configurável)

## Status
Accepted — decisão v3.0 (doação como produto de primeira classe).

## Contexto
A doação/voluntariado (ajuda mútua) era periférica (só "bônus reputacional"). O Buy Nothing Project
(14M+ membros) prova que **doar de graça** é produto viável em escala. O revoa precisa celebrar a ajuda
mútua sem deixá-la em segundo plano, mas **sem transformar doação em transação financeira** (o que
arruinaria o enquadramento de utility token e o "sem fins lucrativos").

## Decisão
**Doação (produto) e Voluntariado (serviço) são produtos de primeira classe**, coexistindo com a troca
em RVM. Reusam o EscrowVault/voucher com **valor 0** (mesma atomicidade; sem RVM do receptor). O
doador/voluntário recebe **recompensa multi-eixo**:
1. **Reputação/badges** (avaliação + selos 🎁 doador / 🤝 voluntário).
2. **Bônus de RVM** creditado ao doador/voluntário — **configurável na área administrativa**
   (`DonationReward:BonusRvm` por kind/modo).
3. **Pontos de ajuda** (contador separado, não-RVM, não conversível; ranking comunitário).

O receptor manifesta interesse (**Pedido de Ajuda / Ask**); o doador **escolhe** o receptor (curadoria
humana: proximidade, reputação, mensagem).

## Alternativas consideradas
- **Só reputação (v2.0):** incentivo fraco; doação periférica.
- **Bônus de RVM fixo em código:** pouco flexível; exige deploy a cada ajuste. (Rejeitado → admin-configurável.)
- **Doação movimentando RVM (receptor paga):** vira transação; quebra enquadramento. (Rejeitado.)

## Consequências
- **+:** ajuda mútua celebrada e flexível (admin calibra incentivo sem código); matching humano (curadoria).
- **+:** doação não é transação financeira → preserva enquadramento utility token + sem FLP.
- **−:** 3 eixos de recompensa a manter/testar (reputação, RVM, pontos); risco de "farmar doação" (mitigado: anti-sybil + curadoria humana + thresholds).
- **−:** reusar escrow com valor 0 exige cuidado para não confundir com Troca (documentado no AGENTS.md).
