using PetCare.Application.Common;
using PetCare.Domain.Entities;
using PetCare.Infrastructure.Repositories;
using Xunit;

namespace PetCare.Infrastructure.Tests;

public class RepositoryPagingTests
{
    private static TutorRepository CriarRepositorioComCincoTutores()
    {
        var context = InMemoryContextFactory.Create();
        foreach (var nome in new[] { "Ana", "Bruno", "Carla", "Diego", "Elisa" })
            context.Set<Tutor>().Add(new Tutor(nome, $"{nome.ToLower()}@email.com", null));
        context.SaveChanges();
        return new TutorRepository(context);
    }

    [Fact]
    public async Task GetPagedAsync_SegundaPagina_RetornaItensCorretosETotal()
    {
        // Arrange
        var repository = CriarRepositorioComCincoTutores();
        var query = new QueryParameters { Page = 2, PageSize = 2, SortBy = "Nome" };

        // Act
        var pagina = await repository.GetPagedAsync(query);

        // Assert
        Assert.Equal(5, pagina.TotalCount);
        Assert.Equal(3, pagina.TotalPages);
        Assert.Equal(new[] { "Carla", "Diego" }, pagina.Items.Select(t => t.Nome));
    }

    [Fact]
    public async Task GetPagedAsync_OrdenacaoDescendente_InverteOrdem()
    {
        // Arrange
        var repository = CriarRepositorioComCincoTutores();
        var query = new QueryParameters { PageSize = 2, SortBy = "nome", SortDir = "desc" };

        // Act
        var pagina = await repository.GetPagedAsync(query);

        // Assert
        Assert.Equal(new[] { "Elisa", "Diego" }, pagina.Items.Select(t => t.Nome));
    }

    [Fact]
    public async Task GetPagedAsync_ComFiltro_AplicaPredicadoAntesDaPaginacao()
    {
        // Arrange
        var repository = CriarRepositorioComCincoTutores();
        var query = new QueryParameters { PageSize = 10 };

        // Act
        var pagina = await repository.GetPagedAsync(query, t => t.Nome.StartsWith("C") || t.Nome.StartsWith("D"));

        // Assert
        Assert.Equal(2, pagina.TotalCount);
    }

    [Fact]
    public async Task GetPagedAsync_CampoDeOrdenacaoInvalido_LancaArgumentException()
    {
        // Arrange
        var repository = CriarRepositorioComCincoTutores();
        var query = new QueryParameters { SortBy = "Pets" }; // navegação: não permitido

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => repository.GetPagedAsync(query));
    }
}