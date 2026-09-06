namespace Revoa.IntegrationTests.Harness;

// Usuário autenticado para um teste: e-mail + JWT (Bearer) + UserId (claim sub).
public sealed record TestUser(string Email, string Token, Guid UserId, string Name);
