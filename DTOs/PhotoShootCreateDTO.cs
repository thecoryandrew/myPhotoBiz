using MyPhotoBiz.Models;

public class PhotoShootCreateDto
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public DateTime ScheduledDate { get; set; }
    public int DurationHours { get; set; }
    public int DurationMinutes { get; set; }
    public string? Location { get; set; }
    public decimal Price { get; set; }
    public string? Notes { get; set; }
    public PhotoShootStatus Status { get; set; }
    public int ClientId { get; set; }
    public string? PhotographerId { get; set; }
    public List<int>? SelectedServiceIds { get; set; }



}

public class PhotoShootUpdateDto : PhotoShootCreateDto
{
    public int Id { get; set; }
}
