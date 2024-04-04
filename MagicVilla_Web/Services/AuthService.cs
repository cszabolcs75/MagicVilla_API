using MagicVilla_Web.Models.Dto;
using MagicVilla_Web.Services.IServices;

namespace MagicVilla_Web.Services
{
    public class AuthService : BaseService, IAuthService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private string villaUrl;

        public AuthService(IHttpClientFactory httpClient, IConfiguration configuration) : base(httpClient)
        {
            _httpClientFactory = httpClient;
            villaUrl = configuration.GetValue<string>("ServiceUrls:https");
        }

        public Task<T> LoginAsync<T>(LoginRequestDTO obj)
        {
            return SendAsync<T>(new Models.APIRequest
            {
                ApiType = MagicVilla_Utility.Details.ApiType.POST,
                Data = obj,
                Url = villaUrl + "/api/Users/login"
            });
        }

        public Task<T> RegisterAsync<T>(RegistrationRequestDTO obj)
        {
            return SendAsync<T>(new Models.APIRequest
            {
                ApiType = MagicVilla_Utility.Details.ApiType.POST,
                Data = obj,
                Url = villaUrl + "/api/Users/register"
            });
        }
    }
}
