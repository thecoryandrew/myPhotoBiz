using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using MyPhotoBiz.Enums;
using MyPhotoBiz.Models;

namespace MyPhotoBiz.ViewModels
{
    public class ContractTemplateListViewModel
    {
        public IEnumerable<ContractTemplate> Templates { get; set; } = new List<ContractTemplate>();
        public ContractTemplateCategory? FilterCategory { get; set; }
        public bool? FilterIsActive { get; set; }
    }

    public class CreateContractTemplateViewModel
    {
        [Required(ErrorMessage = "Template name is required")]
        [StringLength(200, ErrorMessage = "Template name cannot exceed 200 characters")]
        [Display(Name = "Template Name")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Content is required")]
        [Display(Name = "Template Content")]
        public string Content { get; set; } = string.Empty;

        [Display(Name = "Category")]
        public ContractTemplateCategory Category { get; set; } = ContractTemplateCategory.General;

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;

        public List<SelectListItem> CategoryOptions { get; set; } = new List<SelectListItem>();
    }

    public class EditContractTemplateViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Template name is required")]
        [StringLength(200, ErrorMessage = "Template name cannot exceed 200 characters")]
        [Display(Name = "Template Name")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Content is required")]
        [Display(Name = "Template Content")]
        public string Content { get; set; } = string.Empty;

        [Display(Name = "Category")]
        public ContractTemplateCategory Category { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; }

        public List<SelectListItem> CategoryOptions { get; set; } = new List<SelectListItem>();
    }

    public class ContractTemplateDetailsViewModel
    {
        public ContractTemplate Template { get; set; } = new ContractTemplate();
        public int UsageCount { get; set; }
        public DateTime? LastUsedDate { get; set; }
    }

    public class DuplicateContractTemplateViewModel
    {
        public int SourceTemplateId { get; set; }

        [Required(ErrorMessage = "New template name is required")]
        [StringLength(200, ErrorMessage = "Template name cannot exceed 200 characters")]
        [Display(Name = "New Template Name")]
        public string NewName { get; set; } = string.Empty;

        public string SourceTemplateName { get; set; } = string.Empty;
    }

    public class ContractTemplatePreviewViewModel
    {
        public ContractTemplate Template { get; set; } = new ContractTemplate();
        public string PopulatedContent { get; set; } = string.Empty;
        public ClientProfile? SampleClient { get; set; }
    }
}
