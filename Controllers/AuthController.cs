using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens; 
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt; 
using System.Security.Claims; 
using System.Text; 
using System.Threading.Tasks;
using TaskTitan.Api.Dtos; 
using TaskTitanData;      

namespace TaskTitan.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly TaskTitanDbContext _context;
        private readonly IConfiguration _configuration; 

        
        public AuthController(TaskTitanDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // POST: api
        [HttpPost("login")] // Route: /api/auth/login
        [ProducesResponseType(typeof(TokenDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<TokenDto>> Login(LoginDto loginDto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == loginDto.UserId);

            if (user == null)
            {
                return Unauthorized("Invalid User ID.");
            }

            try
            {
                var token = GenerateJwtToken(user);
                return Ok(new TokenDto { Token = token });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating JWT token for user ID {user.Id}: {ex}");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while generating the token.");
            }
        }

        private string GenerateJwtToken(TaskTitanData.Entities.User user)
        {
            var jwtSettings = _configuration.GetSection("Jwt");
            var secretKey = jwtSettings["Key"];
            var issuer = jwtSettings["Issuer"];
            var audience = jwtSettings["Audience"];

            
            if (string.IsNullOrEmpty(secretKey))
            {
                throw new InvalidOperationException("JWT Key not configured in appsettings.");
            }

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

           
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()), 
                new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
                new Claim(JwtRegisteredClaimNames.Name, user.Name ?? string.Empty), 
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()) 
                
            };

            var expireMinutes = _configuration.GetValue<int>("Jwt:ExpireMinutes", 60); 
            var expires = DateTime.UtcNow.AddMinutes(expireMinutes); // UTC 

            // Token
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = expires,
                Issuer = issuer,
                Audience = audience,
                SigningCredentials = credentials
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);

            return tokenHandler.WriteToken(token); // Token'ı string olarak döndür
        }
    }
}