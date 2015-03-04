using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace UIMockup
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private List<Item> itemlist = new List<Item>();
        public MainWindow()
        {
            InitializeComponent();
            InitItemList();
            InitGridView();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Console.Out.WriteLine("Button clicked");
        }

        public void InitItemList()
        {
            Item apple = new Item() { Name = "apple", Price = 0.49d, Unit = 1 };
            Item coke = new Item() { Name = "coke", Price = 0.99d, Unit = 1 };
            Item waterbottle = new Item() { Name = "waterbottle", Price = 5.49d, Unit = 1 };

            itemlist.Add(apple);
            itemlist.Add(coke);
            itemlist.Add(waterbottle);
        }

        public void InitGridView()
        {
            myList.ItemsSource = itemlist;

        }
    }
}
