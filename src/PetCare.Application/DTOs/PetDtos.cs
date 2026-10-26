using System;
using PetCare.Application.Common;

namespace PetCare.Application.DTOs;

public record PetRequest(string Nome, string Especie, string? Raca, DateTime DataNascimento, int TutorId);

public record PetResponse(int Id, string Nome, string Especie, string? Raca, DateTime DataNascimento, int TutorId) : ResourceBase;