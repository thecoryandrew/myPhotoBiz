using MyPhotoBiz.Models;

namespace MyPhotoBiz.ViewModels
{
    public static class ClientProfileMappingExtensions
    {
        public static ClientDetailsViewModel ToDetailsViewModel(this ClientProfile clientProfile)
        {
            return new ClientDetailsViewModel
            {
                Id = clientProfile.Id,
                FirstName = clientProfile.User?.FirstName ?? "",
                LastName = clientProfile.User?.LastName ?? "",
                Email = clientProfile.User?.Email ?? "",
                PhoneNumber = clientProfile.PhoneNumber,
                Address = clientProfile.Address,
                Notes = clientProfile.Notes,
                UpdatedDate = clientProfile.UpdatedDate,
                CreatedDate = clientProfile.CreatedDate,
                User = clientProfile.User,
                PhotoShootCount = clientProfile.PhotoShoots?.Count ?? 0,
                InvoiceCount = clientProfile.Invoices?.Count ?? 0,
                TotalRevenue = clientProfile.Invoices?.Sum(i => i.Amount + i.Tax) ?? 0m,
                PhotoShoots = clientProfile.PhotoShoots?.Select(ps => new PhotoShootViewModel
                {
                    Id = ps.Id,
                    Title = ps.Title,
                    ClientId = ps.ClientProfileId,
                    ScheduledDate = ps.ScheduledDate,
                    UpdatedDate = ps.UpdatedDate,
                    Location = ps.Location,
                    Status = ps.Status,
                    Price = ps.Price,
                    Notes = ps.Notes,
                    DurationHours = ps.DurationHours,
                    DurationMinutes = ps.DurationMinutes
                }).ToList() ?? new List<PhotoShootViewModel>(),
                Invoices = clientProfile.Invoices?.ToList() ?? new List<Invoice>(),
                ClientBadges = clientProfile.ClientBadges?.ToList() ?? new List<ClientBadge>()
            };
        }
    }
}
