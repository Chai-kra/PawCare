using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

#nullable disable warnings

namespace Assignment.Models
{
    public class Database : DbContext
    {
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Pet> Pets { get; set; }
        public DbSet<Staff> Staff { get; set; }
    }

    public class Customer
    {
        [Key, Required,MaxLength(7)]
        public String Id { get; set; }
        [Required, MaxLength(100)]
        public String Name { get; set; }
        [Required, MaxLength(100)]
        public String Email { get; set; }
        [MaxLength(15)]
        public String Phone { get; set; }

        public List<Pet> Pets { get; set; } = [];
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
    }

    
}
