using MediatR;

namespace Revoa.IntegrationContracts.Events;

// Evento de integração: pedido de banimento de usuário (UF-25). Publicado pelo módulo Moderation
// quando um admin resolve uma denúncia com ação Banned sobre TargetType==User. Consumido pelo módulo
// Identity (UserBanRequestedEventHandler), que chama User.Ban() — mantém o Moderation isolado do
// repositório de usuários (comunicação só via barramento de eventos).
public sealed record UserBanRequestedEvent(
    Guid UserId,
    string Reason) : INotification;
