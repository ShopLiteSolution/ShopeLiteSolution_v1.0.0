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
        SerialPort currentPort;
        bool portFound;

        public MotorConnection(DBConnection dbCon)
        {
            isMotorRunning = false;
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

                currentPort.Open();
                buffer[0] = Convert.ToByte('A');
                currentPort.Write(buffer, 0, 1);
                while (currentPort.BytesToRead == 0) {
                    //wait until "A" bytes come back
                }
                if(currentPort.ReadExisting() != "A"){
                    return false;
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

        public bool stopMotor()
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
                if (currentPort.ReadExisting() != "S")
                {
                    return false;
                }
                currentPort.Close();
                isMotorRunning = false;
            }
            catch (Exception e)
            {
            }
            return true;
        }
    }
}
    