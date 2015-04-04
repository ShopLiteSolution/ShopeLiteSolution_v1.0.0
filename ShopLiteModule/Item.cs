using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ShopLiteModule
{
    class Item : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public string SerialID;
        private string NameValue;
        private double PriceValue;
        private int QuantityValue;
        public double Weight;

        // This method is called by the Set accessor of each property. 
        // The CallerMemberName attribute that is applied to the optional propertyName 
        // parameter causes the property name of the caller to be substituted as an argument.
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public string Name {
            get{
                return this.NameValue;
            }
            set{
                if(value != this.NameValue){
                    this.NameValue = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public double Price
        {
            get
            {
                return this.PriceValue;
            }
            set
            {
                if (value != this.PriceValue)
                {
                    this.PriceValue = value;
                }
            }
        }

        public int Quantity
        {
            get
            {
                return this.QuantityValue;
            }
            set
            {
                if (value != this.QuantityValue)
                {
                    this.QuantityValue = value;
                }
            }
        }
    }
}
