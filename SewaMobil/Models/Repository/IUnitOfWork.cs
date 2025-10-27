using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SewaMobil.Models.Repository
{
    public interface IUnitOfWork : IDisposable
    {
        IRepository<User> Users { get; }
        IRepository<Car> Cars { get; }
        IRepository<Rental> Rentals { get; }
        IRepository<RentalHistory> RentalHistories { get; }

        int SaveChanges();
        void BeginTransaction();
        void Commit();
        void Rollback();

        CarRentalContext Context { get; }
    }
}
