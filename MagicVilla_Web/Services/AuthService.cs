using MagicVilla_Web.Models.Dto;
using MagicVilla_Web.Services.IServices;

namespace MagicVilla_Web.Services
{
    public class AuthService : IAuthService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private string villaUrl;
        private readonly IBaseService _baseService;
        public AuthService(IHttpClientFactory httpClient, IConfiguration configuration, IBaseService baseService)
        {
            _httpClientFactory = httpClient;
            villaUrl = configuration.GetValue<string>("ServiceUrls:https");
            _baseService = baseService;
        }

        public async Task<T> LoginAsync<T>(LoginRequestDTO obj)
        {
            return await _baseService.SendAsync<T>(new Models.APIRequest
            {
                ApiType = MagicVilla_Utility.Details.ApiType.POST,
                Data = obj,
                Url = villaUrl + "/api/Users/login"
            }, withBearer:false);
        }

        public async Task<T> RegisterAsync<T>(RegistrationRequestDTO obj)
        {
            return await _baseService.SendAsync<T>(new Models.APIRequest
            {
                ApiType = MagicVilla_Utility.Details.ApiType.POST,
                Data = obj,
                Url = villaUrl + "/api/Users/register"
            }, withBearer: false);
        }

        public async Task<T> LogoutAsync<T>(TokenDTO obj)
        {
            return await _baseService.SendAsync<T>(new Models.APIRequest
            {
                ApiType = MagicVilla_Utility.Details.ApiType.POST,
                Data = obj,
                Url = villaUrl + "/api/Users/revoke"
            });
        }
    }
}
