using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

#nullable disable warnings

namespace PawCare.Models;

public enum AppointmentStatus
{
    PendingPayment = 0, //booked
    Confirmed = 1, //paid
    Completed = 2, //visit done
    Cancelled = 3,
    NoShow = 4
}

// ERD: Appointment (fk customerId, fk StaffId, fk PetId, DateTime, fk Service package)
public class Appointment
{
    [Key, MaxLength(7)]
    public string Id { get; set; }

    public string CustomerId { get; set; }
    [DeleteBehavior(DeleteBehavior.Restrict)] //prevent from deleted if it still related to child records
    public Customer Customers { get; set; }

    public string PetId { get; set; }
    [DeleteBehavior(DeleteBehavior.Restrict)]
    public Pet Pet { get; set; }

    public string StaffId { get; set; }
    [DeleteBehavior(DeleteBehavior.Restrict)]
    public Staff Staffs { get; set; }

    public string ServicePackageId { get; set; }
    [DeleteBehavior(DeleteBehavior.Restrict)]
    public ServicePackage ServicePackages { get; set; }

    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }

    [Column(TypeName = "decimal(8,2)")]
    public decimal Price { get; set; }        // copied from the package at booking time

    public AppointmentStatus Status { get; set; } = AppointmentStatus.PendingPayment;

    [MaxLength(300)]
    public string? Notes { get; set; }

    [MaxLength(300)]
    public string? CancelReason { get; set; }
    public DateTime? CancelledAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

