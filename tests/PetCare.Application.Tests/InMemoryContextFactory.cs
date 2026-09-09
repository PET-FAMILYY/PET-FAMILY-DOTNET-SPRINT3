using System;
using Microsoft.EntityFrameworkCore;
using PetCare.Infrastructure.Persistence;

namespace PetCare.Application.Tests;

/// <summary>
/// Fábrica de PetCareContext usando o provider EF Core InMemory,
/// garantindo isolamento total entre os testes (um banco por instância criada).
/// </summary>
public static class InMemoryContextFactory
{
    public static PetCareContext Create()
    {
        var options = new DbContextOptionsBuilder<PetCareContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new PetCareContext(options);
    }
}
