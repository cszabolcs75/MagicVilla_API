using MagicVilla_Web.Models.Dto;
using MagicVilla_Web.Services.IServices;

namespace MagicVilla_Web.Services
{
    public class VillaNumberService : IVillaNumberService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private string villaUrl;
        private readonly IBaseService _baseService;
        public VillaNumberService(IHttpClientFactory httpClient, IConfiguration configuration, IBaseService baseService)
        {
            _httpClientFactory = httpClient;
            villaUrl = configuration.GetValue<string>("ServiceUrls:https");
            _baseService = baseService;
        }

        public async Task<T> CreateAsync<T>(VillaNumberCreateDTO dto)
        {
            return await _baseService.SendAsync<T>(new Models.APIRequest
            {
                ApiType = MagicVilla_Utility.Details.ApiType.POST,
                Data = dto,
                Url = villaUrl+ "/api/VillaNumberAPI"
            });
        }

        public async Task<T> DeleteAsync<T>(int id)
        {
            return await _baseService.SendAsync<T>(new Models.APIRequest
            {
                ApiType = MagicVilla_Utility.Details.ApiType.DELETE,
                Url = villaUrl + "/api/VillaNumberAPI/" + id
            });
        }

        public async Task<T> GetAllAsync<T>()
        {
            return await _baseService.SendAsync<T>(new Models.APIRequest
            {
                ApiType = MagicVilla_Utility.Details.ApiType.GET,
                Url = villaUrl + "/api/VillaNumberAPI"
            });
        }

        public async Task<T> GetAsync<T>(int id)
        {
            return await _baseService.SendAsync<T>(new Models.APIRequest
            {
                ApiType = MagicVilla_Utility.Details.ApiType.GET,
                Url = villaUrl + "/api/VillaNumberAPI/" + id
            });
        }

        public async Task<T> UpdateAsync<T>(VillaNumberUpdateDTO dto)
        {
            return await _baseService.SendAsync<T>(new Models.APIRequest
            {
                ApiType = MagicVilla_Utility.Details.ApiType.PUT,
                Data = dto,
                Url = villaUrl + "/api/VillaNumberAPI/" + dto.VillaNo
            });
        }
    }
}
