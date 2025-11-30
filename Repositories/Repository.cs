using Microsoft.EntityFrameworkCore;
using _20241129402SoruCevapPortali.Models;
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

        public List<T> GetAll()
        {
            return _dbSet.ToList();
        }

        public T GetById(int id)
        {
            return _dbSet.Find(id);
        }

        public void Add(T entity)
        {
            _dbSet.Add(entity);
            _context.SaveChanges(); // EKSİK OLABİLİR
        }

        public void Update(T entity)
        {
            _dbSet.Update(entity);
            _context.SaveChanges(); // İŞTE ŞİFREYİ KAYDETMEYEN KISIM BURASIYDI
        }

        public void Delete(T entity)
        {
            _dbSet.Remove(entity);
            _context.SaveChanges(); // SİLME DE KAYDEDİLMELİ
        }

        public void Save()
        {
            _context.SaveChanges();
        }
    }
}