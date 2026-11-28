using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

#nullable disable

namespace PawCare.Models
{
    public class Database(DbContextOptions<Database> options) : DbContext(options)
    {
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Pet> Pets { get; set; }
        public DbSet<Staff> Staff { get; set; }

        //Booking module
        public DbSet<Service> Services { get; set; }
        public DbSet<Appointment> Appointments { get; set; }
        public DbSet<ServicePackage> ServicePackages { get; set; }

        //todo later
        // INSERT (loyalty points):  public DbSet<LoyaltyTransaction> LoyaltyTransactions { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Product> Products { get; set; }
    }

    [Index(nameof(Email), IsUnique = true)]
    public class Customer
    {
        [MaxLength(7)]
        public String Id { get; set; }
        // Entered as "Username" on the register form.
        [Required, MaxLength(100)]
        public String Name { get; set; }
        [Required, MaxLength(100)]
        public String Email { get; set; }
        [MaxLength(15)]
        public String Phone { get; set; }

        // Profile photo: GUID file name inside wwwroot/photos
        [MaxLength(100)]
        public String PhotoURL { get; set; }

        // Hash only; never store or log plain passwords.
        [MaxLength(200)]
        public String PasswordHash { get; set; }

        // Email verification (the token is stored hashed).
        public bool EmailConfirmed { get; set; }
        [MaxLength(64)]
        public String VerifyTokenHash { get; set; }
        public DateTime? VerifyTokenExpires { get; set; }

        // Password reset (separate from verification so the two never collide; token stored hashed).
        [MaxLength(64)]
        public String ResetTokenHash { get; set; }
        public DateTime? ResetTokenExpires { get; set; }

        public List<Pet> Pets { get; set; } = [];

        // INSERT (loyalty points): public int LoyaltyPoints { get; set; }
    }

    public class Pet
    {
        [Key, MaxLength(7)]
        public String Id { get; set; }
        [Required, MaxLength(100)]
        public String Name { get; set; }
        [Required, MaxLength(10)]
        public String Type { get; set; }
        [Required, MaxLength(10)]
        public String Gender { get; set; }
        [Required, MaxLength(50)]
        public String Breed { get; set; }

        //FK
        [Required, MaxLength(7)]
        public string CustomerId { get; set; }

        public Customer Customer { get; set; }
    }

    // Login looks staff up by email, so it must be unique.
    // IDs must start with IdPrefix.Staff ('S'), e.g. S000001.
    [Index(nameof(Email), IsUnique = true)]
    public class Staff
    {
        [MaxLength(7)]
        public String Id { get; set; }
        [Required, MaxLength(100)]
        public String Name { get; set; }
        [Required, MaxLength(100)]
        public String Email { get; set; }
        [MaxLength(15)]
        public String Phone { get; set; }
        // "Staff" or "Admin"
        [Required, MaxLength(10)]
        public String Role { get; set; }

        // Null until the staff member sets a password via "Forgot password".
        [MaxLength(200)]
        public String PasswordHash { get; set; }

        [MaxLength(64)]
        public String ResetTokenHash { get; set; }
        public DateTime? ResetTokenExpires { get; set; }
    }

    public class Category
    {
        [MaxLength(4)]
        public String Id { get; set; }
        [Required, MaxLength(100)]
        public String Name { get; set; }
        public List<Product> Products { get; set; } = [];
    }

    public class Service
    {
        [MaxLength(7)]
        public String Id { get; set; }
        [Required, MaxLength(100)]
        public String Name { get; set; }
        [MaxLength(200)]
        public String Description { get; set; }
        public decimal Price { get; set; }
        public int Duration { get; set; }
        [Required, MaxLength(4)]
        public string CategoryId { get; set; }
        public Category Category { get; set; }
    }

    public class Product
    {
        [Key, MaxLength(7)]
        public String Id { get; set; }
        [Required, MaxLength(100)]
        public String Name { get; set; }
        [MaxLength(200)]
        public String Description { get; set; }
        public decimal Price { get; set; }
        public int Stock { get; set; }
        [Required, MaxLength(4)]
        public string CategoryId { get; set; }
        public Category Category { get; set; }
    }
}