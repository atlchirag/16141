

using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using GPSTrackerListeners.AtlantaNew;
using MongoDB.Driver.Builders;
using SuperSocket.SocketBase.Protocol;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Ini;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Mail;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static System.Collections.Specialized.BitVector32;

namespace GPSTrackerListeners.ORSAC
{
    public class General
    {
        private static readonly object PanicStateLock = new object();
        private static readonly Dictionary<string, bool> PanicActiveByServiceId = new Dictionary<string, bool>();

       public static readonly string connectionString = "Data Source={Public DB IP},15433;Initial Catalog=atltracking;User ID={UserID};Password={PW};Max Pool Size=32767;";
        


        public static bool DML(string query)
        {
            SqlConnection connection = null;
            try
            {
                using (connection = new SqlConnection(connectionString))
                {

                    using (SqlCommand cmd = new SqlCommand(query, connection))
                    {

                        connection.Open();
                        cmd.ExecuteNonQuery();
                        return true;
                    }
                }
            }
            catch (SqlException se)
            {
                General.WriteToLogFile("Packet Received \n" + se.Message, GlobalVariable.m_folderpath, "WrongProtocol\\ErrorMessage" + ".txt");

                if (se.ErrorCode != -2146232060)
                {

                    return false;
                }

            }
            finally
            {
                connection.Close();
            }
            return false;
        }




        public  static DataTable SelectQuery(string query)
        {
            SqlConnection conn = null;
            try
            {
                using (conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    SqlCommand command = new SqlCommand(query, conn);
                    SqlDataAdapter adapter = new SqlDataAdapter(command);
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);
                    return dt;
                }
            }
            catch (SqlException se)
            {
                General.WriteToLogFile("Packet Received \n" + se.Message, GlobalVariable.m_folderpath, "WrongProtocol\\ErrorMessage" + ".txt");

                return null;
            }
            finally
            {
                conn.Close();
            }
        }

        
     

        
        public static string GetImeiNumber(string String)
        {
            try
            {

                string sImeI = string.Empty;

                string[] sSpilt = String.Split(',');
                if (sSpilt[0] != "" || sSpilt.Length > 5)
                {
                    //if (String.Contains("ASP"))
                    {
                        for (int j = 0; j < sSpilt.Length; j++)
                        {
                            string IsImei = sSpilt[j].ToString();
                            if (IsImei.Contains('$'))
                            {
                                string sresult = IsImei.Replace("$", string.Empty);
                                IsImei = sresult;
                            }
                            if (IsImei.Length > 12)
                            {
                                if (isDigits(IsImei) == true)
                                {
                                    return IsImei;

                                }

                            }
                        }
                    }
                    //else if (String.Contains("ASPL"))
                    //{
                    //    for (int j = 0; j < sSpilt.Length; j++)
                    //    {
                    //        string IsImei = sSpilt[j].ToString();
                    //        if (IsImei.Contains('$'))
                    //        {
                    //            string sresult = IsImei.Replace("$", string.Empty);
                    //            IsImei = sresult;
                    //        }
                    //        if (IsImei.Length > 12)
                    //        {
                    //            if (isDigits(IsImei) == true)
                    //            {
                    //                return IsImei;

                    //            }

                    //        }
                    //    }
                    //}
                   
                }


            }
            catch (Exception ex)
            {
                General.WriteToLogFile("Packet Received \n" + ex.Message, GlobalVariable.m_folderpath, "WrongProtocol\\ErrorMessage" + ".txt");

                return "";

            }
            return "";

        }
        public static bool isDigits(string s)
        {
            if (s == null || s == "") return false;

            for (int i = 0; i < s.Length; i++)
                if ((s[i] ^ '0') > 9)
                    return false;

            return true;
        }
        
        public static string GetServiceID(string IMEINumber)
        {
            SqlConnection connection = null;
            string str = "0";
            try
            {
                using (connection = new SqlConnection(connectionString))
                {

                    connection.Open();
                    SqlDataAdapter SqlDataAdapter = new SqlDataAdapter("select tbl_services.id,imei from tbl_devices inner join tbl_services on tbl_devices. id = tbl_services.sys_device_id where imei = '" + IMEINumber + "'", connection);
                    DataSet dataSet = new DataSet();
                    ((DataAdapter)SqlDataAdapter).Fill(dataSet);
                    DataTable dataTable = dataSet.Tables[0];
                    if (dataTable.Rows.Count > 0)
                        str = dataTable.Rows[0][0].ToString();
                    //else
                    //WriteToLogFile(IMEINumber,
                    //Logger.Log(FolderPath + "\\InvalidServiceID.txt", IMEINumber);
                    dataTable.Dispose();
                    dataSet.Dispose();
                    SqlDataAdapter.Dispose();
                }

            }
            catch (Exception ex)
            {
                General.WriteToLogFile(ex.Message, "E:/ListenerData/AtlantaNew/16141", "imei ERROR.txt");
                return "0";
            }
            finally
            {
                connection.Close();
            }

            return str;
        }

        public static bool CreateSessionID(string SessionID)
        {
            try
            {
                CustomSession obj = new CustomSession();
                obj.ServiceId = SessionID;
                return true;
            }
            catch (Exception ex)
            {
                General.WriteToLogFile(ex.Message, "E:/ListenerData/AtlantaNew/16141", "session ERROR.txt");

            }
            return false;


        }
       
