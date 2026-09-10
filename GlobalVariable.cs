using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GPSTrackerListeners.ORSAC
{
   public class GlobalVariable
    {
        public static int ListenPort { get; set; }
        public static string IPAddress { get; set; }
        public static string connectionStringctrls  = "Data Source= {public ip};Initial Catalog={catalog};User ID={userid};Password={password};Max Pool Size=32767;";

        public static string ServiceName { get; set; }
        public static string Type { get; set; }

        public static string m_folderpath = @"E:\ListenerData\AIS\16141";


    }
}
