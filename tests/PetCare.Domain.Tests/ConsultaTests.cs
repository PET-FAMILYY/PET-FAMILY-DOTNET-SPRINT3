using System;
using PetCare.Domain.Entities;
using PetCare.Domain.Exceptions;
using Xunit;

namespace PetCare.Domain.Tests;

public class ConsultaTests
{
    [Fact]
    public void Construtor_DadosValidos_CriaConsultaComSucesso()
    {
        // Arrange
        var dataConsulta = DateTime.UtcNow.AddDays(1);
        var observacoes = "Consulta de rotina";
        var petId = 1;

        // Act
        var consulta = new Consulta(dataConsulta, observacoes, petId);

        // Assert
        Assert.Equal(dataConsulta, consulta.DataConsulta);
        Assert.Equal(observacoes, consulta.Observacoes);
        Assert.Equal(petId, consulta.PetId);
    }

    [Fact]
    public void Construtor_DataConsultaInvalida_LancaDomainException()
    {
        // Arrange
        var dataInvalida = default(DateTime);

        // Act
        var exception = Record.Exception(() => new Consulta(dataInvalida, "obs", 1));

        // Assert
        Assert.IsType<DomainException>(exception);
    }

    [Fact]
    public void Atualizar_DadosValidos_AtualizaCamposComSucesso()
    {
        // Arrange
        var consulta = new Consulta(DateTime.UtcNow, "obs inicial", 1);
        var novaData = DateTime.UtcNow.AddDays(5);

        // Act
        consulta.Atualizar(novaData, "obs atualizada");

        // Assert
        Assert.Equal(novaData, consulta.DataConsulta);
        Assert.Equal("obs atualizada", consulta.Observacoes);
    }
}