        public static double GetLatitude(string latitude)
        {



            if (latitude.Length <= 4)
                return 0.0;
            string[] strArray = latitude.Split('.');
            double num = Convert.ToDouble(strArray[0].Substring(strArray[0].Length - 2) + "." + strArray[1]) / 60.0;
            return Convert.ToDouble(strArray[0].Substring(0, strArray[0].Length - 2)) + num;
        }

       
        public static DateTime GetGPSDateTime(string dateStamp, string timeStamp)
        {
            try
            {


                return new DateTime(Convert.ToInt32(dateStamp.Substring(4, 4)), Convert.ToInt32(dateStamp.Substring(2, 2)),
                                   Convert.ToInt32(dateStamp.Substring(0, 2)), Convert.ToInt32(timeStamp.Substring(0, 2)),
                                    Convert.ToInt32(timeStamp.Substring(2, 2)), Convert.ToInt32(timeStamp.Substring(4, 2)));

                //return new DateTime(Convert.ToInt32("20" + dateStamp.Substring(4, 2)), Convert.ToInt32(dateStamp.Substring(2, 2)),
                //                      Convert.ToInt32(dateStamp.Substring(0, 2)), Convert.ToInt32(timeStamp.Substring(0, 2)),
                //                      Convert.ToInt32(timeStamp.Substring(2, 2)), Convert.ToInt32(timeStamp.Substring(4, 2)));


            }
            catch (Exception ex)
            {
                return new DateTime();
            }
        }

        public static int SendEmail(string subject, string messageBody, string toEmailId, List<string> cc = null)
        {
            try
            {
                var fromAddress = new MailAddress("appdev1@atlantasys.com");
                var toAddress = new MailAddress(toEmailId);
                const string fromPassword = "";

                var smtp = new SmtpClient
                {
                    Host = "smtp.gmail.com",
                    Port = 587,
                    EnableSsl = true,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(fromAddress.Address, fromPassword)
                };
                using (var message = new MailMessage(fromAddress, toAddress)
                {
                    Subject = subject,
                    Body = messageBody
                })
                {
                    ServicePointManager.ServerCertificateValidationCallback =
   delegate(object s, X509Certificate certificate,
            X509Chain chain, SslPolicyErrors sslPolicyErrors)
   { return true; };

                    //   smtp.Send(message);
                    return 1;
                }
            }
            catch (Exception ex)
            {
                return 0;
            }

        }


        public delegate T RetryOpenDelegate<T>();

        public static T RetryOpen<T>(RetryOpenDelegate<T> action)
        {
            while (true)
            {

                try
                {

                    return action();

                }

                catch (IOException)
                {

                    System.Threading.Thread.Sleep(50);

                }

            }
            
        }

        //public static void WriteToLogFile(string msg, string folderPath, string logFile, bool isAppend = true, bool isDateTime = true)
        //{


        //    string dir = folderPath + "\\" + DateTime.Now.ToString("ddMMyyyy");

        //    if (!Directory.Exists(dir))

        //        Directory.CreateDirectory(dir);

        //    if (!Directory.Exists(dir + "\\ReceivedData"))
        //        Directory.CreateDirectory(dir + "\\ReceivedData");

        //    if (!Directory.Exists(dir + "\\WrongProtocol"))
        //        Directory.CreateDirectory(dir + "\\WrongProtocol");
        //    if (!Directory.Exists(dir + "\\CommandSend"))
        //        Directory.CreateDirectory(dir + "\\CommandSend");




        //    string LogFile = dir + "\\" + logFile;

        //    // Console.WriteLine("Raw data written to " + dir);

        //    TextWriter tw = null;

        //    try
        //    {



        //        tw = RetryOpen<StreamWriter>(delegate()
        //        {

        //            return new StreamWriter(LogFile, isAppend);



        //        });


        //        // write a line of text to the file
        //        if (isDateTime)
        //            tw.WriteLine(DateTime.Now + Environment.NewLine + msg);
        //        else
        //            tw.WriteLine(msg);

        //    }

        //    catch { }

        //    finally
        //    {

        //        // close the stream

        //        if (tw != null)
        //        {

        //            tw.Close();

        //            tw.Dispose();

        //        }

        //    }

        //}

        public static void WriteToLogFile(string msg, string folderPath, string logFile, bool isAppend = true, bool isDateTime = true)
        {
            try
            {
                // EXACT SAME folder structure as your current code
                string dir = folderPath + "\\" + DateTime.Now.ToString("ddMMyyyy");

                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                if (!Directory.Exists(dir + "\\ReceivedData"))
                    Directory.CreateDirectory(dir + "\\ReceivedData");

                if (!Directory.Exists(dir + "\\WrongProtocol"))
                    Directory.CreateDirectory(dir + "\\WrongProtocol");

                string fullPath = dir + "\\" + logFile;

                string line = isDateTime
                    ? (DateTime.Now + Environment.NewLine + msg)
                    : msg;

                // IMPORTANT: async write (same file path, same content)
                AsyncLogWriter.Enqueue(fullPath, line, isAppend);
            }
            catch
            {
                // do nothing (same as your current behaviour)
            }
        }



