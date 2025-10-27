using System;
using System.Transactions;
using SewaMobil.Models;

namespace SewaMobil.Models.Repository
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly CarRentalContext _context;
        private TransactionScope _transaction;

        private IRepository<User> _users;
        private IRepository<Car> _cars;
        private IRepository<Rental> _rentals;
        private IRepository<RentalHistory> _rentalHistories;

        public CarRentalContext Context
        {
            get { return _context; }
        }

        public UnitOfWork(CarRentalContext context)
        {
            _context = context;
        }

        public IRepository<User> Users
        {
            get { return _users ?? (_users = new Repository<User>(_context)); }
        }

        public IRepository<Car> Cars
        {
            get { return _cars ?? (_cars = new Repository<Car>(_context)); }
        }

        public IRepository<Rental> Rentals
        {
            get { return _rentals ?? (_rentals = new Repository<Rental>(_context)); }
        }

        public IRepository<RentalHistory> RentalHistories
        {
            get { return _rentalHistories ?? (_rentalHistories = new Repository<RentalHistory>(_context)); }
        }

        public int SaveChanges()
        {
            try
            {
                return _context.SaveChanges();
            }
            catch (Exception ex)
            {
                var inner = ex;
                while (inner.InnerException != null)
                    inner = inner.InnerException;

                System.Diagnostics.Debug.WriteLine("SaveChanges Error: " + inner.Message);
                System.Diagnostics.Debug.WriteLine("Stack Trace: " + inner.StackTrace);
                throw;
            }
        }

        public void BeginTransaction()
        {
            _transaction = new TransactionScope(
                TransactionScopeOption.RequiresNew,
                new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted }
            );
        }


        public void Commit()
        {
            try
            {
                SaveChanges();
                if (_transaction != null)
                {
                    _transaction.Complete();
                }
            }
            catch
            {
                Rollback();
                throw;
            }
        }

        public void Rollback()
        {
            if (_transaction != null)
            {
                _transaction.Dispose();
                _transaction = null;
            }
        }

        public void Dispose()
        {
            if (_transaction != null)
            {
                _transaction.Dispose();
            }
            if (_context != null)
            {
                _context.Dispose();
            }
        }
    }
}