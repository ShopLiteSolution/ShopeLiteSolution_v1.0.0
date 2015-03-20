using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Data.SqlClient;

namespace UIMockup
{
    class DBConnection
    {
        private string sqlConnectionString;
        private string query;
        private SqlDataAdapter da;

        public string sql 
        {
            set { query = value; }
        }

        public string connection_string
        {
            set { sqlConnectionString = value; }
        }

        private DataTable MyDataSet()
        {
            SqlConnection con = new SqlConnection(sqlConnectionString);
            con.Open();

            da = new SqlDataAdapter(query,con);

            DataTable dataset = new DataTable();
            da.Fill(dataset);
            con.Close();

            return dataset;
        }
    }
}
