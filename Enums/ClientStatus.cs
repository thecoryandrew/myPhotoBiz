using System.ComponentModel.DataAnnotations;

namespace MyPhotoBiz.Enums
{
    public enum ClientStatus
    {
        [Display(Name = "Active")]
        Active = 0,
        [Display(Name = "Inactive")]
        Inactive = 1,
        [Display(Name = "Archived")]
        Archived = 2
    }
}
