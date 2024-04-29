using MagicVilla_Utility;
using MagicVilla_Web.Models.Dto;
using MagicVilla_Web.Services.IServices;

namespace MagicVilla_Web.Services
{
    public class TokenProvider : ITokenProvider
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public TokenProvider(IHttpContextAccessor httpContextAccessor)
        {
            this._httpContextAccessor = httpContextAccessor;
        }

        public void ClearToken()
        {
            _httpContextAccessor.HttpContext?.Response.Cookies.Delete(Details.AccessToken);
            _httpContextAccessor.HttpContext?.Response.Cookies.Delete(Details.RefreshToken);
        }

        public TokenDTO GetToken()
        {
            try
            {
                bool hasAccessToken = _httpContextAccessor.HttpContext.Request.Cookies.TryGetValue(Details.AccessToken, out string accessToken);
                bool hasRefreshToken = _httpContextAccessor.HttpContext.Request.Cookies.TryGetValue(Details.RefreshToken, out string refreshToken);
                TokenDTO tokenDTO = new TokenDTO()
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshToken
                };
                return hasAccessToken ? tokenDTO : null;
            }
            catch (Exception)
            {

                return null;
            }
        }

        public void SetToken(TokenDTO tokenDTO)
        {
            var cookieOptions = new CookieOptions { Expires = DateTime.UtcNow.AddDays(60) };
            _httpContextAccessor.HttpContext?.Response.Cookies.Append(Details.AccessToken, tokenDTO.AccessToken, cookieOptions);
            _httpContextAccessor.HttpContext?.Response.Cookies.Append(Details.RefreshToken, tokenDTO.RefreshToken, cookieOptions);
        }
    }
}