        public static void SendSms(string message, string mobileNo)
        {
            string receivedData = string.Empty;

            try
            {
                //string msgSenderUrl = "http://bulksms.a-tracker.com/sendsms.php?UserId=abctraq&Pwd=abctraq&Mobileno=" + mobileNo + "&Msg=" + message + "&SenderId=CellApps";
                message = message.Replace("(", "");
                message = message.Replace(")", "");
                message = message.Replace(@"\", "");
                message = message.Replace(@"-", " ");


                StringBuilder sb = new StringBuilder();
                //  int mob_len = 0;
                // StringBuilder student_id = new StringBuilder();

                sb.Clear();
                sb.Append("\"91" + mobileNo + "\"");
                string mobileNo_new = sb.ToString();
                // string msgSenderUrl = "http://182.18.176.147/pushsms.php?username=abctrq&password=90340&sender=abctrq&message=" + message + "&numbers=" + mobileNo;
                WebClient wc = new WebClient();
                wc.UseDefaultCredentials = false;
                wc.Credentials = new NetworkCredential("Atlanta-Systems", "Sandeep@123");
                wc.Headers.Add("Content-Type", "application/json");
                wc.Headers.Add("Authorization", "Basic QXRsYW50YS1TeXN0ZW1zOlNhbmRlZXBAMTIz");

                string data = wc.UploadString("https://api.infobip.com/sms/1/text/single", "{\"from\":\"ATLNTA\",\"to\":[" + mobileNo_new + "],\"text\":\"" + message + "\"}");

            }
            catch (Exception ex)
            {
                General.WriteToLogFile(ex.Message, "E:/ListenerData/AtlantaNew/16141", "ODOMETER ERROR.txt");

            }
        }

        //public static DataTable SelectQuery_ais16140(string query)
        //{
        //    try
        //    {
        //        using (SqlConnection conn = new SqlConnection(connectionString1))
        //        {
        //            conn.Open();
        //            SqlCommand command = new SqlCommand(query, conn);
        //            SqlDataAdapter adapter = new SqlDataAdapter(command);
        //            DataTable dt = new DataTable();
        //            adapter.Fill(dt);
        //            return dt;
        //        }
        //    }
        //    catch (SqlException se)
        //    {
        //        General.WriteToLogFile("Packet Received \n" + se.Message, GlobalVariable.m_folderpath, "WrongProtocol\\ErrorMessage" + ".txt");

        //        return null;
        //    }
        //}

        //public static void DMLinais16140(string query)
        //{
        //    try
        //    {
        //        using (SqlConnection connection = new SqlConnection(connectionString1))
        //        {

        //            using (SqlCommand cmd = new SqlCommand(query, connection))
        //            {

        //                connection.Open();
        //                cmd.ExecuteNonQuery();
                        
        //            }
        //        }
        //    }
        //    catch (SqlException se)
        //    {
        //        General.WriteToLogFile("Packet Received \n" + se.Message, GlobalVariable.m_folderpath, "WrongProtocol\\ErrorMessage" + ".txt");

        //        if (se.ErrorCode != -2146232060)
        //        {

                    
        //        }

        //    }
        //}


        //public static void SendCommand(CustomSession session, StringRequestInfo requestInfo)
        //{
        //    string command = requestInfo.Body.Substring(10);

        //    session.RequestTime = DateTime.Now;
        //    session.CommandRequestId = Guid.NewGuid().ToString();

        //    SqlParameter[] param = new SqlParameter[4];
        //    param[0] = new SqlParameter("@sys_service_id", requestInfo.Key);
        //    param[1] = new SqlParameter("@command", command);
        //    param[2] = new SqlParameter("@send_time", session.RequestTime.ToString("yyyy-MM-dd HH:mm:ss"));
        //    param[3] = new SqlParameter("@request_id", session.CommandRequestId);

        //    string query = @"INSERT INTO send_command_log (sys_service_id,command,send_time,request_id) VALUES (@sys_service_id,@command,@send_time,@request_id)";
        //    General.DML(query, param);

        //    session.Send(command);
        //    session.IsResponsePending = true;
        //}

        //public static void HandleNewRequest(CustomSession session, StringRequestInfo requestInfo, string m_FolderPath)
        //{
        //    // If wrong protocol or unmatched protocol packet received
        //    if (requestInfo.Key == "WrongProtocol")
        //    {
        //        if (requestInfo.Parameters[0] != null)
        //            General.WriteToLogFile(requestInfo.Body, m_FolderPath, "WrongProtocol\\" + requestInfo.Parameters[0] + ".txt");
        //        else
        //            General.WriteToLogFile(requestInfo.Body, m_FolderPath, "WrongProtocol\\Others.txt");
        //        return;
        //    }
        //    if (requestInfo.Key.Length == 15)
        //        General.WriteToLogFile(requestInfo.Body, m_FolderPath, "ReceivedData\\" + requestInfo.Key + ".txt");
        //    else
        //        General.WriteToLogFile(session.IMEI + "," + requestInfo.Body, m_FolderPath, "ReceivedData\\ex.txt");

        //    if (requestInfo.Body.Contains("Second Server IP Changed to"))
        //    {
        //        //   string imei0 = requestInfo.Body.Split(',')[0].Substring(1);
        //        string imei0 = session.IMEI;

        //        if (requestInfo.Body.Contains("$"))
        //        {
        //            imei0 = requestInfo.Body.Substring(requestInfo.Body.IndexOf("$") + 1, 15);
        //        }

        //        if (imei0.Length == 15)
        //            General.DML(@"update orsac_devices set is_second_ip_configured=1 where imei='" + imei0 + "'");
        //        return;
        //    }

        //    if (requestInfo.Body.Contains("Server IP Changed to") && !requestInfo.Body.Contains("Second"))
        //    {
        //        General.WriteToLogFile(session.IMEI + "," + requestInfo.Body, m_FolderPath, "res.txt");

        //        string imei0 = session.IMEI;

        //        if (requestInfo.Body.Contains("$"))
        //        {
        //            imei0 = requestInfo.Body.Substring(requestInfo.Body.IndexOf("$") + 1, 15);
        //        }

        //        if (imei0.Length == 15)
        //        {

        //            General.DML(@"update orsac_devices set is_primary_ip_configured=1 where imei='" + imei0 + "'");

        //        }

        //        //string imei0 = requestInfo.Body.Split(',')[0].Substring(1);


        //        return;
        //    }

        //    try
        //    {
        //        if (string.IsNullOrEmpty(session.IMEI))
        //        {

        //            DataTable dt1 = General.SelectQuery(@"select * from orsac_devices");

        //            if (dt1 != null)
        //            {
        //                if (dt1.Rows.Count >= 0)
        //                {
        //                    string imei = requestInfo.Key;
        //                    if (requestInfo.Key.Length == 15)
        //                    {
        //                        session.IMEI = requestInfo.Key;

        //                        string[] fieldsArray = requestInfo.Parameters[1].Split(',');

        //                        string gpsTime = new DateTime().ToString("yyyy-MM-dd HH:mm:ss");

        //                        try
        //                        {
        //                            gpsTime = General.GetGPSDateTime(fieldsArray[9], fieldsArray[10]).ToString("yyyy-MM-dd HH:mm:ss");

        //                            if (gpsTime.Contains("0001"))
        //                            {
        //                                gpsTime = "2000-01-01 00:00:00";
        //                            }

        //                        }
        //                        catch (Exception ex)
        //                        {

        //                        }




        //                    }
        //                }
        //            }

        //            if (requestInfo.Key.Length < 15)
        //                return;

        //            foreach (char c in requestInfo.Key)
        //            {
        //                if (!char.IsDigit(c))
        //                    return;
        //            }

        //            string serviceId;
        //            string mobileNo = "";


        //            serviceId = General.GetServiceID(requestInfo.Key);

        //            session.ServiceId = serviceId;

        //            if (serviceId != "0")
        //            {


        //                session.MobileNo = mobileNo;

        //                CheckUpdate(session);

        //                if (session.UTCOffset == 0)
        //                {
        //                    string query = @"select t.utc_offset,u.id,s.veh_reg
        //                                    FROM users u inner join user_services s on u.id=s.sys_user_id 
        //                                    inner join time_zone_php t on t.tz_name=u.sys_timezone 
        //                                    left join vehicle_group v on  v.id=s.vehicle_group_id 
        //                                    WHERE s.sys_service_id=" + session.ServiceId;
        //                    DataTable dt = null;




        //                    dt = SelectQuery(query);

        //                    if (dt != null)
        //                    {
        //                        if (dt.Rows.Count > 0)
        //                        {
        //                            session.UserId = Convert.ToInt32(dt.Rows[0]["id"]);
        //                            session.UTCOffset = Convert.ToInt32(dt.Rows[0]["utc_offset"]);
        //                            session.VehicleName = Convert.ToString(dt.Rows[0]["veh_reg"]);

        //                        }
        //                    }
        //                }



        //            }
        //            else
        //            {
        //                General.WriteToLogFile(requestInfo.Key, m_FolderPath, "InvalidServiceId.txt", true, false);
        //                return;
        //            }
        //        }




        //        if (!session.IsPortUpdated)
        //        {
        //            string query = @"UPDATE services SET port_no=" + session.Config.Port + " WHERE id=" + session.ServiceId;

        //            General.DML(query);
        //            session.IsPortUpdated = true;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        General.WriteToLogFile(ex.Message, m_FolderPath, "exHandleRequest.txt");
        //    }
        //}
       
        static double _eQuatorialEarthRadius = 6378.1370D;
        static double _d2r = (Math.PI / 180D);

        public static double HaversineInKM(double lat1, double long1, double lat2, double long2)
        {
            double dlong = (long2 - long1) * _d2r;
            double dlat = (lat2 - lat1) * _d2r;
            double a = Math.Pow(Math.Sin(dlat / 2D), 2D) + Math.Cos(lat1 * _d2r) * Math.Cos(lat2 * _d2r) * Math.Pow(Math.Sin(dlong / 2D), 2D);
            double c = 2D * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1D - a));
            double d = _eQuatorialEarthRadius * c;

