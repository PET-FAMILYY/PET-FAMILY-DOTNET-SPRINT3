using Microsoft.AspNetCore.Mvc;
using PetCare.Application.DTOs;
using PetCare.Application.Services;

namespace PetCare.API.Controllers;

/// <summary>
/// Registro de usuários e emissão de token JWT. Endpoints públicos (não exigem token).
/// </summary>
[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService) => _authService = authService;

    /// <summary>Registra um novo usuário.</summary>
    /// <response code="201">Usuário criado.</response>
    /// <response code="400">Dados inválidos (ex.: senha curta ou e-mail malformado).</response>
    /// <response code="409">Já existe usuário com este e-mail.</response>
    [HttpPost("register")]
    [ProducesResponseType(typeof(RegisterResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RegisterResponse>> Register([FromBody] RegisterRequest request)
    {
        var usuario = await _authService.RegistrarAsync(request);
        return StatusCode(StatusCodes.Status201Created, usuario);
    }

    /// <summary>Autentica um usuário e retorna um token JWT.</summary>
    /// <response code="200">Token JWT e dados do usuário.</response>
    /// <response code="401">E-mail ou senha inválidos.</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
        => Ok(await _authService.LoginAsync(request));
}