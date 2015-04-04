using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Finisar.SQLite;
using System.Data;

namespace ShopLiteModule
{
    class DBConnection
    {
        private const string dbConnectionString = @"Data Source=..\..\database\ShopLiteDB.db;Version=3;";

        private SQLiteConnection con;

        public DBConnection() {
            con = new SQLiteConnection(dbConnectionString);
            Console.Out.WriteLine("connection to:" + dbConnectionString + " is successful");
        }

        public DBConnection(string conString) {
            con = new SQLiteConnection(conString);
            Console.Out.WriteLine("connection to:" + conString + " is successful");
        }

        public DataTable MyDataTable(string query) {
            con.Open();

            SQLiteDataAdapter da = new SQLiteDataAdapter(query, con);
            DataTable datatable = new DataTable();
            da.Fill(datatable);
            
            con.Close();
            return datatable;
        }

        public void RunQuery(string query)
        {
            con.Open();
            SQLiteDataAdapter da = new SQLiteDataAdapter(query, con);
            con.Close();
        }
    }
}
