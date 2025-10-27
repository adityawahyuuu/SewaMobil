using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Web.Mvc;

namespace SewaMobil.Models
{
    // User Model
    public class User
    {
        public User()
        {
            Rentals = new List<Rental>();
            CreatedAt = DateTime.Now;
            IsActive = true;
            Role = "User";
        }

        [Key]
        public int UserId { get; set; }

        [Required(ErrorMessage = "Username harus diisi")]
        [StringLength(50)]
        [Display(Name = "Username")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Email harus diisi")]
        [StringLength(100)]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Password harus diisi")]
        [StringLength(255)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [Required(ErrorMessage = "Nama lengkap harus diisi")]
        [StringLength(100)]
        [Display(Name = "Nama Lengkap")]
        public string FullName { get; set; }

        [StringLength(20)]
        [Display(Name = "No. Telepon")]
        public string PhoneNumber { get; set; }

        [StringLength(255)]
        [Display(Name = "Alamat")]
        public string Address { get; set; }

        [Required]
        [StringLength(20)]
        public string Role { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsActive { get; set; }

        // Navigation Properties
        public virtual ICollection<Rental> Rentals { get; set; }
    }

    // Car Model
    public class Car
    {
        public Car()
        {
            Rentals = new List<Rental>();
            CreatedAt = DateTime.Now;
            IsActive = true;
            Status = "Tersedia";
        }

        [Key]
        public int CarId { get; set; }

        [Required(ErrorMessage = "Merk mobil harus diisi")]
        [StringLength(50)]
        [Display(Name = "Merk")]
        public string Brand { get; set; }

        [Required(ErrorMessage = "Model mobil harus diisi")]
        [StringLength(50)]
        [Display(Name = "Model")]
        public string Model { get; set; }

        [Required(ErrorMessage = "Tahun harus diisi")]
        [Display(Name = "Tahun")]
        [Range(1900, 2100, ErrorMessage = "Tahun tidak valid")]
        public int Year { get; set; }

        [Required(ErrorMessage = "Plat nomor harus diisi")]
        [StringLength(20)]
        [Display(Name = "Plat Nomor")]
        public string LicensePlate { get; set; }

        [StringLength(30)]
        [Display(Name = "Warna")]
        public string Color { get; set; }

        [StringLength(20)]
        [Display(Name = "Transmisi")]
        public string Transmission { get; set; }

        [StringLength(20)]
        [Display(Name = "Jenis Bahan Bakar")]
        public string FuelType { get; set; }

        [Display(Name = "Kapasitas Penumpang")]
        public int? Capacity { get; set; }

        [Required(ErrorMessage = "Harga sewa harus diisi")]
        [Display(Name = "Harga Sewa/Hari")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Harga harus lebih dari 0")]
        public decimal DailyRate { get; set; }

        [Required]
        [StringLength(20)]
        [Display(Name = "Status")]
        public string Status { get; set; }

        [StringLength(255)]
        [Display(Name = "URL Gambar")]
        public string ImageUrl { get; set; }

        [StringLength(500)]
        [Display(Name = "Deskripsi")]
        [DataType(DataType.MultilineText)]
        [AllowHtml]
        public string Description { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsActive { get; set; }

        // Navigation Properties
        public virtual ICollection<Rental> Rentals { get; set; }

        // Computed Property
        [NotMapped]
        [Display(Name = "Nama Mobil")]
        public string CarName
        {
            get { return string.Format("{0} {1} ({2})", Brand, Model, Year); }
        }
    }

    // Rental Model
    public class Rental
    {
        public Rental()
        {
            RentalHistories = new List<RentalHistory>();
            CreatedAt = DateTime.Now;
            Status = "Active";
        }

        [Key]
        public int RentalId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public int CarId { get; set; }

        [Required(ErrorMessage = "Tanggal sewa harus diisi")]
        [Display(Name = "Tanggal Sewa")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public DateTime RentalDate { get; set; }

        [Required(ErrorMessage = "Tanggal kembali harus diisi")]
        [Display(Name = "Tanggal Kembali")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public DateTime ReturnDate { get; set; }

        [Display(Name = "Tanggal Pengembalian Aktual")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public DateTime? ActualReturnDate { get; set; }

        [Required]
        [Display(Name = "Total Hari")]
        public int TotalDays { get; set; }

        [Required]
        [Display(Name = "Total Harga")]
        public decimal TotalPrice { get; set; }

        [Required]
        [StringLength(20)]
        [Display(Name = "Status")]
        public string Status { get; set; }

        [StringLength(500)]
        [Display(Name = "Catatan")]
        [DataType(DataType.MultilineText)]
        [AllowHtml]
        public string Notes { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Navigation Properties
        [ForeignKey("UserId")]
        public virtual User User { get; set; }

        [ForeignKey("CarId")]
        public virtual Car Car { get; set; }

        public virtual ICollection<RentalHistory> RentalHistories { get; set; }
    }

    // RentalHistory Model
    public class RentalHistory
    {
        public RentalHistory()
        {
            ActionDate = DateTime.Now;
        }

        [Key]
        public int HistoryId { get; set; }

        [Required]
        public int RentalId { get; set; }

        [Required]
        [StringLength(50)]
        public string Action { get; set; }

        public DateTime ActionDate { get; set; }

        public int? ActionBy { get; set; }

        [StringLength(500)]
        public string Notes { get; set; }

        // Navigation Properties
        [ForeignKey("RentalId")]
        public virtual Rental Rental { get; set; }

        [ForeignKey("ActionBy")]
        public virtual User ActionByUser { get; set; }
    }
}