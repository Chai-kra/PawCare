using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

#nullable disable warnings
namespace PawCare.Models;

public class BookingDateAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext context)
    {
        if (value is DateTime d)
        {
            if (d.Date < DateTime.Today)
                return new ValidationResult("Appointment date cannot be in the past.");
            if (d.Date > DateTime.Today.AddDays(BookingRules.MaxAdvanceDays))
                return new ValidationResult($"Appointments can only be booked up to {BookingRules.MaxAdvanceDays} days ahead.");
        }
        return ValidationResult.Success;   // [Required] handles null
    }


}

public class PackageOptionVM
{
    public string Id { get; set; }
    public string ServiceType { get; set; }
    public decimal Price { get; set; }
    public int TimeAmount { get; set; }
}

public class BookingVM
{
    // Buddy (pet) details. An existing pet of this customer with the same name is reused,
    // otherwise a new Pet row is created when the booking is saved.
    [Required(ErrorMessage = "Please enter your buddy's name.")]
    [MaxLength(100, ErrorMessage = "Name cannot exceed 100 characters.")]
    [Display(Name = "Your buddy's name")]
    public string? PetName { get; set; }

    [Required(ErrorMessage = "Please choose dog or cat.")]
    public string? PetType { get; set; }        // "Dog" or "Cat"

    [Required(ErrorMessage = "Please choose male or female.")]
    public string? PetGender { get; set; }      // "Male" or "Female"

    [Required(ErrorMessage = "Please enter the breed.")]
    [MaxLength(50, ErrorMessage = "Breed cannot exceed 50 characters.")]
    [Display(Name = "Breed")]
    public string? PetBreed { get; set; }

    [Required(ErrorMessage = "Please select a service package.")]
    [Display(Name = "Service package")]
    public string? ServicePackageId { get; set; }

    [Display(Name = "Preferred staff")]
    public string? StaffId { get; set; }   // null/empty = any available

    [Required(ErrorMessage = "Please select a date.")]
    [DataType(DataType.Date)]
    [BookingDate]
    public DateTime? Date { get; set; }

    [Required(ErrorMessage = "Please select a time slot.")]
    [RegularExpression(@"^([01]\d|2[0-3]):[0-5]\d$", ErrorMessage = "Invalid time slot.")]
    public string? SlotTime { get; set; }   // "HH:mm", set by booking.js

    [MaxLength(300, ErrorMessage = "Notes cannot exceed 300 characters.")]
    [Display(Name = "Notes for the groomer (optional)")]
    public string? Notes { get; set; }

    public List<SelectListItem> Staff { get; set; } = [];
    public List<PackageOptionVM> Packages { get; set; } = [];
}

public class AppointmentListVM // cancelling bookings, viewing upcoming/past appointments, etc.
{
    public string Id { get; set; }
    public DateTime StartTime { get; set; }
    public string PetName { get; set; }
    public string StaffName { get; set; }
    public string ServiceType { get; set; }
    public decimal Price { get; set; }
    public AppointmentStatus Status { get; set; }
    public bool CanCancel { get; set; }
}

public class AppointmentDetailVM
{
    public string Id { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string PetName { get; set; }
    public string StaffName { get; set; }
    public string ServiceType { get; set; }
    public int TimeAmount { get; set; }
    public decimal Price { get; set; }
    public AppointmentStatus Status { get; set; }
    public string? Notes { get; set; }
    public string? CancelReason { get; set; }
    public bool CanCancel { get; set; }
}

public class CancelVM
{
    public string Id { get; set; } = "";

    [Required(ErrorMessage = "Please tell us why you are cancelling.")]
    [StringLength(300, MinimumLength = 5, ErrorMessage = "Reason must be 5 to 300 characters.")]
    [Display(Name = "Reason for cancellation")]
    public string Reason { get; set; } = "";

    // display only (default value needed, or MVC treats it as a missing required value)
    public string PetName { get; set; } = "";
    public DateTime StartTime { get; set; }
}