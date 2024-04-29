using AutoMapper;
using MagicVilla_VillaAPI.Data;
using MagicVilla_VillaAPI.Models;
using MagicVilla_VillaAPI.Models.Dto;
using MagicVilla_VillaAPI.Repository.IRepository;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace MagicVilla_VillaAPI.Repository
{
    public class UserRepository : IUserRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IMapper _mapper;

        private string secretKey;

        public UserRepository(ApplicationDbContext context, IConfiguration configuration,
            UserManager<ApplicationUser> userManager, IMapper mapper, RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            secretKey = configuration.GetValue<string>("ApiSettings:Secret");
            _userManager = userManager;
            _mapper = mapper;
            _roleManager = roleManager;

        }

        public bool IsUniqueUser(string username)
        {
            var user = _context.ApplicationUsers.FirstOrDefault(x => x.UserName == username);
            if (user == null)
            {
                return true;
            }
            return false;
        }

        public async Task<TokenDTO> Login(LoginRequestDTO loginRequestDTO)
        {
            ApplicationUser user = await _context.ApplicationUsers.FirstOrDefaultAsync(x => x.UserName.ToLower() == loginRequestDTO.UserName.ToLower());

            bool isValid = await _userManager.CheckPasswordAsync(user, loginRequestDTO.Password);

            if (user == null || isValid == false)
            {
                return new TokenDTO()
                {
                    AccessToken = ""
                };
            }
            var jwtTokenId = $"JTI{Guid.NewGuid()}";
            var accessToken = await GetAccessToken(user, jwtTokenId);
            var refreshToken = await CreateNewRefreshToken(user.Id, jwtTokenId);
            TokenDTO tokenDto = new TokenDTO()
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken
            };

            return tokenDto;

        }

        public async Task<TokenDTO> RefreshAccessToken(TokenDTO tokenDTO)
        {
            var existingRefreshToken = await _context.RefreshTokens.FirstOrDefaultAsync(u => u.Refresh_Token == tokenDTO.RefreshToken);
            if (existingRefreshToken == null)
            {
                return new TokenDTO();
            }

            var isTokenValid = GetAccessTokenData(tokenDTO.AccessToken, existingRefreshToken.UserId, existingRefreshToken.JwtTokenId);
            if (!isTokenValid)
            {
                await MarkTokenAsInvalid(existingRefreshToken);
                return new TokenDTO();
            }
            if (!existingRefreshToken.IsValid)
            {
                await MarkAllTokenInChainAsInvalid(existingRefreshToken.UserId, existingRefreshToken.JwtTokenId);

                return new TokenDTO();
            }


            if (existingRefreshToken.ExpiresAt < DateTime.UtcNow)
            {
                await MarkTokenAsInvalid(existingRefreshToken);
                return new TokenDTO();
            }

            var newRefreshToken = await CreateNewRefreshToken(existingRefreshToken.UserId, existingRefreshToken.JwtTokenId);

            await MarkTokenAsInvalid(existingRefreshToken);

            var applicationUser = _context.ApplicationUsers.FirstOrDefaultAsync(u => u.Id == existingRefreshToken.UserId);
            if (applicationUser != null)
            {
                return new TokenDTO();
            }

            var newAccessToken = await GetAccessToken(applicationUser.Result, existingRefreshToken.JwtTokenId);

            return new TokenDTO()
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken
            };
        }

        public async Task<UserDTO> Register(RegistrationRequestDTO registrationRequestDTO)
        {
            ApplicationUser user = new ApplicationUser()
            {
                Name = registrationRequestDTO.Name,
                Email = registrationRequestDTO.UserName,
                NormalizedEmail = registrationRequestDTO.UserName.ToUpper(),
                UserName = registrationRequestDTO.UserName,
            };

            try
            {
                var result = await _userManager.CreateAsync(user, registrationRequestDTO.Password);
                if (result.Succeeded)
                {
                    if (!_roleManager.RoleExistsAsync(registrationRequestDTO.Role).GetAwaiter().GetResult())
                    {
                        await _roleManager.CreateAsync(new IdentityRole(registrationRequestDTO.Role));
                    }
                    await _userManager.AddToRoleAsync(user, registrationRequestDTO.Role);
                    var userToReturn = _context.ApplicationUsers.FirstOrDefault(x => x.UserName == registrationRequestDTO.UserName);
                    return _mapper.Map<UserDTO>(userToReturn);
                }
            }
            catch (Exception)
            {

                throw;
            }

            return new UserDTO();
        }


        public async Task RevokeRefreshToken(TokenDTO tokenDTO)
        {
            var existingRefreshToken = await _context.RefreshTokens.FirstOrDefaultAsync(x=>x.Refresh_Token == tokenDTO.RefreshToken);

            if (existingRefreshToken == null)
            {
                return;
            }

            var isTokenValid = GetAccessTokenData(tokenDTO.AccessToken, existingRefreshToken.UserId, existingRefreshToken.JwtTokenId);
            if (!isTokenValid)
            {
                return;
            }

            await MarkAllTokenInChainAsInvalid(existingRefreshToken.UserId, existingRefreshToken.JwtTokenId);
        }

        private async Task<string> CreateNewRefreshToken(string userId, string tokenId)
        {
            RefreshToken refreshToken = new()
            {
                IsValid = true,
                UserId = userId,
                JwtTokenId = tokenId,
                ExpiresAt = DateTime.UtcNow.AddDays(30),
                Refresh_Token = Guid.NewGuid() + "-" + Guid.NewGuid(),
            };

            await _context.RefreshTokens.AddAsync(refreshToken);
            await _context.SaveChangesAsync();
            return refreshToken.Refresh_Token;
        }

        private async Task<string> GetAccessToken(ApplicationUser user, string jwtTokenId)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(secretKey);
            var roles = await _userManager.GetRolesAsync(user);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimTypes.Name, user.UserName.ToString()),
                    new Claim(ClaimTypes.Role, roles.FirstOrDefault()),
                    new Claim(JwtRegisteredClaimNames.Jti, jwtTokenId),
                    new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                    new Claim(JwtRegisteredClaimNames.Aud, "dotnetmastery.com"),
                }),
                Expires = DateTime.UtcNow.AddMinutes(60),
                Issuer = "https://magicvilla-api.com",
                Audience = "https://test-magic-api.com",
                SigningCredentials = new(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenStr = tokenHandler.WriteToken(token);
            return tokenStr;

        }

        private bool GetAccessTokenData(string accessToken, string expectedUserId, string expectedTokenId)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var jwt = tokenHandler.ReadJwtToken(accessToken);
                var jwtTokenId = jwt.Claims.FirstOrDefault(u => u.Type == JwtRegisteredClaimNames.Jti).Value;
                var userId = jwt.Claims.FirstOrDefault(u => u.Type == JwtRegisteredClaimNames.Sub).Value;
                return userId == expectedUserId && jwtTokenId == expectedTokenId;
            }
            catch
            {

                return false;
            }
        }

        private async Task MarkAllTokenInChainAsInvalid(string userId, string tokenId)
        {

            await _context.RefreshTokens.Where(u => u.UserId == userId &&
                                u.JwtTokenId == tokenId)
                                .ExecuteUpdateAsync(u => u.SetProperty(refreshToken => refreshToken.IsValid, false));       
        }

        private Task MarkTokenAsInvalid(RefreshToken refreshToken)
        {
            refreshToken.IsValid = false;
           return _context.SaveChangesAsync();
        }

    }
}
