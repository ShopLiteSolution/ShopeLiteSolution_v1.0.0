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
using System.Data;

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
            initImage();
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

        private void initImage()
        {
            LogoImage.Source = new BitmapImage(new Uri(@"/resources/ShopLiteSolutionLogo.jpg", UriKind.Relative));
        }
        private void refreshList() {
            myList.ItemsSource = null;
            DataTable data = con.MyDataTable("SELECT * FROM Itemlist");
            myList.ItemsSource = data.DefaultView;
            TotalPriceLbl.Content = calculateTotalPrice(data);
            //Console.Out.WriteLine(data.Rows[0]["Price"]);
        }

        private string calculateTotalPrice(DataTable data) { 
            string output = "";
            double sum = 0.0d;
            foreach(DataRow row in data.Rows){
                sum += Double.Parse(System.Convert.ToString(row["Price"]));
            }
            output = System.Convert.ToString(sum);
            return output;
        }

    }
}
