using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;
using System.Threading.Tasks;
using PC_Parts_Scrapper.Data;
using PC_Parts_Scrapper.Models;
using Microsoft.EntityFrameworkCore;

namespace PC_Parts_Scrapper.Middleware
{
    public class Auth0UserSyncMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IServiceScopeFactory _scopeFactory;
.
        private static readonly string[] EmailClaimTypes =
        {
            ClaimTypes.Email,
            "email",
            "https://findpcparts.app/email"
        };

        private static readonly string[] NameClaimTypes =
        {
            "name",
            "nickname",
            ClaimTypes.Name,
            "https://findpcparts.app/name"
        };

        public Auth0UserSyncMiddleware(RequestDelegate next, IServiceScopeFactory scopeFactory)
        {
            _next = next;
            _scopeFactory = scopeFactory;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var auth0Id = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? context.User.FindFirst("sub")?.Value;

            if (context.User.Identity?.IsAuthenticated == true && !string.IsNullOrEmpty(auth0Id))
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<PcPartsContext>();

                var user = await db.Users.FirstOrDefaultAsync(u => u.Id == auth0Id);

                var email = FirstClaim(context.User, EmailClaimTypes);
                var displayName = FirstClaim(context.User, NameClaimTypes);

                if (user == null)
                {
                    user = new ApplicationUser
                    {
                        Id = auth0Id,                 // keep local PK == Auth0 "sub"
                        Auth0Id = auth0Id,
                        Email = email,
                        UserName = email ?? auth0Id,
                        FirstName = displayName,
                        CreatedAt = DateTime.UtcNow,
                        EmailConfirmed = true          // Auth0 handles verification
                    };
                    db.Users.Add(user);

                    try
                    {
                        await db.SaveChangesAsync();
                    }
                    catch (DbUpdateException)
                    {
                        // Row was created by a concurrent request between our read and write.
                    }
                }
                else if (user.Auth0Id != auth0Id || (email != null && user.Email != email))
                {
                    user.Auth0Id = auth0Id;
                    if (email != null) user.Email = email;
                    await db.SaveChangesAsync();
                }
            }

            await _next(context);
        }

        private static string? FirstClaim(ClaimsPrincipal principal, string[] claimTypes)
        {
            foreach (var type in claimTypes)
            {
                var value = principal.FindFirst(type)?.Value;
                if (!string.IsNullOrWhiteSpace(value))
                    return value;
            }
            return null;
        }
    }
}
