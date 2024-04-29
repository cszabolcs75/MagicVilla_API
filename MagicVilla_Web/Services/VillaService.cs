using MagicVilla_Utility;
using MagicVilla_Web.Models.Dto;
using MagicVilla_Web.Services.IServices;

namespace MagicVilla_Web.Services
{
    public class VillaService : IVillaService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private string villaUrl;
        private readonly IBaseService _baseService;
        public VillaService(IHttpClientFactory httpClient, IConfiguration configuration, IBaseService baseService)
        {
            _httpClientFactory = httpClient;
            villaUrl = configuration.GetValue<string>("ServiceUrls:https");
            _baseService = baseService;
        }

        public async Task<T> CreateAsync<T>(VillaCreateDTO dto)
        {
            return await _baseService.SendAsync<T>(new Models.APIRequest
            {
                ApiType = MagicVilla_Utility.Details.ApiType.POST,
                Data = dto,
                Url = villaUrl+"/api/villaAPI",
                ContentType = Details.ContentType.MultipartFormData
            });
        }

        public async Task<T> DeleteAsync<T>(int id)
        {
            return await _baseService.SendAsync<T>(new Models.APIRequest
            {
                ApiType = MagicVilla_Utility.Details.ApiType.DELETE,
                Url = villaUrl + "/api/villaAPI/"+id
            });
        }

        public async Task<T> GetAllAsync<T>()
        {
            return await _baseService.SendAsync<T>(new Models.APIRequest
            {
                ApiType = MagicVilla_Utility.Details.ApiType.GET,
                Url = villaUrl + "/api/villaAPI"
            });
        }

        public async Task<T> GetAsync<T>(int id)
        {
            return await _baseService.SendAsync<T>(new Models.APIRequest
            {
                ApiType = MagicVilla_Utility.Details.ApiType.GET,
                Url = villaUrl + "/api/villaAPI/"+id
            });
        }

        public async Task<T> UpdateAsync<T>(VillaUpdateDTO dto)
        {
            return await _baseService.SendAsync<T>(new Models.APIRequest
            {
                ApiType = MagicVilla_Utility.Details.ApiType.PUT,
                Data = dto,
                Url = villaUrl + "/api/villaAPI/"+ dto.Id,
                ContentType = Details.ContentType.MultipartFormData
            });
        }
    }
}
