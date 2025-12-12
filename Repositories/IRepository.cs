namespace _20241129402SoruCevapPortali.Repositories
{
    public interface IRepository<T> where T : class
    {
        List<T> GetAll();
        // ID parametresini 'int' yerine 'object' yaptık. Böylece hem int hem string kabul eder.
        T GetById(object id);
        void Add(T p);
        void Delete(T p);
        void Update(T p);
    }
}