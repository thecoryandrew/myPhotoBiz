using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using MyPhotoBiz.Enums;
using MyPhotoBiz.Models;
using MyPhotoBiz.Services;
using MyPhotoBiz.ViewModels;

namespace MyPhotoBiz.Controllers
{
    [Authorize]
    public class ContractTemplatesController : Controller
    {
        private readonly IContractTemplateService _templateService;
        private readonly ILogger<ContractTemplatesController> _logger;

        public ContractTemplatesController(
            IContractTemplateService templateService,
            ILogger<ContractTemplatesController> logger)
        {
            _templateService = templateService ?? throw new ArgumentNullException(nameof(templateService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // GET: ContractTemplates
        public async Task<IActionResult> Index(ContractTemplateCategory? category, bool? isActive)
        {
            try
            {
                IEnumerable<ContractTemplate> templates;

                if (category.HasValue)
                {
                    templates = await _templateService.GetTemplatesByCategoryAsync(category.Value);
                }
                else if (isActive.HasValue)
                {
                    templates = isActive.Value
                        ? await _templateService.GetActiveTemplatesAsync()
                        : (await _templateService.GetAllTemplatesAsync()).Where(t => !t.IsActive);
                }
                else
                {
                    templates = await _templateService.GetAllTemplatesAsync();
                }

                var viewModel = new ContractTemplateListViewModel
                {
                    Templates = templates,
                    FilterCategory = category,
                    FilterIsActive = isActive
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving contract templates");
                TempData["Error"] = "An error occurred while loading templates.";
                return View(new ContractTemplateListViewModel());
            }
        }

        // GET: ContractTemplates/Create
        public IActionResult Create()
        {
            var viewModel = new CreateContractTemplateViewModel
            {
                CategoryOptions = GetCategorySelectList()
            };
            return View(viewModel);
        }

        // POST: ContractTemplates/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateContractTemplateViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var template = new ContractTemplate
                    {
                        Name = model.Name,
                        Description = model.Description,
                        Content = model.Content,
                        Category = model.Category,
                        IsActive = model.IsActive,
                        CreatedBy = User.Identity?.Name
                    };

                    await _templateService.CreateTemplateAsync(template);
                    TempData["Success"] = "Template created successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating contract template");
                    ModelState.AddModelError("", "An error occurred while creating the template.");
                }
            }

            model.CategoryOptions = GetCategorySelectList();
            return View(model);
        }

        // GET: ContractTemplates/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var template = await _templateService.GetTemplateByIdAsync(id);
                if (template == null)
                {
                    TempData["Error"] = "Template not found.";
                    return RedirectToAction(nameof(Index));
                }

                var viewModel = new EditContractTemplateViewModel
                {
                    Id = template.Id,
                    Name = template.Name,
                    Description = template.Description,
                    Content = template.Content,
                    Category = template.Category,
                    IsActive = template.IsActive,
                    CategoryOptions = GetCategorySelectList()
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving template for edit: {TemplateId}", id);
                TempData["Error"] = "An error occurred while loading the template.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: ContractTemplates/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, EditContractTemplateViewModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var template = await _templateService.GetTemplateByIdAsync(id);
                    if (template == null)
                    {
                        TempData["Error"] = "Template not found.";
                        return RedirectToAction(nameof(Index));
                    }

                    template.Name = model.Name;
                    template.Description = model.Description;
                    template.Content = model.Content;
                    template.Category = model.Category;
                    template.IsActive = model.IsActive;
                    template.UpdatedBy = User.Identity?.Name;

                    await _templateService.UpdateTemplateAsync(template);
                    TempData["Success"] = "Template updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating contract template: {TemplateId}", id);
                    ModelState.AddModelError("", "An error occurred while updating the template.");
                }
            }

            model.CategoryOptions = GetCategorySelectList();
            return View(model);
        }

        // GET: ContractTemplates/Details/5
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var template = await _templateService.GetTemplateByIdAsync(id);
                if (template == null)
                {
                    TempData["Error"] = "Template not found.";
                    return RedirectToAction(nameof(Index));
                }

                var viewModel = new ContractTemplateDetailsViewModel
                {
                    Template = template
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving template details: {TemplateId}", id);
                TempData["Error"] = "An error occurred while loading the template.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: ContractTemplates/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var success = await _templateService.DeleteTemplateAsync(id);
                if (success)
                {
                    TempData["Success"] = "Template deleted successfully!";
                }
                else
                {
                    TempData["Error"] = "Template not found.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting contract template: {TemplateId}", id);
                TempData["Error"] = "An error occurred while deleting the template.";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: ContractTemplates/ToggleStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            try
            {
                var success = await _templateService.ToggleTemplateStatusAsync(id);
                if (success)
                {
                    TempData["Success"] = "Template status updated successfully!";
                }
                else
                {
                    TempData["Error"] = "Template not found.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling template status: {TemplateId}", id);
                TempData["Error"] = "An error occurred while updating the template status.";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: ContractTemplates/Duplicate/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Duplicate(int id, string newName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(newName))
                {
                    TempData["Error"] = "Please provide a name for the duplicated template.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                var duplicatedTemplate = await _templateService.DuplicateTemplateAsync(id, newName);
                TempData["Success"] = "Template duplicated successfully!";
                return RedirectToAction(nameof(Edit), new { id = duplicatedTemplate.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error duplicating contract template: {TemplateId}", id);
                TempData["Error"] = "An error occurred while duplicating the template.";
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        // Helper method to get category select list
        private List<SelectListItem> GetCategorySelectList()
        {
            return Enum.GetValues(typeof(ContractTemplateCategory))
                .Cast<ContractTemplateCategory>()
                .Select(c => new SelectListItem
                {
                    Value = ((int)c).ToString(),
                    Text = c.ToString()
                })
                .ToList();
        }
    }
}
