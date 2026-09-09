using PetCare.Domain.Entities;

namespace PetCare.Application.Interfaces;

public interface IJwtTokenGenerator
{
    (string Token, DateTime ExpiraEm) GerarToken(Usuario usuario);
}