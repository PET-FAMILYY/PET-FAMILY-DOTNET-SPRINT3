using PetCare.Application.Common;
using Xunit;

namespace PetCare.Application.Tests;

public class PagedResultTests
{
    [Theory]
    [InlineData(0, 10, 0)]
    [InlineData(10, 10, 1)]
    [InlineData(11, 10, 2)]
    [InlineData(95, 10, 10)]
    public void TotalPages_CalculaPaginasCorretamente(int total, int pageSize, int esperado)
    {
        // Arrange
        var pagina = new PagedResult<int>(new int[0], 1, pageSize, total);

        // Act
        var totalPages = pagina.TotalPages;

        // Assert
        Assert.Equal(esperado, totalPages);
    }

    [Fact]
    public void Map_PreservaMetadadosETransformaItens()
    {
        // Arrange
        var pagina = new PagedResult<int>(new[] { 1, 2, 3 }, 2, 3, 9);

        // Act
        var mapeada = pagina.Map(i => $"#{i}");

        // Assert
        Assert.Equal(new[] { "#1", "#2", "#3" }, mapeada.Items);
        Assert.Equal(2, mapeada.Page);
        Assert.Equal(9, mapeada.TotalCount);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-5, 1)]
    [InlineData(3, 3)]
    public void QueryParameters_PageNaoPodeSerMenorQueUm(int informado, int esperado)
    {
        // Arrange & Act
        var query = new QueryParameters { Page = informado };

        // Assert
        Assert.Equal(esperado, query.Page);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1000, QueryParameters.MaxPageSize)]
    [InlineData(20, 20)]
    public void QueryParameters_PageSizeFicaDentroDoLimite(int informado, int esperado)
    {
        // Arrange & Act
        var query = new QueryParameters { PageSize = informado };

        // Assert
        Assert.Equal(esperado, query.PageSize);
    }

    [Theory]
    [InlineData("desc", true)]
    [InlineData("DESC", true)]
    [InlineData("asc", false)]
    public void QueryParameters_Descending_ReconheceDirecao(string direcao, bool esperado)
    {
        // Arrange & Act
        var query = new QueryParameters { SortDir = direcao };

        // Assert
        Assert.Equal(esperado, query.Descending);
    }
}