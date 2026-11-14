using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

#nullable disable warnings

namespace PawCare.Models
{
    public class Database(DbContextOptions<Database> options) : DbContext(options)
    {
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Pet> Pets { get; set; }
        public DbSet<Staff> Staff { get; set; }

        // INSERT (appointments):   public DbSet<Appointment> Appointments { get; set; }
        // INSERT (loyalty points): public DbSet<LoyaltyTransaction> LoyaltyTransactions { get; set; }
    }

    [Index(nameof(Email), IsUnique = true)]
    public class Customer
    {
        [Key, Required, MaxLength(7)]
        public String Id { get; set; }
        // Entered as "Username" on the register form.
        [Required, MaxLength(100)]
        public String Name { get; set; }
        [Required, MaxLength(100)]
        public String Email { get; set; }
        [MaxLength(15)]
        public String Phone { get; set; }

        // Hash only; never store or log plain passwords.
        [Required, MaxLength(200)]
        public String PasswordHash { get; set; }

        // Email verification (the token is stored hashed).
        public bool EmailConfirmed { get; set; }
        [MaxLength(64)]
        public String VerifyTokenHash { get; set; }
        public DateTime? VerifyTokenExpires { get; set; }

        public List<Pet> Pets { get; set; } = [];

        // INSERT (loyalty points): public int LoyaltyPoints { get; set; }
        // INSERT (appointments):   public List<Appointment> Appointments { get; set; } = [];
    }

    public class Pet
    {
        [Key, MaxLength(7)]
        public String Id { get; set; }
        [Required, MaxLength(100)]
        public String Name { get; set; }
        [Required, MaxLength(10)]
        public String Type { get; set; }
        [Required, MaxLength(50)]
        public String Breed { get; set; }

        //FK
        public string CustomerId { get; set; }

        public Customer Customer { get; set; }
    }

    public class Staff
    {
        [Key, MaxLength(7)]
        public String Id { get; set; }
        [Required, MaxLength(100)]
        public String Name { get; set; }
        [Required, MaxLength(100)]
        public String Email { get; set; }
        [MaxLength(15)]
        public String Phone { get; set; }

        // INSERT (staff login): add PasswordHash like Customer, then sign in
    }
}
