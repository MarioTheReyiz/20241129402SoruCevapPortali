using _20241129402SoruCevapPortali.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;

namespace _20241129402SoruCevapPortali.Repositories
{
    public class Repository<T> : IRepository<T> where T : class
    {
        private readonly AppDbContext _context;
        private readonly DbSet<T> _dbSet;

        public Repository(AppDbContext context)
        {
            _context = context;
            _dbSet = _context.Set<T>();
        }

        public void Add(T p)
        {
            _dbSet.Add(p);
            _context.SaveChanges();
        }

        public void Delete(T p)
        {
            _dbSet.Remove(p);
            _context.SaveChanges();
        }

        public List<T> GetAll()
        {
            return _dbSet.ToList();
        }
        public T GetById(object id)
        {
            return _dbSet.Find(id);
        }

        public void Update(T p)
        {
            _dbSet.Update(p);
            _context.SaveChanges();
        }
    }
}