            return d;
        }

        public static bool IsAlertTime(DateTime timeFrom, DateTime timeTo)
        {
            try
            {

                if (timeTo < timeFrom)
                    timeTo = timeTo.AddDays(1);

                // Checking if current time is within specified range
                if (DateTime.Now >= timeFrom && DateTime.Now <= timeTo)
                    return true;

                return false;
            }
            catch (Exception ex) { return false; }

        }

        public static void CheckUpdate(CustomSession session,General gen)
        {

            try
            {
                if (!string.IsNullOrEmpty(session.ServiceId))
                {

                    DataTable dtPanicAlert = General.SelectQuery(@"select p.*,s.veh_reg from  panicalert p, services s where p.sys_service_id=s.id and s.is_active=1 and s.id=" + session.ServiceId);


                    if (dtPanicAlert != null)
                    {
                        if (dtPanicAlert.Rows.Count > 0)
                        {

                            session.PanicAlert = new PanicAlert();

                            session.IsPanicAlert = true;

                            session.PanicAlert.Email = dtPanicAlert.Rows[0]["alert_email"].ToString();
                            session.PanicAlert.Email1 = dtPanicAlert.Rows[0]["alert_email1"].ToString();
                            session.PanicAlert.Email2 = dtPanicAlert.Rows[0]["alert_email2"].ToString();
                            session.PanicAlert.Mobile = dtPanicAlert.Rows[0]["alert_mobileNo"].ToString();
                            session.PanicAlert.Mobile1 = dtPanicAlert.Rows[0]["alert_mobileNo1"].ToString();
                            session.PanicAlert.Mobile2 = dtPanicAlert.Rows[0]["alert_mobileNo2"].ToString();
                            session.PanicAlert.FromAlertTime = TimeSpan.Parse(dtPanicAlert.Rows[0]["from_alert_time"].ToString());
                            session.PanicAlert.ToAlertTime = TimeSpan.Parse(dtPanicAlert.Rows[0]["to_alert_time"].ToString());
                            session.VehicleName = Convert.ToString(dtPanicAlert.Rows[0]["veh_reg"]);

                        }
                        else
                        {
                            //session.PanicAlert = null;
                            session.IsPanicAlert = false;
                        }

                    }
                    else
                    {
                        // session.PanicAlert = null;
                        session.IsPanicAlert = false;
                    }

                }
                else
                    session.IsAlertCheck = true;
            }
            catch (Exception ex)
            {
                General.WriteToLogFile(ex.Message, "E:/ListenerData/AtlantaNew/16141", "ODOMETER ERROR.txt");
            }
        }


        //public static void SendNotification(string msg, int user_id)
        //{
        //    // EtaPredict p = new EtaPredict();


        //    string address = "https://fcm.googleapis.com/fcm/send";
        //    DataTable dt = null;


        //    try
        //    {

        //        dt = General.SelectQuery("select auid,iuid from user_did where sys_user_id=" + user_id);
        //        if (dt != null)
        //        {
        //            if (dt.Rows.Count != 0)
        //            {
        //                foreach (DataRow dr in dt.Rows)
        //                {
        //                    using (WebClient wc = new WebClient())
        //                    {
        //                        wc.Headers.Add("Content-Type", "application/json");
        //                        wc.Headers.Add("Authorization", "key=AAAAT3DETk8:APA91bF6KEzkfSZPWohBFj1eCct1U3JlbtWulHxNFqjmw9n75TFYsI3dpogkCsoUkndJvo9nN8WfGngbKw7Yw4puzdX_KX3tHmi3-ShrWHEsgsRyDo6hnU_zQeLiZS9hLJaTosmwm-MT");
        //                        if (!string.IsNullOrEmpty(dr["auid"].ToString()))
        //                        {
        //                            try
        //                            {
        //                                wc.UploadString(address, "{\"to\":\"" + dr["auid"].ToString().Trim() + "\",\"data\": {\"abctraq\": \"" + msg + "\"}}");

        //                            }
        //                            catch (Exception e)
        //                            {
        //                                General.WriteToLogFile(e.Message, AppDomain.CurrentDomain.BaseDirectory, "Notification_Exp.txt");
        //                            }


        //                        }
        //                        if (!string.IsNullOrEmpty(dr["iuid"].ToString()))
        //                        {
        //                            try
        //                            {

        //                                // string s = wc.UploadString(address, "{\"to\":\"" + dr["iuid"].ToString().Trim() + "\",\"notification\": {\"body\": \"" + msg + "\"}}");
        //                                wc.UploadString(address, "{\"to\":\"" + dr["iuid"].ToString().Trim() + "\",\"notification\": {\"body\": \"" + msg + "\",\"priority\" : \"high\",\"sound\": \"default\"}}");


        //                            }
        //                            catch (Exception e)
        //                            {
        //                                General.WriteToLogFile(e.Message, AppDomain.CurrentDomain.BaseDirectory, "Notification_Exp.txt");
        //                            }


        //                        }

        //                    }
        //                }
        //                //DML("insert into tbl_alert_log(alert_setting_id,message,sent_on,gps_latitude,gps_longitude) values (" + alert_setting_id + ",'" + msg + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "'," + lat + "," + lng + ")");

        //            }
        //        }

        //    }
        //    catch (Exception e)
        //    {
        //        General.WriteToLogFile(e.Message, AppDomain.CurrentDomain.BaseDirectory, "Notification_Exp.txt");
        //    }

        //}


        public static async Task<string> GetLocationFromLatLong(double latitude, double longitude)
        {
            string address = "NA";
            if (latitude != null && longitude != null)
            {
                using (WebClient wc = new WebClient())
                {
                    try
                    {
                        var handler = new HttpClientHandler
                        {
                            ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
                        };
                        HttpClient httpClient = new HttpClient(handler);
                        HttpResponseMessage httpRequestMessage = httpClient.GetAsync("https://fasttracksoft.us/api/get_address_mongo.php?latitude="+latitude+"&longitude="+longitude).Result;
                        if (httpRequestMessage.IsSuccessStatusCode)
                        {
                            address = httpRequestMessage.Content.ReadAsStringAsync().Result;
                        }
                        //address = wc.DownloadString("http://fasttracksoft.us/api/get_address_mongo.php?latitude="+latitude+"&longitude="+longitude);
                    }
                    catch(Exception ex) 
                    {
                        
                    }
                }
            }

            return address;
        }


        public static async Task SendNotification(int user_id, string msg, int alert_setting_id, double lat, double lng, string main_message)
        {
            try
            {
                // Fetch tokens for THIS user (remove hardcode)
                DataTable dt = General.SelectQuery(
                    $"SELECT auid, iuid, user_id FROM tbl_user_did WHERE user_id={user_id} ORDER BY id DESC");

                if (dt != null && dt.Rows.Count > 0)
                {
                    // Reuse a single Firebase instance in the loop (still backward-compatible)
                    var firebase = new GPSTrackerListeners.ORSAC.Firebase();

                    foreach (DataRow dr in dt.Rows)
                    {
                        string userid = dr["user_id"]?.ToString();

                        string auid = dr["auid"]?.ToString()?.Trim();
                        if (!string.IsNullOrEmpty(auid))
                            await firebase.FirebaseNotifications(auid, msg, userid);

                        string iuid = dr["iuid"]?.ToString()?.Trim();
                        if (!string.IsNullOrEmpty(iuid))
                            await firebase.FirebaseNotifications(iuid, msg, userid);
                    }

                    // Log the notification
                    DML("INSERT INTO tbl_alert_notification_log(alert_setting_id,message,sent_on,gps_latitude,gps_longitude) VALUES (" +
                        alert_setting_id + ",'" + main_message.Replace("'", "''") + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "'," + lat + "," + lng + ")");
                }
            }
            catch (Exception e)
            {
                General.WriteToLogFile(e.ToString(), AppDomain.CurrentDomain.BaseDirectory, "Notification_Exp.txt");
            }
        }



        //public static async Task SendNotification(int user_id, string msg, int alert_setting_id, double lat, double lng, string main_message,string service_id)
        //{ 

        //    DataTable dt = null;
        //    var firebase = new Firebase();
        //    string type = "alert";
        //    try
        //    {
        //        dt = General.SelectQuery($"select auid,iuid,user_id from tbl_user_did where user_id={user_id} order by id desc" );
        //        if (dt != null)
        //        {
        //            if (dt.Rows.Count != 0)
        //            {
        //                foreach (DataRow dr in dt.Rows)
        //                {

        //                    string userid = dr["user_id"].ToString();

        //                    //DataTable p3stbl = General.SelectQuery($"select id from tbl_users where domain_id='85' and id= {user_id} order by id desc");
        //                    //if (p3stbl.Rows.Count > 0)
        //                    {
        //                        if (!String.IsNullOrEmpty(dr["auid"].ToString().Trim()))
        //                        {
        //                            //firebase = new Firebase();
        //                            await firebase.FirebaseNotifications(dr["auid"].ToString().Trim(), msg,userid);
        //                        }


        //                    }

        //                    //else
        //                    {
        //                        if (!String.IsNullOrEmpty(dr["iuid"].ToString().Trim()))
        //                        {
        //                            //firebase = new Firebase();
        //                            await firebase.FirebaseNotifications(dr["iuid"].ToString().Trim(), msg, userid);
        //                        }

        //                    }


        //                }

        //                General.DML("insert into tbl_alert_log(sys_service_id,alert_setting_id,message,sent_on,gps_latitude,gps_longitude) values (" + service_id + "," + alert_setting_id + ",'" + main_message + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "'," + lat + "," + lng + ")");
        //                //General.DML("insert into tbl_panicalert_log(sys_service_id,alert_msg,lastAlertTime,gps_latitude,gps_longitude) values (" + service_id + ",'" + main_message + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "'," + lat + "," + lng + ")");

        //                Thread.Sleep(100);
        //            }
        //        }

        //    }
        //    catch (Exception e)
        //    {
        //        General.WriteToLogFile(e.Message, AppDomain.CurrentDomain.BaseDirectory, "Notification_Exp.txt");
        //    }

        //}

