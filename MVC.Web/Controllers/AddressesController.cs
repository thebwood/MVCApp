using Microsoft.AspNetCore.Mvc;
using MVC.Web.Models;
using System.Text;
using System.Text.Json;

namespace MVC.Web.Controllers
{
    public class AddressesController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly string _apiBaseUrl;

        public AddressesController(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _apiBaseUrl = _configuration["ApiSettings:BaseUrl"] ?? "https://localhost:7208";
        }

        // GET: Addresses
        public async Task<IActionResult> Index()
        {
            var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync($"{_apiBaseUrl}/api/addresses");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var addresses = JsonSerializer.Deserialize<List<AddressDto>>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return View(addresses ?? new List<AddressDto>());
            }

            TempData["Error"] = "Failed to load addresses";
            return View(new List<AddressDto>());
        }

        // GET: Addresses/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Addresses/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateAddressDto createAddressDto)
        {
            if (!ModelState.IsValid)
            {
                return View(createAddressDto);
            }

            var client = _httpClientFactory.CreateClient();
            var json = JsonSerializer.Serialize(createAddressDto);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync($"{_apiBaseUrl}/api/addresses", content);

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "Address created successfully";
                return RedirectToAction(nameof(Index));
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            ModelState.AddModelError("", $"Failed to create address: {errorContent}");
            return View(createAddressDto);
        }

        // GET: Addresses/Edit/5
        public async Task<IActionResult> Edit(Guid id)
        {
            var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync($"{_apiBaseUrl}/api/addresses/{id}");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var address = JsonSerializer.Deserialize<AddressDto>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (address != null)
                {
                    return View(address);
                }
            }

            TempData["Error"] = "Address not found";
            return RedirectToAction(nameof(Index));
        }

        // POST: Addresses/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, UpdateAddressDto updateAddressDto)
        {
            if (!ModelState.IsValid)
            {
                return View(updateAddressDto);
            }

            var client = _httpClientFactory.CreateClient();
            var json = JsonSerializer.Serialize(updateAddressDto);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PutAsync($"{_apiBaseUrl}/api/addresses/{id}", content);

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "Address updated successfully";
                return RedirectToAction(nameof(Index));
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            ModelState.AddModelError("", $"Failed to update address: {errorContent}");
            return View(updateAddressDto);
        }

        // GET: Addresses/Delete/5
        public async Task<IActionResult> Delete(Guid id)
        {
            var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync($"{_apiBaseUrl}/api/addresses/{id}");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var address = JsonSerializer.Deserialize<AddressDto>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (address != null)
                {
                    return View(address);
                }
            }

            TempData["Error"] = "Address not found";
            return RedirectToAction(nameof(Index));
        }

        // POST: Addresses/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var client = _httpClientFactory.CreateClient();
            var response = await client.DeleteAsync($"{_apiBaseUrl}/api/addresses/{id}");

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "Address deleted successfully";
                return RedirectToAction(nameof(Index));
            }

            TempData["Error"] = "Failed to delete address";
            return RedirectToAction(nameof(Index));
        }
    }
}
