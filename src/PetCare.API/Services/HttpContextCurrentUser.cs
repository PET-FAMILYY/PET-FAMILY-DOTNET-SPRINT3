using System.Security.Claims;
using PetCare.Application.Auditing;

namespace PetCare.API.Services;

public class HttpContextCurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _accessor;

    public HttpContextCurrentUser(IHttpContextAccessor accessor) => _accessor = accessor;

    public string Nome
    {
        get
        {
            var user = _accessor.HttpContext?.User;
            return user?.Identity?.Name
                   ?? user?.FindFirstValue(ClaimTypes.Email)
                   ?? "anonimo";
        }
    }
}