using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyPhotoBiz.Enums;
using MyPhotoBiz.Models;
using MyPhotoBiz.Services;
using MyPhotoBiz.ViewModels;

namespace MyPhotoBiz.Controllers
{
    [Authorize]
    public class ContractsController : Controller
    {
        private readonly IContractService _contractService;
        private readonly ILogger<ContractsController> _logger;

        public ContractsController(IContractService contractService, ILogger<ContractsController> logger)
        {
            _contractService = contractService;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var contracts = await _contractService.GetAllContractsAsync();
                return View(contracts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving contracts");
                TempData["Error"] = "An error occurred while loading contracts.";
                return View(new List<Contract>());
            }
        }

        public async Task<IActionResult> Create()
        {
            var viewModel = new CreateContractViewModel
            {
                AvailableClients = await _contractService.GetClientsAsync(),
                AvailablePhotoShoots = await _contractService.GetPhotoShootsAsync(),
                AvailableBadges = await _contractService.GetBadgesAsync()
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateContractViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var contract = await _contractService.CreateContractAsync(model);
                    TempData["Success"] = "Contract created successfully!";
                    return RedirectToAction(nameof(Details), new { id = contract.Id });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating contract");
                    ModelState.AddModelError("", "An error occurred while creating the contract.");
                }
            }

            model.AvailableClients = await _contractService.GetClientsAsync();
            model.AvailablePhotoShoots = await _contractService.GetPhotoShootsAsync();
            model.AvailableBadges = await _contractService.GetBadgesAsync();
            return View(model);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var contract = await _contractService.GetContractByIdAsync(id);
            if (contract == null)
                return NotFound();

            var viewModel = new EditContractViewModel
            {
                Id = contract.Id,
                Title = contract.Title,
                Content = contract.Content ?? "",
                ClientId = contract.ClientProfileId,
                PhotoShootId = contract.PhotoShootId,
                Status = contract.Status,
                CreatedDate = contract.CreatedDate,
                AvailableClients = await _contractService.GetClientsAsync(),
                AvailablePhotoShoots = await _contractService.GetPhotoShootsAsync()
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, EditContractViewModel model)
        {
            if (id != model.Id)
                return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    var contract = await _contractService.UpdateContractAsync(model);
                    TempData["Success"] = "Contract updated successfully!";
                    return RedirectToAction(nameof(Details), new { id = contract.Id });
                }
                catch (KeyNotFoundException)
                {
                    return NotFound();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating contract");
                    ModelState.AddModelError("", "An error occurred while updating the contract.");
                }
            }

            model.AvailableClients = await _contractService.GetClientsAsync();
            model.AvailablePhotoShoots = await _contractService.GetPhotoShootsAsync();
            return View(model);
        }

        public async Task<IActionResult> Details(int id)
        {
            var contract = await _contractService.GetContractByIdAsync(id);
            if (contract == null)
                return NotFound();

            var viewModel = new ContractDetailsViewModel
            {
                Id = contract.Id,
                Title = contract.Title,
                Content = contract.Content,
                CreatedDate = contract.CreatedDate,
                SignedDate = contract.SignedDate,
                SentDate = contract.SentDate,
                SignatureImagePath = contract.SignatureImagePath,
                Status = contract.Status,
                ClientId = contract.ClientProfileId,
                ClientName = contract.ClientProfile?.User != null
                    ? $"{contract.ClientProfile.User.FirstName} {contract.ClientProfile.User.LastName}"
                    : null,
                ClientEmail = contract.ClientProfile?.User?.Email,
                PhotoShootId = contract.PhotoShootId,
                PhotoShootTitle = contract.PhotoShoot?.Title
            };

            return View(viewModel);
        }

        public async Task<IActionResult> Sign(int id)
        {
            var contract = await _contractService.GetContractByIdAsync(id);
            if (contract == null)
                return NotFound();

            if (contract.Status == ContractStatus.Signed)
            {
                TempData["Error"] = "This contract has already been signed.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var viewModel = new SignContractViewModel
            {
                Id = contract.Id,
                Title = contract.Title,
                Content = contract.Content,
                ClientName = contract.ClientProfile?.User != null
                    ? $"{contract.ClientProfile.User.FirstName} {contract.ClientProfile.User.LastName}"
                    : null,
                CreatedDate = contract.CreatedDate
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Sign(int id, string signatureBase64)
        {
            try
            {
                var badgeName = await _contractService.SignContractAsync(id, signatureBase64);

                if (!string.IsNullOrEmpty(badgeName))
                {
                    TempData["Success"] = $"Contract signed successfully! Badge '{badgeName}' awarded!";
                }
                else
                {
                    TempData["Success"] = "Contract signed successfully!";
                }

                return RedirectToAction(nameof(Details), new { id });
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error signing contract");
                TempData["Error"] = "An error occurred while signing the contract.";
                return RedirectToAction(nameof(Sign), new { id });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var result = await _contractService.DeleteContractAsync(id);
                if (!result)
                    return NotFound();

                TempData["Success"] = "Contract deleted successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting contract");
                TempData["Error"] = "An error occurred while deleting the contract.";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}
