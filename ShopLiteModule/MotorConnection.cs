using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;
using System.Data;

namespace ShopLiteModule
{
    class MotorConnection
    {
        public bool isMotorRunning;
        private DBConnection con;
        SerialPort currentPort;
        bool portFound;
        
        public MotorConnection(DBConnection dbCon)
        {
            isMotorRunning = false;
            con = dbCon;
            currentPort = new SerialPort();
            portFound = false;
            SetComPort();
        }

        private void SetComPort()
        {
            try
            {
                string[] ports = SerialPort.GetPortNames();
                foreach (string port in ports)
                {
                    currentPort = new SerialPort(port, 9600);
                    if (rotateMotor())
                    {
                        portFound = true;
                        break;
                    }
                    else
                    {
                        portFound = false;
                    }
                }
            }
            catch (Exception e)
            {
            }
        }

        public bool rotateMotor()
        {
            try
            {
                //The below setting are for the Hello handshake
                byte[] buffer = new byte[5];
                //buffer[0] = Convert.ToByte('1'); // header bit
                // command bit

                buffer[0] = Convert.ToByte('A'); // 90 CW, 90 CCW
                buffer[1] = Convert.ToByte(readPrevCount()); // 90 CW, 90 CCW

                currentPort.Open();
                currentPort.Write(buffer, 0, 2);
                int count =  currentPort.BytesToRead;
                while(count > 0){ //clear all the info in port
                    currentPort.ReadByte();
                    count--;
                }
                currentPort.Close();
                isMotorRunning = true;
                return true;
            }
            catch (Exception e)
            {
                return false;
            }
        }

        public int stopMotor()
        {
            try
            {
                byte[] buffer = new byte[5];
                buffer[0] = Convert.ToByte('S'); //Stop byte
                
                currentPort.Open();
                currentPort.Write(buffer, 0, 1);
                int count = currentPort.BytesToRead;
                int returnMessage = 0;
                while (count > 0)//TODO : check this mechanism
                {
                    returnMessage = currentPort.ReadByte();
                    Console.Out.WriteLine("Motor stopped at position: " + returnMessage);
                    count--;
                }
                currentPort.Close();
                savePrevCount(returnMessage);
                isMotorRunning = false;
                return returnMessage;
            }
            catch (Exception e)
            {
            }
            return -1;
        }

        private int readPrevCount()
        {
            DataTable newItems = con.MyDataTable("SELECT * FROM Motor");
            int prevCount = -1;
            foreach (DataRow row in newItems.Rows)
            {
                prevCount = Convert.ToInt32(row["Position"].ToString());
                Console.Out.WriteLine("prevCount value is found in database: " + prevCount);
            }
            con.RunQuery("DELETE * FROM Motor"); // clear up database
            if (prevCount == -1)
            {
                prevCount = 0;
            }
            return prevCount;
        }

        private void savePrevCount(int prevCount)
        {
            con.RunQuery("INSET INTO Motor Values(" + prevCount + ")");
            Console.Out.WriteLine("prevCount value is updated in database");
        }
    }
}
