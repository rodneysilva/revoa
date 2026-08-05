using MediatR;

namespace Revoa.IntegrationContracts.Events;

// Evento de integração: avaliação pós-troca enviada (UF-23). Consumido pelo módulo Reputation
// para alimentar a média de avaliações do usuário avaliado (reputation.ApplyReview).
public sealed record ReviewSubmittedEvent(
    Guid RevieweeId,
    int Rating) : INotification;
