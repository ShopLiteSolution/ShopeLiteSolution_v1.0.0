using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShopLiteModule
{
    class DefaultSettings
    {
        //Scale connection
        public static string Default_COM = "COM1";
        public static string Default_Baudrate = "115200";   

        //Reader connection
        public static string SignalStrength = "fcc";
        public static string OutputPower = "33";
        public static string ipAddress = "192.168.0.178";
        public static string ipPort = "4001";

        public static double ErrorPercentage = 0.01d;
        public static double LightErrorP = 0.1d;
        public static double HeavyErrorP = 0.01d;
        public static double minimunRange = 1.7d;

        
    }
}