//        public static async Task detactPanicAlert(string service_id, double latitude, double longitude, DateTime gps_time, string alert_type, string serviceid)
//        {
//            if (alert_type != "EA" && alert_type != "EU")
//            {
//                return;
//            }

//            lock (PanicStateLock)
//            {
//                bool isPanicActive = PanicActiveByServiceId.ContainsKey(service_id) && PanicActiveByServiceId[service_id];

//                if (alert_type == "EA")
//                {
//                    if (isPanicActive)
//                    {
//                        return;
//                    }

//                    PanicActiveByServiceId[service_id] = true;
//                }
//                else
//                {
//                    if (!isPanicActive)
//                    {
//                        return;
//                    }

//                    PanicActiveByServiceId[service_id] = false;
//                }
//            }

//            string location = General.GetLocationFromLatLong(latitude, longitude).Result;
//            string main_message = "NA";
//            if (location.Contains("fasttrack"))
//            {
//                location = "";
//            }

//            int alertId = alert_type == "EA" ? 6 : 42;
//            string is_notification_enable = $@"select tas.id as tbl_alert_setting_id,* from tbl_alert_master  tam
//                                                join tbl_alert_setting tas
//                                                on tam.id = tas.alert_id
//                                                where tas.alert_id = {alertId} and tas.service_id = {service_id} and tas.is_active=1 and tas.is_notification = 1";
//            string getvehiclename = $@"select veh_reg,sys_user_id from tbl_services where id = {service_id}";

