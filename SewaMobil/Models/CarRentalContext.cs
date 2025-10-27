using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.Entity;

namespace SewaMobil.Models
{
    public class CarRentalContext : DbContext
    {
        public CarRentalContext()
            : base("CarRentalContext")
        {
            // SQL CE tidak support lazy loading dengan baik
            Configuration.LazyLoadingEnabled = false;
            Configuration.ProxyCreationEnabled = false;
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Car> Cars { get; set; }
        public DbSet<Rental> Rentals { get; set; }
        public DbSet<RentalHistory> RentalHistories { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            // User Configuration
            modelBuilder.Entity<User>()
                .Property(u => u.Username)
                .IsRequired()
                .HasMaxLength(50);

            modelBuilder.Entity<User>()
                .Property(u => u.Email)
                .IsRequired()
                .HasMaxLength(100);

            // Car Configuration
            modelBuilder.Entity<Car>()
                .Property(c => c.LicensePlate)
                .IsRequired()
                .HasMaxLength(20);

            modelBuilder.Entity<Car>()
                .Property(c => c.DailyRate)
                .HasPrecision(18, 2);

            // Rental Configuration
            modelBuilder.Entity<Rental>()
                .HasRequired(r => r.User)
                .WithMany(u => u.Rentals)
                .HasForeignKey(r => r.UserId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Rental>()
                .HasRequired(r => r.Car)
                .WithMany(c => c.Rentals)
                .HasForeignKey(r => r.CarId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Rental>()
                .Property(r => r.TotalPrice)
                .HasPrecision(18, 2);

            // RentalHistory Configuration
            modelBuilder.Entity<RentalHistory>()
                .HasRequired(rh => rh.Rental)
                .WithMany(r => r.RentalHistories)
                .HasForeignKey(rh => rh.RentalId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<RentalHistory>()
                .HasOptional(rh => rh.ActionByUser)
                .WithMany()
                .HasForeignKey(rh => rh.ActionBy)
                .WillCascadeOnDelete(false);

            base.OnModelCreating(modelBuilder);
        }

        public static void SeedData(CarRentalContext context)
        {
            // Cek apakah sudah ada data
            if (context.Users.Any())
                return;

            // Tambah Admin Default
            var admin = new User
            {
                Username = "admin",
                Email = "admin@carrental.com",
                Password = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
                FullName = "Administrator",
                PhoneNumber = "081234567890",
                Role = "Admin",
                IsActive = true
            };

            var user = new User
            {
                Username = "user1",
                Email = "user1@example.com",
                Password = BCrypt.Net.BCrypt.HashPassword("User123!"),
                FullName = "John Doe",
                PhoneNumber = "081234567891",
                Role = "User",
                IsActive = true
            };

            context.Users.Add(admin);
            context.Users.Add(user);

            // Tambah Data Mobil
            var cars = new[]
            {
                new Car
                {
                    Brand = "Toyota",
                    Model = "Avanza",
                    Year = 2022,
                    LicensePlate = "B 1234 ABC",
                    Color = "Silver",
                    Transmission = "Manual",
                    FuelType = "Bensin",
                    Capacity = 7,
                    DailyRate = 300000,
                    Status = "Tersedia",
                    Description = "MPV nyaman untuk keluarga"
                },
                new Car
                {
                    Brand = "Honda",
                    Model = "Brio",
                    Year = 2023,
                    LicensePlate = "B 5678 DEF",
                    Color = "White",
                    Transmission = "Automatic",
                    FuelType = "Bensin",
                    Capacity = 5,
                    DailyRate = 250000,
                    Status = "Tersedia",
                    Description = "City car irit dan lincah"
                },
                new Car
                {
                    Brand = "Mitsubishi",
                    Model = "Xpander",
                    Year = 2022,
                    LicensePlate = "B 9012 GHI",
                    Color = "Black",
                    Transmission = "Automatic",
                    FuelType = "Bensin",
                    Capacity = 7,
                    DailyRate = 350000,
                    Status = "Tersedia",
                    Description = "MPV modern dan stylish"
                },
                new Car
                {
                    Brand = "Daihatsu",
                    Model = "Terios",
                    Year = 2021,
                    LicensePlate = "B 3456 JKL",
                    Color = "Red",
                    Transmission = "Manual",
                    FuelType = "Bensin",
                    Capacity = 7,
                    DailyRate = 320000,
                    Status = "Tersedia",
                    Description = "SUV tangguh untuk segala medan"
                },
                new Car
                {
                    Brand = "Toyota",
                    Model = "Innova",
                    Year = 2023,
                    LicensePlate = "B 7890 MNO",
                    Color = "Gray",
                    Transmission = "Automatic",
                    FuelType = "Diesel",
                    Capacity = 8,
                    DailyRate = 400000,
                    Status = "Tersedia",
                    Description = "MPV premium untuk perjalanan jauh"
                }
            };

            foreach (var car in cars)
            {
                context.Cars.Add(car);
            }

            context.SaveChanges();
        }
    }

    // Database Initializer untuk SQL CE
    public class CarRentalInitializer : DropCreateDatabaseIfModelChanges<CarRentalContext>
    {
        protected override void Seed(CarRentalContext context)
        {
            CarRentalContext.SeedData(context);
            base.Seed(context);
        }
    }
}