using MediatR;

namespace Revoa.IntegrationContracts.Events;

// Evento de integração: premiação de bônus RVM a um usuário (ex.: recompensa de doação).
// O módulo Reputation publica; o módulo Token consome (mint via faucet). Mantém o isolamento:
// Reputation não referencia Token/Account/Exchange.
public sealed record RewardUserEvent(Guid UserId, long AmountRvm, string Reason) : INotification;
