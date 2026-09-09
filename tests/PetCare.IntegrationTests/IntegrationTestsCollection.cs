using Xunit;

namespace PetCare.IntegrationTests;

/// <summary>
/// Define uma coleção de testes que compartilha uma única instância de
/// CustomWebApplicationFactory (e do mesmo banco InMemory) entre várias classes,
/// evitando recriar o WebApplicationFactory a cada classe de teste.
/// </summary>
[CollectionDefinition("IntegrationTests")]
public class IntegrationTestsCollection : ICollectionFixture<CustomWebApplicationFactory>
{
    // Esta classe não precisa de implementação: serve apenas para aplicar
    // [CollectionDefinition] e o ICollectionFixture<T> combinados.
}