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
using System.Data.SqlClient;

namespace UIMockup
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private List<Item> itemlist;

        public MainWindow()
        {
            InitializeComponent();
            Init();
            InitItemList();
            InitListView();
            //((App)Application.Current).getSQLConnection
        }

        public void Init() {
            itemlist = new List<Item>();
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Item apple = new Item() { Name = "alkex", Price = 0.49d, Unit = 1 };
            itemlist.Add(apple);
            refreshGrid();
        }

        public void InitItemList()
        {
            Item apple = new Item() { Name = "Apple", Price = 0.49d, Unit = 1};
            Item coke = new Item() { Name = "Coke", Price = 0.99d, Unit = 1 };
            Item waterbottle = new Item(){ Name = "Water Bottle", Price = 5.49d, Unit = 1};

            itemlist.Add(apple);
            itemlist.Add(coke);
            itemlist.Add(waterbottle);
        }

        public void InitListView()
        {

            refreshGrid();
        }

        private void refreshGrid()
        {
            myList.ItemsSource = null;
            myList.ItemsSource = itemlist;
        }
    }
}
