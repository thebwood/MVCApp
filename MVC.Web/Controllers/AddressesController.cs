using Microsoft.AspNetCore.Mvc;
using MVC.Web.Models;
using MVC.Web.Services;

namespace MVC.Web.Controllers
{
    public class AddressesController : Controller
    {
        private readonly IAddressApiService _addressApiService;
        private readonly ILogger<AddressesController> _logger;

        public AddressesController(IAddressApiService addressApiService, ILogger<AddressesController> logger)
        {
            _addressApiService = addressApiService;
            _logger = logger;
        }

        // GET: Addresses
        public async Task<IActionResult> Index()
        {
            var result = await _addressApiService.GetAllAddressesAsync();

            if (result.IsFailure)
            {
                _logger.LogWarning("Failed to load addresses: {Error}", result.Error);
                TempData["Error"] = result.Error;
                return View(new List<AddressDto>());
            }

            return View(result.Value);
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

            var result = await _addressApiService.CreateAddressAsync(createAddressDto);

            if (result.IsFailure)
            {
                _logger.LogWarning("Failed to create address: {Error}", result.Error);
                ModelState.AddModelError("", result.Error);
                return View(createAddressDto);
            }

            TempData["Success"] = "Address created successfully";
            return RedirectToAction(nameof(Index));
        }

        // GET: Addresses/Edit/5
        public async Task<IActionResult> Edit(Guid id)
        {
            var result = await _addressApiService.GetAddressByIdAsync(id);

            if (result.IsFailure)
            {
                _logger.LogWarning("Failed to load address {AddressId}: {Error}", id, result.Error);
                TempData["Error"] = result.Error;
                return RedirectToAction(nameof(Index));
            }

            return View(result.Value);
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

            var result = await _addressApiService.UpdateAddressAsync(id, updateAddressDto);

            if (result.IsFailure)
            {
                _logger.LogWarning("Failed to update address {AddressId}: {Error}", id, result.Error);
                ModelState.AddModelError("", result.Error);
                return View(updateAddressDto);
            }

            TempData["Success"] = "Address updated successfully";
            return RedirectToAction(nameof(Index));
        }

        // GET: Addresses/Delete/5
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _addressApiService.GetAddressByIdAsync(id);

            if (result.IsFailure)
            {
                _logger.LogWarning("Failed to load address {AddressId} for deletion: {Error}", id, result.Error);
                TempData["Error"] = result.Error;
                return RedirectToAction(nameof(Index));
            }

            return View(result.Value);
        }

        // POST: Addresses/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var result = await _addressApiService.DeleteAddressAsync(id);

            if (result.IsFailure)
            {
                _logger.LogWarning("Failed to delete address {AddressId}: {Error}", id, result.Error);
                TempData["Error"] = result.Error;
                return RedirectToAction(nameof(Index));
            }

            TempData["Success"] = "Address deleted successfully";
            return RedirectToAction(nameof(Index));
        }
    }
}
