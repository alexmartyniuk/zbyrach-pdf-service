using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace Zbyrach.PdfService.Controllers
{
    public class AuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        private const string AUTH_TOKEN_PARAM_NAME = "AuthToken";
        private readonly string _authToken;

        public AuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISystemClock clock,
            IConfiguration configuration)
            : base(options, logger, encoder, clock)
        {
            _authToken = configuration["PDF_SERVICE_AUTH_TOKEN"];
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var endpoint = Context.GetEndpoint();
            if (endpoint?.Metadata?.GetMetadata<IAllowAnonymous>() != null)
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            var authToken = GetAuthToken();
            if (string.IsNullOrEmpty(authToken))
            {
                return Task.FromResult(AuthenticateResult.Fail("Missing Header with AuthToken"));
            }

            if (!_authToken.Equals(authToken))
            {
                return Task.FromResult(AuthenticateResult.Fail("Incorrect AuthToken"));
            }

            var claims = new[] {                
                new Claim(ClaimTypes.Authentication, authToken),                
            };
            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }

        private string GetAuthToken()
        {
            if (Request.Headers.TryGetValue(AUTH_TOKEN_PARAM_NAME, out var headerValue))
            {
                return headerValue.ToString();
            }

            if (Request.Query.TryGetValue(AUTH_TOKEN_PARAM_NAME, out var queryValue))
            {
                return queryValue.ToString();
            }

            return null;
        }        
    }
}
