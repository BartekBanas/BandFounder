using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace BandFounder.Application.Services.Jwt;

public interface IJwtService
{
    string GenerateSymmetricJwtToken(ClaimsIdentity claimsIdentity);
}

public class JwtService : IJwtService
{
    private readonly JwtConfiguration _jwtConfiguration;
    
    public JwtService(IOptions<JwtConfiguration> jwtConfiguration)
    {
        _jwtConfiguration = jwtConfiguration.Value;
    }
    
    public string GenerateSymmetricJwtToken(ClaimsIdentity claimsIdentity)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_jwtConfiguration.SecretKey);
        
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = claimsIdentity,
            Expires = DateTime.UtcNow.AddMinutes(_jwtConfiguration.Expires),
            Issuer = _jwtConfiguration.Issuer,
            Audience = _jwtConfiguration.Audience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };
        
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}