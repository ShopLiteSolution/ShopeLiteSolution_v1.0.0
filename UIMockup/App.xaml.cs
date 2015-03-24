using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Data.SqlClient;

namespace UIMockup
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private const string connectionString = @"Data Source=(LocalDB)\v11.0;
                AttachDbFilename=|DataDirectory|\ShopLiteSolutionDB.mdf;
                Integrated Security=True;Connect Timeout=30";
    }
}
