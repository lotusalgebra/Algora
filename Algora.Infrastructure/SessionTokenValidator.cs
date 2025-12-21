using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Algora.Infrastructure
{
    public static class SessionTokenValidator
    {
        public static bool ValidateToken(string token, string shopifyAppSecret, out JwtSecurityToken? jwt)
        {
            jwt = null;
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(shopifyAppSecret);

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = false,              // tighten in prod
                ValidateAudience = false,            // validate audience if provided
                ValidateLifetime = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ClockSkew = TimeSpan.FromMinutes(2)
            };

            try
            {
                tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);
                jwt = validatedToken as JwtSecurityToken;
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
