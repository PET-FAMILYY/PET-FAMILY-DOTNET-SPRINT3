using System.Security.Cryptography;
using PetCare.Application.Interfaces;

namespace PetCare.Infrastructure.Security;

/// <summary>
/// Hash de senha usando PBKDF2 (Rfc2898DeriveBytes), sem dependências externas.
/// </summary>
public class PasswordHasher : IPasswordHasher
{
    private const int SaltSize = 16;
    private const int KeySize = 32;
    private const int Iterations = 100_000;
    private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA256;

    public string Hash(string senha)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var key = Rfc2898DeriveBytes.Pbkdf2(senha, salt, Iterations, Algorithm, KeySize);

        return string.Join('.',
            Convert.ToBase64String(salt),
            Convert.ToBase64String(key),
            Iterations);
    }

    public bool Verificar(string senha, string hash)
    {
        var partes = hash.Split('.');
        if (partes.Length != 3)
            return false;

        var salt = Convert.FromBase64String(partes[0]);
        var keyEsperada = Convert.FromBase64String(partes[1]);
        var iterations = int.Parse(partes[2]);

        var key = Rfc2898DeriveBytes.Pbkdf2(senha, salt, iterations, Algorithm, keyEsperada.Length);

        return CryptographicOperations.FixedTimeEquals(key, keyEsperada);
    }
}