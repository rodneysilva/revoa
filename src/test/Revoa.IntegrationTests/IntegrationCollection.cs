using Revoa.IntegrationTests.Harness;
using Xunit;

namespace Revoa.IntegrationTests;

// Coleção compartilhada: UM container Mongo + UM host por execução de testes (mais rápido). O
// reset entre testes garante isolamento de dados. xUnit serializa testes da mesma coleção.
[CollectionDefinition("Integration")]
public sealed class IntegrationCollection : ICollectionFixture<ApiFactory> { }
