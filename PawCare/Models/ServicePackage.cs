using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

#nullable disable warnings

namespace PawCare.Models;

public class ServicePackage
{
    [Key, MaxLength(7)]
    public String Id { get; set; }
    [Required, MaxLength(100)]
    public String ServiceType { get; set; }
    [Column(TypeName = "decimal(8, 2)")]
    public decimal Price { get; set; }
    public int TimeAmount { get; set; } //minutes

}

