using PetCare.Application.DTOs;
using PetCare.Application.Interfaces;
using PetCare.Domain.Entities;
using PetCare.Domain.Exceptions;

namespace PetCare.Application.Services;

public class AuthService : IAuthService
{
    public const int TamanhoMinimoSenha = 6;

    private readonly IUsuarioRepository _usuarios;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public AuthService(
        IUsuarioRepository usuarios,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator)
    {
        _usuarios = usuarios;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<RegisterResponse> RegistrarAsync(RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Senha) || request.Senha.Length < TamanhoMinimoSenha)
            throw new DomainException($"A senha deve ter no mínimo {TamanhoMinimoSenha} caracteres.");

        if (await _usuarios.ExisteEmailAsync(request.Email))
            throw new ConflictException("Já existe um usuário cadastrado com este e-mail.");

        var senhaHash = _passwordHasher.Hash(request.Senha);
        var usuario = new Usuario(request.Nome, request.Email, senhaHash);

        await _usuarios.AdicionarAsync(usuario);

        return new RegisterResponse(usuario.Id, usuario.Nome, usuario.Email);
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        var usuario = await _usuarios.ObterPorEmailAsync(request.Email);

        // Mesma mensagem para e-mail inexistente e senha errada: não revela quais e-mails existem.
        if (usuario is null || !_passwordHasher.Verificar(request.Senha, usuario.SenhaHash))
            throw new AuthenticationFailedException("E-mail ou senha inválidos.");

        var (token, expiraEm) = _jwtTokenGenerator.GerarToken(usuario);

        return new LoginResponse(token, expiraEm, usuario.Nome, usuario.Email, usuario.Role);
    }
}