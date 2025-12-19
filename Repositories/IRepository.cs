namespace _20241129402SoruCevapPortali.Repositories
{
    public interface IRepository<T> where T : class
    {
        List<T> GetAll();
        T GetById(object id);
        void Add(T p);
        void Delete(T p);
        void Update(T p);
    }
}