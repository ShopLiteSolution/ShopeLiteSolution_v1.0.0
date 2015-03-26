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
using System.Threading;
using System.ComponentModel;
using System.Globalization;

namespace ShopLiteModule
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private DBConnection con;
        private BackgroundWorker worker;
        private AutoResetEvent _cancelEvent;

        public MainWindow()
        {
            _cancelEvent = new AutoResetEvent(false);
            InitializeComponent();
            initImage();
            initDB();
            initBgWorker();
        }

        private void initDB()
        {
            con = new DBConnection();
            refreshList();
        }

        private void initImage()
        {
            LogoImage.Source = new BitmapImage(new Uri(@"/resources/ShopLiteSolutionLogo.jpg", UriKind.Relative));
        }

        private void initBgWorker()
        {
            if (worker != null && worker.IsBusy)
            {
                worker.CancelAsync();
                _cancelEvent.WaitOne();
            }

            worker = new BackgroundWorker();

            worker.WorkerReportsProgress = true;
            worker.WorkerSupportsCancellation = true;
            worker.DoWork += _workerDoWork;
            worker.ProgressChanged += _workerProgressChanged;
            worker.RunWorkerCompleted += _workerJobComplete;

            worker.RunWorkerAsync();
        }

        private void checkoutBtnClicked(object sender, RoutedEventArgs e)
        {
        }

        private void rescanBtnClicked(object sender, RoutedEventArgs e)
        {
            initBgWorker();
        }

        private void cancelBtnClicked(object sender, RoutedEventArgs e)
        {
            if (worker != null && worker.IsBusy)
            {
                Console.Out.WriteLine("cancel button clicked"); 
                worker.CancelAsync();
                _cancelEvent.WaitOne();
                Console.Out.WriteLine("cancel event returned to main thread");
                cancelBtn.Visibility = Visibility.Hidden;
                TimerStatusLbl.Content = "Scanning  !";
                Timer.Value = 0;
            }
        }

        private void AskforassistBtn_clicked(object sender, RoutedEventArgs e)
        {
            refreshList();
        }

        private void refreshList() {
            myList.ItemsSource = null;
            DataTable data = con.MyDataTable("SELECT * FROM Itemlist");
            myList.ItemsSource = data.DefaultView;
            TotalPriceLbl.Content = calculateTotalPrice(data);
            //Console.Out.WriteLine(data.Rows[0]["Price"]);
        }

        private void refreshList(DataTable data)
        {
            myList.ItemsSource = null;
            myList.ItemsSource = data.DefaultView;
            TotalPriceLbl.Content = calculateTotalPrice(data);
        }

        private string calculateTotalPrice(DataTable data) { 
            string output = "";
            double sum = 0.0d;
            foreach(DataRow row in data.Rows){
                sum += Double.Parse(System.Convert.ToString(row["Price"]));
            }
            output = sum.ToString("C2", CultureInfo.CurrentCulture);
            return output;
        }

        private void _workerDoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            for (int i = 0; i < 100; i++)
            {
                if (worker.CancellationPending == true)
                {
                    e.Cancel = true;
                    _cancelEvent.Set();
                    break;
                }
                else
                {
                    // TODO:
                    // use Reader.dll to scan tags
                    // check db with serialIDs
                    // refresh the list: refreshList(DataTable data)
                    worker.ReportProgress(i);
                    Thread.Sleep(100);
                }
            }

            worker = null;
        }
        private void _workerJobComplete(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled == true)
            {

            }
            else if (!(e.Error == null))
            {
                //TODO
            }
            else
            {
                TimerStatusLbl.Content = "Finished scanning.";
                cancelBtn.Visibility = Visibility.Hidden;
            }
        }

        private void _workerProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (e.ProgressPercentage < 100 && e.ProgressPercentage > 0 && !(sender as BackgroundWorker).CancellationPending)
            {
                TimerStatusLbl.Content = "Scanning...";
                cancelBtn.Visibility = Visibility.Visible;
            }
            Timer.Value = e.ProgressPercentage;

            if ((sender as BackgroundWorker).CancellationPending)
            {
                //TODO: clean the list view
            }
            

        }
    }
}
