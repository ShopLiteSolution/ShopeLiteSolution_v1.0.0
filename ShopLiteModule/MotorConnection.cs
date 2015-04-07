using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;
using System.Data;
using System.Threading;

namespace ShopLiteModule
{
    class MotorConnection
    {
        public bool isMotorRunning;
        private DBConnection con;
        SerialPort currentPort;
        bool portFound;
        bool shouldFlip;

        public MotorConnection(DBConnection dbCon)
        {
            shouldFlip = false;
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
                int prevCount = 0;
                if (shouldFlip)
                {
                    prevCount = 512 - readPrevCount();
                }
                else
                {
                    prevCount = readPrevCount();
                }
                shouldFlip = !shouldFlip;
                //int prevCount = readPrevCount();
                currentPort.Open();
                buffer[0] = (byte)(prevCount / 256);
                currentPort.Write(buffer, 0, 1);
                while (currentPort.BytesToRead == 0) {
                    //wait until "A" bytes come back
                }
                
                if(currentPort.ReadExisting() != "A"){
                    return false;
                }

                buffer[1] = (byte)(prevCount % 256);
                currentPort.Write(buffer, 1, 1);

                while (currentPort.BytesToRead == 0)
                {
                    //wait until "A" bytes come back
                }
                int count = currentPort.BytesToRead;
                if(count > 0){ //clear all the info in port
                    Console.Out.WriteLine("calculated count: "+ currentPort.ReadLine());
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
                while (currentPort.BytesToRead == 0) {
                    //wait until bytes come back
                }
                String number = "0";
                int count = currentPort.BytesToRead;
                if (currentPort.BytesToRead > 0)
                {
                    number = currentPort.ReadExisting();
                }
                currentPort.Close();

                int prevCount = Convert.ToInt32(number);
                savePrevCount(prevCount);
                isMotorRunning = false;
                return prevCount;
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
            con.MyDataTable("DELETE FROM Motor");
            if (prevCount == -1)
            {
                prevCount = 0;
            }
            if (prevCount >= 512)
            {
                prevCount = 511;
            }
            return prevCount;
        }

        private void savePrevCount(int prevCount)
        {
            if (prevCount == -1)
            {
                prevCount = 0;
            }
            if (prevCount >= 512)
            {
                prevCount = 511;
            }
            Console.Out.WriteLine("prevCount saved with " + prevCount);
            con.MyDataTable("INSERT INTO Motor Values(" + prevCount + ")");     
        }
    }
}
    