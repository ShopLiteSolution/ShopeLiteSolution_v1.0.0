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
using System.Windows.Shapes;

namespace ShopLiteModule
{
    /// <summary>
    /// Interaction logic for CustomDialog.xaml
    /// </summary>
    public partial class CustomDialog : Window
    {
        public enum DialogType
        {
            AskForAssistance, EnterWeight, Error
        }

        private DialogType type;

        public CustomDialog(string title, string question, string defaultAnswer, DialogType type){
            InitializeComponent();
            this.Title = title;
            QuestionLabel.Content = question;
            TextAnswer.Text = defaultAnswer;

            this.type = type;
            switch (type) {
                case DialogType.AskForAssistance:
                    Image.Source = new BitmapImage(new Uri(@"/resources/afa.png", UriKind.Relative));
                    break;
                case DialogType.EnterWeight:
                    Image.Source = new BitmapImage(new Uri(@"/resources/weight.png", UriKind.Relative));
                    break;
                case DialogType.Error:
                    Image.Source = new BitmapImage(new Uri(@"/resources/error.png", UriKind.Relative));
                    break;
            }

            if (type != DialogType.EnterWeight)
            {
                TextAnswer.Visibility = Visibility.Hidden;
                btnDialogOk.Visibility = Visibility.Hidden;
                btnDialogCancel.Content = "Close";
            }
        }

        private void DialogOKClicked(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void DialogCancelClicked(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void WindowRendered(object sender, EventArgs e)
        {
            TextAnswer.SelectAll();
            TextAnswer.Focus();
        }

        public string Answer
        {
            get { return TextAnswer.Text; }
        }
    }
}
