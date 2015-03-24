using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UIMockup
{
    class Item
    {
        public string Name { get; set; }
        public double Price { get; set; }
        public int Unit { get; set; }
        public string toString() { 
            return "Item: " + Name  + " , Unit: " + Unit + " , Price: " + Price;
        }
    }
}
