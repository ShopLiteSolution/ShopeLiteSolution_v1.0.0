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

namespace ShopLiteModule
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private DBConnection con;
        public MainWindow()
        {
            InitializeComponent();
            initDB();
            refreshList();
        }

        private void CheckoutBtn_clicked(object sender, RoutedEventArgs e)
        {
        }

        private void AskforassistBtn_clicked(object sender, RoutedEventArgs e)
        {
            refreshList();
        }

        private void initDB()
        {
            con = new DBConnection();   
        }

        private void refreshList() {
            myList.ItemsSource = null;
            myList.ItemsSource = con.MyDataTable("SELECT * FROM Itemlist").DefaultView;
        }

    }
    //hello world
}
