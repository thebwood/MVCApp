using MVC.Web.Models;
using Polly;
using Polly.Extensions.Http;
using System.Net;
using System.Text;
using System.Text.Json;

namespace MVC.Web.Services
{
    public class AddressApiService : IAddressApiService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<AddressApiService> _logger;
        private readonly string _apiBaseUrl;

        public AddressApiService(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<AddressApiService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _apiBaseUrl = configuration["ApiSettings:BaseUrl"] ?? "https://localhost:7208";
        }

        public async Task<Result<List<AddressDto>>> GetAllAddressesAsync()
        {
            try
            {
                _logger.LogInformation("Fetching all addresses from API");

                var client = _httpClientFactory.CreateClient("AddressApi");
                var response = await client.GetAsync($"{_apiBaseUrl}/api/addresses");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var addresses = JsonSerializer.Deserialize<List<AddressDto>>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    _logger.LogInformation("Successfully retrieved {Count} addresses", addresses?.Count ?? 0);
                    return Result.Success(addresses ?? new List<AddressDto>());
                }

                var error = await GetErrorMessageAsync(response);
                _logger.LogWarning("Failed to retrieve addresses. Status: {StatusCode}, Error: {Error}", 
                    response.StatusCode, error);
                return Result.Failure<List<AddressDto>>(error);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while fetching all addresses");
                return Result.Failure<List<AddressDto>>($"An error occurred: {ex.Message}");
            }
        }

        public async Task<Result<AddressDto>> GetAddressByIdAsync(Guid id)
        {
            try
            {
                _logger.LogInformation("Fetching address with ID: {AddressId}", id);

                var client = _httpClientFactory.CreateClient("AddressApi");
                var response = await client.GetAsync($"{_apiBaseUrl}/api/addresses/{id}");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var address = JsonSerializer.Deserialize<AddressDto>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    _logger.LogInformation("Successfully retrieved address with ID: {AddressId}", id);
                    return Result.Success(address!);
                }

                var error = await GetErrorMessageAsync(response);
                _logger.LogWarning("Failed to retrieve address {AddressId}. Status: {StatusCode}, Error: {Error}", 
                    id, response.StatusCode, error);
                return Result.Failure<AddressDto>(error);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while fetching address {AddressId}", id);
                return Result.Failure<AddressDto>($"An error occurred: {ex.Message}");
            }
        }

        public async Task<Result<AddressDto>> CreateAddressAsync(CreateAddressDto createAddressDto)
        {
            try
            {
                _logger.LogInformation("Creating new address");

                var client = _httpClientFactory.CreateClient("AddressApi");
                var json = JsonSerializer.Serialize(createAddressDto);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync($"{_apiBaseUrl}/api/addresses", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var address = JsonSerializer.Deserialize<AddressDto>(responseContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    _logger.LogInformation("Successfully created address with ID: {AddressId}", address?.Id);
                    return Result.Success(address!);
                }

                var error = await GetErrorMessageAsync(response);
                _logger.LogWarning("Failed to create address. Status: {StatusCode}, Error: {Error}", 
                    response.StatusCode, error);
                return Result.Failure<AddressDto>(error);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while creating address");
                return Result.Failure<AddressDto>($"An error occurred: {ex.Message}");
            }
        }

        public async Task<Result<AddressDto>> UpdateAddressAsync(Guid id, UpdateAddressDto updateAddressDto)
        {
            try
            {
                _logger.LogInformation("Updating address with ID: {AddressId}", id);

                var client = _httpClientFactory.CreateClient("AddressApi");
                var json = JsonSerializer.Serialize(updateAddressDto);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PutAsync($"{_apiBaseUrl}/api/addresses/{id}", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var address = JsonSerializer.Deserialize<AddressDto>(responseContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    _logger.LogInformation("Successfully updated address with ID: {AddressId}", id);
                    return Result.Success(address!);
                }

                var error = await GetErrorMessageAsync(response);
                _logger.LogWarning("Failed to update address {AddressId}. Status: {StatusCode}, Error: {Error}", 
                    id, response.StatusCode, error);
                return Result.Failure<AddressDto>(error);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while updating address {AddressId}", id);
                return Result.Failure<AddressDto>($"An error occurred: {ex.Message}");
            }
        }

        public async Task<Result> DeleteAddressAsync(Guid id)
        {
            try
            {
                _logger.LogInformation("Deleting address with ID: {AddressId}", id);

                var client = _httpClientFactory.CreateClient("AddressApi");
                var response = await client.DeleteAsync($"{_apiBaseUrl}/api/addresses/{id}");

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Successfully deleted address with ID: {AddressId}", id);
                    return Result.Success();
                }

                var error = await GetErrorMessageAsync(response);
                _logger.LogWarning("Failed to delete address {AddressId}. Status: {StatusCode}, Error: {Error}", 
                    id, response.StatusCode, error);
                return Result.Failure(error);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while deleting address {AddressId}", id);
                return Result.Failure($"An error occurred: {ex.Message}");
            }
        }

        private async Task<string> GetErrorMessageAsync(HttpResponseMessage response)
        {
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return "The requested address was not found.";
            }

            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                var content = await response.Content.ReadAsStringAsync();
                return string.IsNullOrEmpty(content) ? "Bad request." : content;
            }

            return $"Request failed with status code: {response.StatusCode}";
        }
    }
}
