using MediatR;

namespace Revoa.IntegrationContracts.Events;

// Evento de integração: doação/voluntariado concluído (trade total 0 liberado).
// Consumido pelos módulos Reputation/Token na Fase 3 (recompensa multi-eixo: reputação +
// bônus RVM admin-configurável + pontos de ajuda). MVP: SEM handler (stub).
public sealed record DonationCompletedEvent(
    Guid TradeId,
    Guid ListingId,
    Guid DonorId,
    Guid ReceptorId,
    string Modo) : INotification;