//            DataTable dt = General.SelectQuery(is_notification_enable);
//            DataTable dt1 = General.SelectQuery(getvehiclename);
//            if (dt != null && dt.Rows.Count > 0 && dt1 != null && dt1.Rows.Count > 0)  
//            {
//                if (alert_type == "EA")
//                {
//                    string type = "trigger";
//                    main_message = string.Format(dt.Rows[0]["message"].ToString(), type, location, gps_time.AddMinutes(330).ToString("dd/MM/yyyy HH:mm"), dt1.Rows[0]["veh_reg"].ToString());
//                }
//                else
//                {
//                    string type = "released";
//                    main_message = string.Format(dt.Rows[0]["message"].ToString(), type, location, gps_time.AddMinutes(330).ToString("dd/MM/yyyy HH:mm"), dt1.Rows[0]["veh_reg"].ToString());
//                }

//                await General.SendNotification(Convert.ToInt32(dt1.Rows[0]["sys_user_id"].ToString()), main_message, Convert.ToInt32(dt.Rows[0]["tbl_alert_setting_id"].ToString()), latitude, longitude, main_message);

//                string insertQuery = @"insert into tbl_panicalert_log  
//(sys_service_id,lastAlertTime,alert_msg,readFlag,gps_latitude,gps_longitude)  
//values 
//(" + service_id + ",'"
//+ Convert.ToDateTime(gps_time).AddMinutes(330).ToString("yyyy-MM-dd HH:mm:ss")
//+ "','" + main_message.Replace("'", "''")
//+ "',0,'" + latitude + "','" + longitude + "')";

