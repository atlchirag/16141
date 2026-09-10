using GPSTrackerListeners.AtlantaNew;
using SuperSocket.SocketBase.Config;
using SuperSocket.SocketEngine;
using System;
using System.Collections.Generic;
using System.Configuration.Install;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Reflection;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using static System.Collections.Specialized.BitVector32;


namespace GPSTrackerListeners.ORSAC
{
    static class StringExtensions
    {

        public static IEnumerable<String> SplitInParts(this String s, Int32 partLength)
        {
            if (s == null)
                throw new ArgumentNullException("s");
            if (partLength <= 0)
                throw new ArgumentException("Part length has to be positive.", "partLength");

            for (var i = 0; i < s.Length; i += partLength)

                yield return s.Substring(i, Math.Min(partLength, s.Length - i));
        }

    }
    class Program : ServiceBase
    {


        static void Main(string[] args)
        {

            //string a = "abc 123\nxyz 112\n";
            //string[] b=a.Split('\t');

            //string[] data = a.Split('\n');
            //General.GetInitialValues();

            //var server = new ORSACServer();
            //var config = new ServerConfig { Port = 16141, MaxRequestLength = 40960000, MaxConnectionNumber = 20000 };
            //server.Setup(config);
            //server.Start();
            //Console.Read();

            //double dis = General.HaversineInKM(Convert.ToDouble("32.60265"), Convert.ToDouble("74.906616"), Convert.ToDouble("32.605965"), Convert.ToDouble("74.911392"));


            //dis = General.HaversineInKM(Convert.ToDouble("32.605965"), Convert.ToDouble("74.911392"), Convert.ToDouble("32.60265"), Convert.ToDouble("74.906616"));

            if (args.Length > 0)
            {
                for (int ii = 0; ii < args.Length; ii++)
                {
                    switch (args[ii].ToUpper())
                    {
                        case "/I":
                            InstallService();
                            return;
                        case "/U":
                            UninstallService();
                            return;
                        default:
                            break;
                    }
                }
            }

            else
                System.ServiceProcess.ServiceBase.Run(new Program());

        }


        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            string loggerMessage = DateTime.Now + Environment.NewLine +
                                  "An unhandled exception has occured in method " + new StackTrace(((Exception)e.ExceptionObject), true).GetFrame(0).GetMethod().Name + " at line no." + new StackTrace(((Exception)e.ExceptionObject), true).GetFrame(0).GetFileLineNumber() + " : " + Environment.NewLine +
                                 "Exception is: " + ((Exception)e.ExceptionObject).Message;
            General.WriteToLogFile(loggerMessage, AppDomain.CurrentDomain.BaseDirectory, "exUn.txt");


        }

        protected override void OnStart(string[] args)
        {
            try
            {
                base.OnStart(args);
                AsyncLogWriter.EnsureStarted();
                AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);

                var server = new ORSACServer();
                var config = new ServerConfig { Port = 16141, MaxConnectionNumber = 20000 };
                server.Setup(config);
                server.Start();
                Console.Read();
  
            }
            catch(Exception ex)
            {
               General.WriteToLogFile(ex.Message, "E:/ListenerData/AtlantaNew/16141", "listnerstartERROR.txt");
            }
        }

        //protected override void OnStop()
        //{
        //    base.OnStop();
        //}
        protected override void OnStop()
        {
            try { AsyncLogWriter.Stop(); } catch { }
            base.OnStop();
        }


        protected override void Dispose(bool disposing)
        {
            //clean your resources if you have to
            base.Dispose(disposing);
        }

        private static void InstallService()
        {
            if (IsServiceInstalled())
            {
                UninstallService();
            }

            ManagedInstallerClass.InstallHelper(new string[] { Assembly.GetExecutingAssembly().Location });
        }

        private static bool IsServiceInstalled()
        {
            return ServiceController.GetServices().Any(s => s.ServiceName == "ListenerAis_140_16141_new");
        }

        private static void UninstallService()
        {

            ManagedInstallerClass.InstallHelper(new string[] { "/u", Assembly.GetExecutingAssembly().Location });
        }


    }
}
