using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMultiSync.Entitys;

namespace ViMultiSync.Repositories
{
    public class GenericRepository<T>
        where T : class, IEntity, new()
    {
        protected readonly List<T> _items = new List<T>();

        public void Add(T item)
        {
            _items.Add(item);
        }

        public void Save()
        {
            foreach (var item in _items)
            {
                Console.WriteLine(item);
            }
        }

        public T GetById(int id)
        {
            return _items.FirstOrDefault(item => item.Id == id);
        }

        public IEnumerable<T> GetAll()
        {
            return _items;
        }

        public void Remove(int id)
        {
            var itemToRemove = _items.FirstOrDefault(item => item.Id == id);
            if (itemToRemove != null)
            {
                _items.Remove(itemToRemove);
            }
        }
    }

}
