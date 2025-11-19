using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace GoalSettingApp.Services
{
    public class SupabaseAuthenticationStateProvider : AuthenticationStateProvider
    {
        private readonly Supabase.Client _supabase;

        public SupabaseAuthenticationStateProvider(Supabase.Client supabase)

        {
            _supabase = supabase;
        }

        // Who's logged in
        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var user = _supabase.Auth.CurrentUser;

            if (user == null)
            {
                // No one logged in - return anonymous user
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
            }

            // Someone is logged in. Claims are pieces of info about the user (in this case email, name, ID)
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Email, user.Email ?? ""),
                new Claim(ClaimTypes.Name, user.UserMetadata?["display_name"]?.ToString() ?? user.Email ?? "")
            };

            // A collection of claims representing one identity
            var identity = new ClaimsIdentity(claims, "supabase");
            // The user (can have multiple identities)
            var principal = new ClaimsPrincipal(identity);

            return new AuthenticationState(principal);
        }

        // notify Blazor when a user logs in/out. Auth changed, refresh everything
        public void NotifyAuthenticationStateChanged()
        {
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        }
    }
}