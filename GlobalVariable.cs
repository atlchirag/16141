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
        //public static string connectionString = "Data Source=192.168.23.131,15433;Initial Catalog=newtrack;User ID=newtrack;Password=55hD&44m7E3jnd;Max Pool Size=32767;";
        //  public static string connectionString = "Data Source=45.113.189.23;Initial Catalog=newtrack;User ID=newtrack;Password=55hD&44m7E3jnd";
        public static string connectionStringctrls  = "Data Source= 192.168.23.131,15433;Initial Catalog=newtrack;User ID=newtrack;Password=55hD&44m7E3jnd;Max Pool Size=32767;";
        //public static string connectionStringctrls = "Data Source=(localdb)\\MSSQLLocalDB;Database=newtrack;Trusted_Connection=True;TrustServerCertificate=True;";

        public static string ServiceName { get; set; }
        public static string Type { get; set; }

        public static string m_folderpath = @"E:\ListenerData\AIS\16141";


    }
}
