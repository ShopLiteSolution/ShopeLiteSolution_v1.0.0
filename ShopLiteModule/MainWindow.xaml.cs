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
using System.Collections.ObjectModel;

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
        private ReaderConnection rCon;
        private ObservableCollection<Item> itemList;
        private double totalPrice;
        private double totalWeight;
        private int totalItem;
        private bool sessionStart;
        private double observedWeight;
        private MotorConnection mCon;

        public MainWindow()
        {
            InitializeComponent();

            _cancelEvent = new AutoResetEvent(false);
            itemList = new ObservableCollection<Item>();
            totalPrice = 0.0d;
            totalWeight = 0.0d;
            totalItem = 0;
            observedWeight = 0.0d;
            sessionStart = false;

            CancelBtn.IsEnabled = false;
            CheckoutBtn.IsEnabled = false;
            TimerStatusLbl.Content = "Welcome!";
            initImage();
            initDB();

            mCon = new MotorConnection(con);
        }
        private void OnAfterContentRendered(object sender, EventArgs e)
        {
            initReaderConnection();
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
        private void initReaderConnection()
        {
            rCon = new ReaderConnection();
            rCon.Added += new AddEventHandler(newTagDetected);
        }
        private void initBgWorker()
        {
            if (worker != null && worker.IsBusy)
            {
                worker.CancelAsync();
                _cancelEvent.WaitOne();
            }
            mCon.rotateMotor();
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
            //TODO
        }
        private void rescanBtnClicked(object sender, RoutedEventArgs e)
        {
            if (!sessionStart)
            {
                CustomDialog customDialog = new CustomDialog("Enter Weight", "Please enter the weight (Kg): ", "0.0", CustomDialog.DialogType.EnterWeight);
                customDialog.Owner = this;
                if (customDialog.ShowDialog() == true)
                {
                    sessionStart = true;
                    RescanBtn.Content = "Rescan";
                    observedWeight = Convert.ToDouble(customDialog.Answer);
                    initBgWorker();
                }

                if (rCon != null && rCon.isReading()) rCon.stopReader();
                if (mCon != null && mCon.isMotorRunning) mCon.stopMotor();

                itemList = new ObservableCollection<Item>();
                totalPrice = 0.0d;
                totalItem = 0;
                totalWeight = 0.0d;
                refreshList();
                rCon.existTags.Clear();
                CheckoutBtn.IsEnabled = false;

                initBgWorker();
                return;
            }
            else
            {
                CustomDialog dialog = new CustomDialog("Error",
                    "Please cancal the current scan before rescanning", "", CustomDialog.DialogType.Error);
                dialog.Owner = this;
                if (dialog.ShowDialog() == false) { }
            }
        }
        private void cancelBtnClicked(object sender, RoutedEventArgs e)
        {
            if (worker != null && worker.IsBusy)
            {
                //Console.Out.WriteLine("cancel button clicked"); 
                worker.CancelAsync();
                _cancelEvent.WaitOne();

                rCon.stopReader();
                sessionStart = false;
                mCon.stopMotor();
                CancelBtn.IsEnabled = false;
                TimerStatusLbl.Content = "Scanning cancelled!";
                Timer.Value = 0;
            }
        }

        private void AskforassistBtn_clicked(object sender, RoutedEventArgs e)
        {
            String display = "The assistant is coming. Thank you for you patience!";
            CustomDialog customDialog = new CustomDialog("Ask for assistant", display, "", CustomDialog.DialogType.AskForAssistance);
            customDialog.Owner = this;
            if (customDialog.ShowDialog() == false) { }
        }

        private void refreshList()
        {
            myList.ItemsSource = null;
            myList.ItemsSource = itemList;
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
                    if (i % 10 == 0)
                    {
                        rCon.RealTimeInventory();
                    }
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
                CancelBtn.IsEnabled = false;
                rCon.stopReader();
                mCon.stopMotor();
                enableCheckout();
                sessionStart = false;
            }
        }

        private void enableCheckout()
        {
            if (!checkWeight())
            {
                CustomDialog dialog = new CustomDialog("Error", 
                    "Observed weight and the total scanned items weight not match! You can choose to rescan or ask for assistance", "", CustomDialog.DialogType.Error);
                dialog.Owner = this;
                if (dialog.ShowDialog() == false) { }
                TimerStatusLbl.Content = "Please rescan!";
                return;
            }
            if (itemList.Count == 0)
            {
                CustomDialog dialog = new CustomDialog("Error", "Nothing is found after scanning.","",CustomDialog.DialogType.Error);
                dialog.Owner = this;
                if (dialog.ShowDialog() == false) { }
                TimerStatusLbl.Content = "Nothing is detected!";
                return;
            }
            
            CheckoutBtn.IsEnabled = true;
           
        }

        private bool checkWeight()
        {
            double errorRange = observedWeight * DefaultSettings.ErrorPercentage;
            
            if (observedWeight < 7.0d){
                errorRange = observedWeight * DefaultSettings.LightErrorP;
            }
            else
            {
                errorRange = observedWeight * DefaultSettings.HeavyErrorP;
            }
            if (errorRange <= DefaultSettings.minimunRange)
            {
                errorRange = DefaultSettings.minimunRange;
            }
            Console.Out.WriteLine("Calculated: " + totalWeight/1000d);
            Console.Out.WriteLine("Read: " + observedWeight);
            Console.Out.WriteLine("Error range: " + errorRange);

            if ((totalWeight/1000d) < (observedWeight - errorRange) || (totalWeight/1000d) > (observedWeight + errorRange))
            {
                return false;
            }
            return true;
        }
        private void _workerProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (e.ProgressPercentage < 100 && e.ProgressPercentage > 0 && !(sender as BackgroundWorker).CancellationPending)
            {
                TimerStatusLbl.Content = "Scanning...";
                CancelBtn.IsEnabled = true;
                RescanBtn.IsEnabled = true;
            }
            Timer.Value = e.ProgressPercentage;
        }

        private void newTagDetected(object sender, SetAddEventArgs e)
        {
            //Console.Out.WriteLine("UI gets event: " + e.newEntry as string);
            DataTable newItems = con.MyDataTable("SELECT * FROM Itemlist where SerialID = \"" + e.newEntry as string + "\"");
            Item newItem;
            foreach (DataRow row in newItems.Rows)
            {
                newItem = new Item();
                newItem.SerialID = (string)row.ItemArray[0];
                newItem.Name = (string)row.ItemArray[1];
                newItem.Price = (double)row.ItemArray[2];
                newItem.Quantity = Int32.Parse((string)row.ItemArray[3]);
                newItem.Weight = Double.Parse((string)row.ItemArray[4]);
                if (newItem.SerialID == "E2003000040F013725001C79")
                {
                    //detected cart
                    observedWeight -= (newItem.Weight / 1000d);
                    return;
                }
                totalPrice += newItem.Price;
                totalWeight += newItem.Weight;
                totalItem += 1;
                App.Current.Dispatcher.Invoke((Action)delegate
                {
                    itemList.Add(newItem);
                    TotalPriceLbl.Content = totalPrice.ToString("C2", CultureInfo.CurrentCulture);
                    itemCount.Content = totalItem.ToString();
                });
                //Thread.Sleep(200);
            }
        }
    }
}
