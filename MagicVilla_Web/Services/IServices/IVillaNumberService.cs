using MagicVilla_Web.Models.Dto;
using System.Linq.Expressions;

namespace MagicVilla_Web.Services.IServices
{
    public interface IVillaNumberService
    {
        Task<T> CreateAsync<T>(VillaNumberCreateDTO dto);
        Task<T> DeleteAsync<T>(int id);
        Task<T> UpdateAsync<T>(VillaNumberUpdateDTO dto);
        Task<T> GetAllAsync<T>();
        Task<T> GetAsync<T>(int id);
    }
}
