namespace Vantage.PMS.Models.Booking;

public class BookingAddOn
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public decimal Price { get; set; }

    public bool IsPerNight { get; set; }

    public bool IsPerPerson { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<BookingRequestAddOn> BookingRequestAddOns { get; set; } = new List<BookingRequestAddOn>();
}
