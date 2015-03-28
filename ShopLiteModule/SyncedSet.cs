using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShopLiteModule
{
    public delegate void AddEventHandler(object sender, SetAddEventArgs e);
    public class SyncedSet<T> : HashSet<T>
    {
        public event AddEventHandler Added;

        protected virtual void OnAdded(SetAddEventArgs e)
        {
            if (Added != null)
            {
                Added(this, e);
            }
        }

        public bool AddSafe(T item)
        {
            bool result = base.Add(item);
            OnAdded(new SetAddEventArgs(item));
            return result;
        }
    }
}
