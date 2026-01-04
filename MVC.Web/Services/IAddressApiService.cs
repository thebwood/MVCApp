using MVC.Web.Models;

namespace MVC.Web.Services
{
    public interface IAddressApiService
    {
        Task<Result<List<AddressDto>>> GetAllAddressesAsync();
        Task<Result<AddressDto>> GetAddressByIdAsync(Guid id);
        Task<Result<AddressDto>> CreateAddressAsync(CreateAddressDto createAddressDto);
        Task<Result<AddressDto>> UpdateAddressAsync(Guid id, UpdateAddressDto updateAddressDto);
        Task<Result> DeleteAddressAsync(Guid id);
    }
}
