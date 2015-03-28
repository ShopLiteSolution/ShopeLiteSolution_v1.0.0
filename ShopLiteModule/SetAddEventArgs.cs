using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShopLiteModule
{
    public class SetAddEventArgs : EventArgs
    {
        public object newEntry {get; set;}

        public SetAddEventArgs(object nEntry)
        {
            this.newEntry = nEntry;
        }
    }
}
