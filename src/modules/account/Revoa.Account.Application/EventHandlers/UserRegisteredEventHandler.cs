using MediatR;
using Nethereum.Signer;
using Nethereum.Web3.Accounts;
using Revoa.Abstractions;
using Revoa.Account.Domain.Aggregates.AccountAggregate;
using Revoa.Account.Domain.Repositories;
using Revoa.IntegrationContracts.Events;

namespace Revoa.Account.Application.EventHandlers;

// Reage ao cadastro de usuário (Identity) e provisiona a carteira EOA do usuário.
// Cadeia: RegisterUser → UserRegisteredEvent → [aqui] cria Account → WalletCreatedEvent → Token (faucet).
public class UserRegisteredEventHandler : INotificationHandler<UserRegisteredEvent>
{
    private readonly IAccountRepository _accounts;
    private readonly IIntegrationEventBus _eventBus;

    public UserRegisteredEventHandler(IAccountRepository accounts, IIntegrationEventBus eventBus)
    {
        _accounts = accounts;
        _eventBus = eventBus;
    }

    public async Task Handle(UserRegisteredEvent notification, CancellationToken ct)
    {
        // Idempotência: se já existe carteira para o usuário, apenas republica o evento.
        var existing = await _accounts.GetByUserIdAsync(notification.UserId, ct);
        if (existing is not null)
        {
            await _eventBus.PublishAsync(
                new WalletCreatedEvent(notification.UserId, existing.WalletAddress), ct);
            return;
        }

        // PROVISÓRIO (MVP dev): gera EOA com chave aleatória guardada pelo backend.
        var ecKey = EthECKey.GenerateKey();
        var privateKey = ecKey.GetPrivateKey(); // já vem com prefixo 0x
        var eoa = new Nethereum.Web3.Accounts.Account(privateKey);
        var walletAddress = eoa.Address;

        var aggregate = UserAccount.Create(
            notification.UserId, walletAddress, privateKey);

        await _accounts.AddAsync(aggregate, ct);

        await _eventBus.PublishAsync(
            new WalletCreatedEvent(notification.UserId, walletAddress), ct);
    }
}