//                General.DML(insertQuery);
//            }
//        }


     
            public static async Task detactPanicAlert( string service_id, double latitude, double longitude, DateTime gps_time, string alert_type, string serviceid,DateTime gpstime)
            {
               try
            {
                if (alert_type != "EA" && alert_type != "EU")
                {
                    return;
                }

                string location =
                    General.GetLocationFromLatLong(latitude, longitude).Result;

                if (location.Contains("fasttrack"))
                {
                    location = "";
                }


                //string lastQuery = @"
                //SELECT TOP 1 alert_msg,lastAlertTime,sys_proc_time
                //FROM tbl_panicalert_log
                //WHERE sys_service_id = " + service_id + @"
                //AND
                //(
                //    alert_msg LIKE '%Panic Button%'
                //    OR
                //    alert_msg LIKE '%Emergency Free Acknowledgement%'
                //)
                //ORDER BY lastAlertTime DESC";
                string lastQuery = @"
SELECT TOP 1
    alert_msg,
    lastAlertTime,
    sys_proc_time
FROM tbl_panicalert_log
WHERE sys_service_id = " + service_id + @"
AND
(
    alert_msg LIKE '%Panic Button%'
    OR alert_msg LIKE '%Emergency Free Acknowledgement%'
)
AND sys_proc_time BETWEEN DATEADD(MINUTE,-30,'"
                + gpstime.ToString("yyyy-MM-dd HH:mm:ss") + @"')
AND '"
                + gpstime.ToString("yyyy-MM-dd HH:mm:ss") + @"'
ORDER BY sys_proc_time DESC";

                DataTable dtLast = General.SelectQuery(lastQuery);

                string lastMessage = "";
                DateTime? lastAlertTime = null;

                if (dtLast != null && dtLast.Rows.Count > 0)
                {
                    lastMessage =
                        dtLast.Rows[0]["alert_msg"].ToString();

                    lastAlertTime =
                        Convert.ToDateTime(
                            dtLast.Rows[0]["sys_proc_time"]);
                }


                if (lastAlertTime == null)
                {
                    if (alert_type == "EU")
                    {
                        return;
                    }
                }


                bool isOlderThan30Min = false;

                if (lastAlertTime != null)
                {
                    isOlderThan30Min =
                        DateTime.Now >
                        lastAlertTime.Value.AddMinutes(30);
                }

                if (!isOlderThan30Min)
                {
                    if (alert_type == "EA")
                    {
                   
                        if (!string.IsNullOrEmpty(lastMessage)
                            && lastMessage.Contains("Panic button"))
                        {
                            return;
                        }
                    }
                    else if (alert_type == "EU")
                    {
                        
                        if (string.IsNullOrEmpty(lastMessage))
                        {
                            return;
                        }

                
                        if (!lastMessage.Contains("Panic button"))
                        {
                            return;
                        }
                    }
                }


                int alertId = alert_type == "EA" ? 6 : 42;

                string is_notification_enable = $@"
        select tas.id as tbl_alert_setting_id,* 
        from tbl_alert_master tam
        join tbl_alert_setting tas
        on tam.id = tas.alert_id
        where tas.alert_id = {alertId}
        and tas.service_id = {service_id}
        and tas.is_active = 1
        and tas.is_notification = 1";

                string getvehiclename = $@"
        select veh_reg,sys_user_id
        from tbl_services
        where id = {service_id}";

                DataTable dt = General.SelectQuery(is_notification_enable);
                DataTable dt1 = General.SelectQuery(getvehiclename);

                if (dt != null
                    && dt.Rows.Count > 0
                    && dt1 != null
                    && dt1.Rows.Count > 0)
                {
                    string type =
                        alert_type == "EA"
                        ? "trigger"
                        : "released";

                    string main_message =
                        string.Format(
                            dt.Rows[0]["message"].ToString(),
                            type,
                            location,
                            gps_time.AddMinutes(330)
                            .ToString("dd/MM/yyyy HH:mm"),
                            dt1.Rows[0]["veh_reg"].ToString()
                        );





                    await General.SendNotification(
                        Convert.ToInt32(
                            dt1.Rows[0]["sys_user_id"].ToString()),
                        main_message,
                        Convert.ToInt32(
                            dt.Rows[0]["tbl_alert_setting_id"]
                            .ToString()),
                        latitude,
                        longitude,
                        main_message);




                    string insertQuery = @"
insert into tbl_panicalert_log
(
    sys_service_id,
    sys_user_id,
    lastAlertTime,
    alert_msg,
    readFlag,
    gps_latitude,
    gps_longitude,
   sys_proc_time 
)
values
(
    " + service_id + @",
    " + dt1.Rows[0]["sys_user_id"].ToString() + @",
    '" + Convert.ToDateTime(gps_time)
            .AddMinutes(330)
            .ToString("yyyy-MM-dd HH:mm:ss") + @"',
    '" + main_message.Replace("'", "''") + @"',
    0,
    '" + latitude + @"',
    '" + longitude + @"',
'" + gpstime.ToString("yyyy-MM-dd HH:mm:ss") + @"')";




                    General.DML(insertQuery);
                }
            }
            catch (Exception ex)
            {
                General.WriteToLogFile(
                    ex.ToString(),
                    AppDomain.CurrentDomain.BaseDirectory,
                    "panic_error.txt");
            }
            }



    }


    public class Firebase
    {
        private static readonly object _sync = new object();

        
        private static FirebaseApp _app;            // <- use this, not DefaultInstance
        private static bool _initialized = false;

        private readonly string _baseDir;

        public Firebase()
        {
            _baseDir = AppDomain.CurrentDomain.BaseDirectory;
            EnsureInitialized();
        }

        private static FirebaseApp TryGetDefaultApp()
        {
            try
            {
                return FirebaseApp.DefaultInstance; // some SDKs return null instead of throwing
            }
            catch
            {
                return null;
            }
        }

        // One-time, thread-safe init that always leaves _app non-null
        private void EnsureInitialized()
        {
            if (_initialized && _app != null) return;

            lock (_sync)
            {
                if (_initialized && _app != null) return;

                string serviceAccountPath = Path.Combine(
                     _baseDir,
                 //"schoolbuddy-4fc6d-firebase-adminsdk-xh2kk-f674f1807a.json"
                 "schoolbuddy-4fc6d-firebase-adminsdk-xh2kk-5fac47bd25.json"
                 //"trackofy-b7dc9-firebase-adminsdk-fbsvc-a57783caf2.json"
                 );
                if (!File.Exists(serviceAccountPath))
                {
                    // Log a clear error and throw so you see it right away
                    General.WriteToLogFile("FCM JSON not found: " + serviceAccountPath,
                        AppDomain.CurrentDomain.BaseDirectory, "notificationerror.txt");
                    throw new FileNotFoundException("Firebase service account JSON not found: " + serviceAccountPath);
                }

                // If someone already created a default app, use it.
                _app = TryGetDefaultApp();

                // Otherwise create our own default app.
                if (_app == null)
                {
                    _app = FirebaseApp.Create(new AppOptions
                    {
                        Credential = GoogleCredential.FromFile(serviceAccountPath),
                    });
                }

                _initialized = true;
            }
        }

        public async Task FirebaseNotifications(string auidoriuid, string msg, string uid)
        {
            try
            {
                EnsureInitialized(); // guarantees _app != null

                if (string.IsNullOrWhiteSpace(auidoriuid))
                    throw new ArgumentException("registration token is null/empty", nameof(auidoriuid));

                // IMPORTANT: bind messaging to our app (not DefaultInstance)
                var messaging = FirebaseMessaging.GetMessaging(_app);

                var message = new Message
                {
                    Notification = new Notification
                    {
                        Title = "trackofy",
                        Body = msg ?? string.Empty
                    },
                    Data = new Dictionary<string, string>
                {
                    { "trackofy", msg ?? string.Empty }
                },
                    Token = auidoriuid
                };

                string response = await messaging.SendAsync(message);
                General.WriteToLogFile($"{uid} : sent notification ({response})",
                    AppDomain.CurrentDomain.BaseDirectory, "notification.txt");
            }
            catch (Exception ex)
            {
                General.WriteToLogFile("Notification error: " + ex,
                    AppDomain.CurrentDomain.BaseDirectory, "notificationerror.txt");
            }
        }
    }
}
