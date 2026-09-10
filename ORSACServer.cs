using FirebaseAdmin.Messaging;
using MongoDB.Bson;
using MongoDB.Driver;
using SuperSocket.SocketBase;
using SuperSocket.SocketBase.Config;
using SuperSocket.SocketBase.Protocol;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Net.Mail;
using System.Net.Sockets;
using System.Security.Policy;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GPSTrackerListeners.ORSAC
{
    public class ORSACServer : AppServer<CustomSession>
    {

        //      CustomSession oCustomSession = new CustomSession();
        public List<DTCCode> lstDtcCode = new List<DTCCode>();



        public static string mFolderpath;
        private string sDataType = string.Empty;
        private string m_DBType;
        ///  private string m_FolderPath;
        private string m_userName;
        public static ConcurrentQueue<string> QueuedData = new ConcurrentQueue<string>();
        string[] imei_commanding = { "861230045931426", "862846040374384", "861230045924363", "861230046147444", "861230046124369", "861230049273742", "862846040266093", "862846040356217", "861230049437123", "862846040270608", "861230045890796", "862846040226105", "861230049267520", "861230045927515", "862846040226667", "862846040349022", "862846040225172", "861230049252837", "861230049253066", "862846040273057", "861230046008877", "861230045976926", "861230049438956", "861230049389894", "861230045927416", "861230049355796", "861230049274898", "861230045891596", "861230049253991", "861230049287593", "861230049262703", "861230045976272", "862846040350723", "861230046145208", "861230045929891", "861230046145257", "861230045915619", "861230045924512", "861230045890770", "861230046076841", "861230045892263", "861230045890689", "861230045949386", "861230045976504", "861230045933562", "862846040225446", "862846040348719", "862846040348560", "862846040224696", "861230049312110", "861230049296016", "862846040348651", "862846040225263", "861230049274039", "862846040228929", "862846040265863" };

        public ORSACServer()
            : base(new DefaultReceiveFilterFactory<CustomReceiveFilter, StringRequestInfo>())
        {

            //this.NewRequestReceived += new RequestHandler<CustomSession, StringRequestInfo>(server_NewRequestReceived);

            this.NewRequestReceived += async (session, requestInfo) => await server_NewRequestReceived(session, requestInfo);

            // Tap the raw inbound bytes before the receive filter runs, so a byte-for-byte
            // copy of what the device sent is relayed to the mirror endpoint. The handler
            // always returns true, so normal processing continues exactly as before.
            if (RawMirrorConfig.Enabled)
                ((IRawDataProcessor<CustomSession>)this).RawDataReceived += MirrorRawData;

        }

        /// <summary>
        /// Copies every inbound packet to this session's mirror connection.
        /// Returns true unconditionally: the packet is then processed by the listener
        /// exactly as it was before the mirror existed.
        /// </summary>
        private bool MirrorRawData(CustomSession session, byte[] buffer, int offset, int length)
        {
            try
            {
                if (session != null)
                {
                    RawDataMirror mirror = session.RawMirror;

                    if (mirror == null)
                    {
                        mirror = new RawDataMirror(Convert.ToString(session.RemoteEndPoint));
                        session.RawMirror = mirror;
                    }

                    mirror.Enqueue(buffer, offset, length);
                }
            }
            catch
            {
                // Mirroring must never interfere with the listener.
            }

            return true;
        }

        protected override void OnSessionClosed(CustomSession session, CloseReason reason)
        {
            try
            {
                if (session != null && session.RawMirror != null)
                {
                    session.RawMirror.Close();
                    session.RawMirror = null;
                }
            }
            catch { }

            base.OnSessionClosed(session, reason);
        }

        protected override bool Setup(IRootConfig rootConfig, IServerConfig config)
        {
            m_DBType = "SQLServer";

            return true;
        }

        private async Task server_NewRequestReceived(CustomSession session, StringRequestInfo requestInfo)
        {
            try
            {
                General gen = new General();
                string CheckIn = requestInfo.Body;


                if (requestInfo.Key == "response")
                {
                    insertResponse(requestInfo.Body, session,gen);
                    General.WriteToLogFile(requestInfo.Parameters[1].Substring(0, 15) + ":" + requestInfo.Body, GlobalVariable.m_folderpath, "res.txt");
                    // CheckCommand(session.ServiceId, session);
                    return;
                }

                if (requestInfo.Key == "EPB")
                {
                    General.WriteToLogFile(requestInfo.Body, GlobalVariable.m_folderpath, "ReceivedData\\" + session.IMEI + ".txt");
                    //insertEMRData(requestInfo, session);
                    return;
                }
                string Imei = General.GetImeiNumber(requestInfo.Body);
                //session.Send($"MSG,FE<6906>&");
                //General.WriteToLogFile("command send : MSG,FE<6906>& ", GlobalVariable.m_folderpath, "CommandSend\\" + Imei + ".txt");
                session.IMEI = Imei;
                CheckCommand(session.ServiceId, session,gen);
                string[] RequestArray = requestInfo.Parameters.ToArray();

                General.WriteToLogFile(requestInfo.Body, GlobalVariable.m_folderpath, "ReceivedData\\" + Imei + ".txt");
                
                string sSession = Convert.ToString(session.ServiceId);

                if (Imei != "")
                {
                    session.IMEI = Imei.ToString();

                    if (sSession == null)
                    {
                        string sServiceId = General.GetServiceID(Imei);
                        session.ServiceId = sServiceId;
                        if (sServiceId != "0")
                        {

                            bool ISServiceID = General.CreateSessionID(sServiceId);

                            session.ServiceId = sServiceId.ToString();
                            sSession = sServiceId;
                            General.CheckUpdate(session,gen);
                            string query = @"select tz.utc_offset,u.id,s.veh_reg from tbl_timezone tz inner join tbl_users u on tz.id=u.timezone_id inner join tbl_services s on s.sys_user_id=u.id where s.id=" + session.ServiceId;
                            DataTable dt1 = null;

                            dt1 = General.SelectQuery(query);

                            if (dt1 != null)
                            {
                                if (dt1.Rows.Count > 0)
                                {
                                    session.UserId = Convert.ToInt32(dt1.Rows[0]["id"]);
                                    session.UTCOffset = Convert.ToInt32(dt1.Rows[0]["utc_offset"]);
                                    session.VehicleName = Convert.ToString(dt1.Rows[0]["veh_reg"]);

                                }
                            }

                        }

                    }

                    DataTable dt = new DataTable();


                    string source = requestInfo.Body.ToString();

                    if (source.Contains("ATL") || source.Contains("ASPL")||source.Contains("ASP"))
                    {
                        //sSession = "1";
                        if (sSession != null)
                        {
                            
                            List<CordinateValue> lstVlaue = DecryptResponse.AssignValue(RequestArray);
                            if (lstVlaue[0].Alert=="EA" || lstVlaue[0].Alert == "EU")
                            {
                                await General.detactPanicAlert(sSession, Convert.ToDouble(lstVlaue[0].Latitude), Convert.ToDouble(lstVlaue[0].Longitude), lstVlaue[0].GpsDateTime, lstVlaue[0].Alert, session.ServiceId, lstVlaue[0].GpsDateTime.AddMinutes(330));

                            }
                            InsertTrackingData(lstVlaue, session,gen);
                            //CheckCommandServiceId(session.ServiceId, session);
                            //CheckCommand(session.ServiceId, session);

                        }
                    }
                }
            }
            catch (Exception e)
            {
                General.WriteToLogFile(e.Message , GlobalVariable.m_folderpath, "error.txt");

            }
        }



        
        public void InsertTrackingData(List<CordinateValue> lstvalues, CustomSession session,General gen)
        {

            try
            {
                //  string sSession = oCustomSession.ServiceId;


                for (int i = 0; i < lstvalues.Count; i++)
                {
                    if (lstvalues[i].packet_type == "EA")
                    {
                        try
                        {
                            if (session.IsPanicAlert)
                            {
                                if (lstvalues[i].Latitude == 0.0 || lstvalues[i].Longitude == 0.0)
                                {

                                    continue;

                                }
                                string address = General.GetLocationFromLatLong(lstvalues[i].Latitude, lstvalues[i].Longitude).Result;
                                string insertQuery = @"insert into panicalert_log  (sys_service_id,lastAlertTime,alert_msg,readFlag,gps_latitude,gps_longitude)  values (" + session.ServiceId + ",'" + Convert.ToDateTime(lstvalues[i].GpsDateTime).AddMinutes(330).ToString("yyyy-MM-dd HH:mm:ss") + "','Warning: Panic button has been pressed on vehicle " + session.VehicleName + " is " + address + " on  " + Convert.ToDateTime(lstvalues[i].GpsDateTime).AddMinutes(330).ToString("yyyy-MM-dd HH:mm:ss") + "' ,0,'" + lstvalues[i].Latitude + "','" + lstvalues[i].Longitude + "')";
                                General.DML(insertQuery);

                                if (General.IsAlertTime(Convert.ToDateTime(session.PanicAlert.FromAlertTime.ToString()), Convert.ToDateTime(session.PanicAlert.ToAlertTime.ToString())))
                                {
                                    if (session.LastPanicTime == new DateTime())
                                        session.LastPanicTime = DateTime.Now;

                                    if (!session.IsPanicSent || (DateTime.Now - session.LastPanicTime).TotalMinutes >= 15)
                                    {
                                        session.IsPanicSent = true;
                                        session.LastPanicTime = DateTime.Now;

                                        // string address = General.GetLocationFromLatLong(lstvalues[i].Latitude, lstvalues[i].Longitude);

                                        //General.SendNotification("Warning: Panic button has been pressed on vehicle " + session.VehicleName + " is " + address + " on " + session.LastPanicTime + "", session.UserId);

                                        //if (!string.IsNullOrEmpty(session.PanicAlert.Mobile))
                                        //    General.SendSms("Warning: Panic button has been pressed on vehicle " + session.VehicleName + " is " + address + " on " + session.LastPanicTime + "", session.PanicAlert.Mobile);

                                        //if (!string.IsNullOrEmpty(session.PanicAlert.Mobile1))
                                        //    General.SendSms("Warning: Panic button has been pressed on vehicle " + session.VehicleName + " is " + address + " on " + session.LastPanicTime + "", session.PanicAlert.Mobile1);

                                        //if (!string.IsNullOrEmpty(session.PanicAlert.Mobile2))
                                        //    General.SendSms("Warning: Panic button has been pressed on vehicle " + session.VehicleName + " is " + address + " on " + session.LastPanicTime + "", session.PanicAlert.Mobile2);

                                        List<string> cc = new List<string>();

                                        if (!string.IsNullOrEmpty(session.PanicAlert.Email1))
                                        {
                                            cc.Add(session.PanicAlert.Email1);
                                        }
                                        if (!string.IsNullOrEmpty(session.PanicAlert.Email2))
                                            cc.Add(session.PanicAlert.Email2);

                                        if (!string.IsNullOrEmpty(session.PanicAlert.Email))
                                            General.SendEmail("Panic Alert", "Warning: Panic button has been pressed on vehicle " + session.VehicleName + " at " + address + " on " + session.LastPanicTime + "", session.PanicAlert.Email, cc);


                                    }
                                }
                            }

                        }
                        catch (Exception ex)
                        {
                            //General.WriteToLogFile(ex.Message + Environment.NewLine + requestInfo.Body, GlobalVariable.m_folderpath, "exPanic.txt");
                        }
                    }

                    if (lstvalues[i].packet_type == "EU")
                    {
                        try
                        {
                            if (session.IsPanicAlert)
                            {
                                if (lstvalues[i].Latitude == 0.0 || lstvalues[i].Longitude == 0.0)
                                {

                                    continue;

                                }
                                string is_notification_enable = $@"select tas.id as tbl_alert_setting_id,* from tbl_alert_master  tam
                                                join tbl_alert_setting tas
                                                on tam.id = tas.alert_id
                                                where tas.alert_id = 42 and tas.service_id = {session.ServiceId} and tas.is_active=1 and tas.is_notification = 1";



                                DataTable dt = General.SelectQuery(is_notification_enable);
                                if (dt != null && dt.Rows.Count > 0)
                                {
                                    string address = General.GetLocationFromLatLong(lstvalues[i].Latitude, lstvalues[i].Longitude).Result;
                                    string insertQuery = @"insert into tbl_alert_log  (sys_service_id,alert_setting_id,lastAlertTime,alert_msg,readFlag,gps_latitude,gps_longitude)  values (" + session.ServiceId + "," + dt.Rows[0]["tbl_alert_setting_id"].ToString() + ",'" + Convert.ToDateTime(lstvalues[i].GpsDateTime).AddMinutes(330).ToString("yyyy-MM-dd HH:mm:ss") + "','Dear Customer: Emergency Free Acknowledgement triggered on vehicle " + session.VehicleName + " is " + address + " on  " + Convert.ToDateTime(lstvalues[i].GpsDateTime).AddMinutes(330).ToString("yyyy-MM-dd HH:mm:ss") + "' ,0,'" + lstvalues[i].Latitude + "','" + lstvalues[i].Longitude + "')";
                                    General.DML(insertQuery);


                                    string msg = "Dear Customer, Emergency Free Acknowledgement triggered on vehicle" +
                     session.VehicleName + " at " + address + " on " + Convert.ToDateTime(lstvalues[i].GpsDateTime).AddMinutes(330).ToString("yyyy-MM-dd HH:mm:ss");

                                    // EA/EU push notifications are controlled by General.detactPanicAlert to avoid duplicates.

                                }
                            }


                        }
                        catch (Exception ex)
                        {
                            //General.WriteToLogFile(ex.Message + Environment.NewLine + requestInfo.Body, GlobalVariable.m_folderpath, "exPanic.txt");
                        }
                    }

                    //if (lstvalues[i].packet_type == "TT")
                    //{

                    //    string insertQuery = @"insert into tbl_panicalert_log  (sys_service_id,lastAlertTime,alert_msg,readFlag)  values (" + session.ServiceId + ",'" + Convert.ToDateTime(lstvalues[i].GpsDateTime).AddMinutes(330).ToString("yyyy-MM-dd HH:mm:ss") + "','Dear Customer , A tilt has been observed in your Vehicle " + session.VehicleName + "',0)";
                    //    General.DML(insertQuery);

                    //   // General.SendNotification("Dear Customer , A tilt has been observed in your Vehicle " + session.VehicleName, session.UserId);
                    //}


                    if (lstvalues[i].packet_type == "BD")
                    {
                        try
                        {

                            if (lstvalues[i].Latitude == 0.0 || lstvalues[i].Longitude == 0.0)
                            {

                                continue;

                            }
                            string isnotificationsend = "SELECT top(1) sent_on FROM tbl_alert_log WHERE sys_service_id = " + session.ServiceId + " AND message LIKE '%Dear Customer, Battery disconnection has been detected in your vehicle%' order by sent_on desc;";
                            DataTable issended = General.SelectQuery(isnotificationsend);
                            if (issended.Rows.Count > 0)
                            {
                                DateTime lastAlertTime = Convert.ToDateTime(issended.Rows[0]["sent_on"]);

                                if (DateTime.Now > lastAlertTime.AddMinutes(10))
                                {

                                    string is_notification_enable = $@"select tas.id as tbl_alert_setting_id,* from tbl_alert_master  tam
                                                join tbl_alert_setting tas
                                                on tam.id = tas.alert_id
                                                where tas.alert_id = 37 and tas.service_id = {session.ServiceId} and tas.is_active=1 and tas.is_notification = 1";



                                    DataTable dt = General.SelectQuery(is_notification_enable);

                                    if (dt != null && dt.Rows.Count > 0)
                                    {
                                        string address = General.GetLocationFromLatLong(lstvalues[i].Latitude, lstvalues[i].Longitude).Result;
                                        string insertQuery = @"insert into tbl_alert_log
(sys_service_id,alert_setting_id,sent_on,message,gps_latitude,gps_longitude,is_read,msg_status)
values
(" + session.ServiceId + "," + dt.Rows[0]["tbl_alert_setting_id"].ToString() + ",'"
+ DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
+ "','Dear Customer, Battery disconnection has been detected in your vehicle "
+ session.VehicleName + " is " + address + " on "
+ Convert.ToDateTime(lstvalues[i].GpsDateTime).AddMinutes(330).ToString("yyyy-MM-dd HH:mm:ss")
+ "','" + lstvalues[i].Latitude + "','" + lstvalues[i].Longitude + "',0,'Battery disconnection')";
                                        General.DML(insertQuery);
                                         
                                        string msg = "Dear Customer, battery disconnection has been detected in your vehicle " +
                         session.VehicleName + " at " + address + " on " + Convert.ToDateTime(lstvalues[i].GpsDateTime).AddMinutes(330).ToString("yyyy-MM-dd HH:mm:ss");

                                        General.SendNotification(Convert.ToInt32(session.UserId.ToString()), msg, Convert.ToInt32(dt.Rows[0]["tbl_alert_setting_id"].ToString()), lstvalues[i].Latitude, lstvalues[i].Longitude, msg).GetAwaiter().GetResult();


                                    }
                                }
                            }
                            else
                            {
                                string is_notification_enable = $@"select tas.id as tbl_alert_setting_id,* from tbl_alert_master  tam
                                                join tbl_alert_setting tas
                                                on tam.id = tas.alert_id
                                                where tas.alert_id = 37 and tas.service_id = {session.ServiceId} and tas.is_active=1 and tas.is_notification = 1";



                                DataTable dt = General.SelectQuery(is_notification_enable);

                                if (dt != null && dt.Rows.Count > 0)
                                {
                                    string address = General.GetLocationFromLatLong(lstvalues[i].Latitude, lstvalues[i].Longitude).Result;
                                   string insertQuery = @"insert into tbl_alert_log  
(sys_service_id,alert_setting_id,sent_on,message,gps_latitude,gps_longitude,is_read,msg_status)  
values 
(" + session.ServiceId + "," + dt.Rows[0]["tbl_alert_setting_id"].ToString() + ",'" 
+ DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
+ "','Dear Customer, Battery disconnection has been detected in your vehicle " 
+ session.VehicleName + " is " + address + " on " 
+ Convert.ToDateTime(lstvalues[i].GpsDateTime).AddMinutes(330).ToString("yyyy-MM-dd HH:mm:ss") 
+ "','" + lstvalues[i].Latitude + "','" + lstvalues[i].Longitude + "',0,'Battery disconnection')";
                                    General.DML(insertQuery);


                                    string msg = "Dear Customer, battery disconnection has been detected in your vehicle " +
                     session.VehicleName + " at " + address + " on " + Convert.ToDateTime(lstvalues[i].GpsDateTime).AddMinutes(330).ToString("yyyy-MM-dd HH:mm:ss");

                                    General.SendNotification(Convert.ToInt32(session.UserId.ToString()), msg, Convert.ToInt32(dt.Rows[0]["tbl_alert_setting_id"].ToString()), lstvalues[i].Latitude, lstvalues[i].Longitude, msg).GetAwaiter().GetResult();
                                }
                        }




                        }
                        catch (Exception ex)
                        {
                            //General.WriteToLogFile(ex.Message + Environment.NewLine + requestInfo.Body, GlobalVariable.m_folderpath, "exPanic.txt");
                        }
                    }

                    if (lstvalues[i].packet_type == "BL")
                    {
                        try
                        {
                            
                                if (lstvalues[i].Latitude == 0.0 || lstvalues[i].Longitude == 0.0)
                                {

                                    continue;

                                }
                                string is_notification_enable = $@"select tas.id as tbl_alert_setting_id,* from tbl_alert_master  tam
                                                join tbl_alert_setting tas
                                                on tam.id = tas.alert_id
                                                where tas.alert_id = 38 and tas.service_id = {session.ServiceId} and tas.is_active=1 and tas.is_notification = 1";

                            string isnotificationsend = "SELECT top(1) sent_on FROM tbl_alert_log WHERE sys_service_id = " + session.ServiceId + " AND message LIKE '%Dear Customer, low battery has been detected in your vehicle%' order by sent_on desc;";
                            DataTable issended = General.SelectQuery(isnotificationsend);
                            if (issended.Rows.Count > 0)
                            {
                                DateTime lastAlertTime = Convert.ToDateTime(issended.Rows[0]["sent_on"]);

                                if (DateTime.Now > lastAlertTime.AddMinutes(10))
                                {

                                    DataTable dt = General.SelectQuery(is_notification_enable);

                                    if (dt != null && dt.Rows.Count > 0)
                                    {
                                        string address = General.GetLocationFromLatLong(lstvalues[i].Latitude, lstvalues[i].Longitude).Result;
                                        string insertQuery = @"insert into tbl_alert_log  
(sys_service_id,alert_setting_id,sent_on,message,gps_latitude,gps_longitude,is_read,msg_status)  
values 
(" + session.ServiceId + "," + dt.Rows[0]["tbl_alert_setting_id"].ToString() + ",'"
 + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
 + "','Dear Customer, low battery has been detected in your vehicle "
 + session.VehicleName + " is " + address + " on "
 + Convert.ToDateTime(lstvalues[i].GpsDateTime).AddMinutes(330).ToString("yyyy-MM-dd HH:mm:ss")
 + "','" + lstvalues[i].Latitude + "','" + lstvalues[i].Longitude + "',0,'Battery Low')";
                                        General.DML(insertQuery);

                                        string msg = "Dear Customer, low battery has been detected in your vehicle " +
                         session.VehicleName + " at " + address + " on " + Convert.ToDateTime(lstvalues[i].GpsDateTime).AddMinutes(330).ToString("yyyy-MM-dd HH:mm:ss");

                                        General.SendNotification(Convert.ToInt32(session.UserId.ToString()), msg, Convert.ToInt32(dt.Rows[0]["tbl_alert_setting_id"].ToString()), lstvalues[i].Latitude, lstvalues[i].Longitude, msg).GetAwaiter().GetResult();
                                    }
                                }
                            }
                            else
                            {
                                DataTable dt = General.SelectQuery(is_notification_enable);

                                if (dt != null && dt.Rows.Count > 0)
                                {
                                    string address = General.GetLocationFromLatLong(lstvalues[i].Latitude, lstvalues[i].Longitude).Result;
                                    string insertQuery = @"insert into tbl_alert_log  
(sys_service_id,alert_setting_id,sent_on,message,gps_latitude,gps_longitude,is_read,msg_status)  
values 
(" + session.ServiceId + "," + dt.Rows[0]["tbl_alert_setting_id"].ToString() + ",'"
 + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
 + "','Dear Customer, low battery has been detected in your vehicle "
 + session.VehicleName + " is " + address + " on "
 + Convert.ToDateTime(lstvalues[i].GpsDateTime).AddMinutes(330).ToString("yyyy-MM-dd HH:mm:ss")
 + "','" + lstvalues[i].Latitude + "','" + lstvalues[i].Longitude + "',0,'Battery Low')";
                                    General.DML(insertQuery);

                                    string msg = "Dear Customer, low battery has been detected in your vehicle " +
                     session.VehicleName + " at " + address + " on " + Convert.ToDateTime(lstvalues[i].GpsDateTime).AddMinutes(330).ToString("yyyy-MM-dd HH:mm:ss");

                                    General.SendNotification(Convert.ToInt32(session.UserId.ToString()), msg, Convert.ToInt32(dt.Rows[0]["tbl_alert_setting_id"].ToString()), lstvalues[i].Latitude, lstvalues[i].Longitude, msg).GetAwaiter().GetResult();
                                }
                            }

                        }
                        catch (Exception ex)
                        {
                            //General.WriteToLogFile(ex.Message + Environment.NewLine + requestInfo.Body, GlobalVariable.m_folderpath, "exPanic.txt");
                        }
                    }

                    if (lstvalues[i].packet_type == "BH")
                    {
                        try
                        {
                            
                                if (lstvalues[i].Latitude == 0.0 || lstvalues[i].Longitude == 0.0)
                                {

                                    continue;

                                }

                                string is_notification_enable = $@"select tas.id as tbl_alert_setting_id,* from tbl_alert_master  tam
                                                join tbl_alert_setting tas
                                                on tam.id = tas.alert_id
                                                where tas.alert_id = 39 and tas.service_id = {session.ServiceId} and tas.is_active=1 and tas.is_notification = 1";


                            string isnotificationsend = "SELECT top(1) sent_on FROM tbl_alert_log WHERE sys_service_id = " + session.ServiceId + " AND message LIKE '%Dear Customer, your device internal battery has been charged again and is now above 15%.%' order by sent_on desc;";
                            DataTable issended = General.SelectQuery(isnotificationsend);
                            if (issended.Rows.Count > 0)
                            {
                                DateTime lastAlertTime = Convert.ToDateTime(issended.Rows[0]["sent_on"]);

                                if (DateTime.Now > lastAlertTime.AddMinutes(10))
                                {

                                    DataTable dt = General.SelectQuery(is_notification_enable);

                                    if (dt != null && dt.Rows.Count > 0)
                                    {
                                        string address = General.GetLocationFromLatLong(lstvalues[i].Latitude, lstvalues[i].Longitude).Result;
                                        string insertQuery = @"insert into tbl_alert_log  
(sys_service_id,alert_setting_id,sent_on,message,gps_latitude,gps_longitude,is_read,msg_status)  
values 
(" + session.ServiceId + "," + dt.Rows[0]["tbl_alert_setting_id"].ToString() + ",'"
+ DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
+ "','Dear Customer, your device internal battery has been charged again and is now above 15%. "
+ session.VehicleName + " is " + address + " on "
+ Convert.ToDateTime(lstvalues[i].GpsDateTime).AddMinutes(330).ToString("yyyy-MM-dd HH:mm:ss")
+ "','" + lstvalues[i].Latitude + "','" + lstvalues[i].Longitude + "',0,'Battery Charged')";
                                        General.DML(insertQuery);


                                        string msg = "Dear Customer,your device internal battery has been charged again and is now above 15% " +
                         session.VehicleName + " at " + address + " on " + Convert.ToDateTime(lstvalues[i].GpsDateTime).AddMinutes(330).ToString("yyyy-MM-dd HH:mm:ss");

                                        General.SendNotification(Convert.ToInt32(session.UserId.ToString()), msg, Convert.ToInt32(dt.Rows[0]["tbl_alert_setting_id"].ToString()), lstvalues[i].Latitude, lstvalues[i].Longitude, msg).GetAwaiter().GetResult();
                                    }
                                }

                            }
                            else
                            {
                                DataTable dt = General.SelectQuery(is_notification_enable);

                                if (dt != null && dt.Rows.Count > 0)
                                {
                                    string address = General.GetLocationFromLatLong(lstvalues[i].Latitude, lstvalues[i].Longitude).Result;
                                    string insertQuery = @"insert into tbl_alert_log  
(sys_service_id,alert_setting_id,sent_on,message,gps_latitude,gps_longitude,is_read,msg_status)  
values 
(" + session.ServiceId + "," + dt.Rows[0]["tbl_alert_setting_id"].ToString() + ",'"
+ DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
+ "','Dear Customer, your device internal battery has been charged again and is now above 15%. "
+ session.VehicleName + " is " + address + " on "
+ Convert.ToDateTime(lstvalues[i].GpsDateTime).AddMinutes(330).ToString("yyyy-MM-dd HH:mm:ss")
+ "','" + lstvalues[i].Latitude + "','" + lstvalues[i].Longitude + "',0,'Battery Charged')";
                                    General.DML(insertQuery);


                                    string msg = "Dear Customer,your device internal battery has been charged again and is now above 15% " +
                     session.VehicleName + " at " + address + " on " + Convert.ToDateTime(lstvalues[i].GpsDateTime).AddMinutes(330).ToString("yyyy-MM-dd HH:mm:ss");

                                    General.SendNotification(Convert.ToInt32(session.UserId.ToString()), msg, Convert.ToInt32(dt.Rows[0]["tbl_alert_setting_id"].ToString()), lstvalues[i].Latitude, lstvalues[i].Longitude, msg).GetAwaiter().GetResult();
                                }
                            }



                        }
                        catch (Exception ex)
                        {
                            //General.WriteToLogFile(ex.Message + Environment.NewLine + requestInfo.Body, GlobalVariable.m_folderpath, "exPanic.txt");
                        }
                    }

                    if (lstvalues[i].packet_type == "BR")
                    {
                        try
                        {
                            
                                if (lstvalues[i].Latitude == 0.0 || lstvalues[i].Longitude == 0.0)
                                {

                                    continue;

                                }

                                string is_notification_enable = $@"select tas.id as tbl_alert_setting_id,* from tbl_alert_master  tam
                                                join tbl_alert_setting tas
                                                on tam.id = tas.alert_id
                                                where tas.alert_id = 40 and tas.service_id = {session.ServiceId} and tas.is_active=1 and tas.is_notification = 1";


                            string isnotificationsend = "SELECT top(1) sent_on FROM tbl_alert_log WHERE sys_service_id = " + session.ServiceId + " AND message LIKE '%Dear Customer, battery has been reconnected in your vehicle..%' order by sent_on desc;";
                            DataTable issended = General.SelectQuery(isnotificationsend);
                            if (issended.Rows.Count > 0)
                            {
                                DateTime lastAlertTime = Convert.ToDateTime(issended.Rows[0]["sent_on"]);

                                if (DateTime.Now > lastAlertTime.AddMinutes(10))
                                {

                                    DataTable dt = General.SelectQuery(is_notification_enable);



                                    if (dt != null)
                                    {
                                        string address = General.GetLocationFromLatLong(lstvalues[i].Latitude, lstvalues[i].Longitude).Result;
                                        string insertQuery = @"insert into tbl_alert_log  
(sys_service_id,alert_setting_id,sent_on,message,gps_latitude,gps_longitude,is_read,msg_status)  
values 
(" + session.ServiceId + "," + dt.Rows[0]["tbl_alert_setting_id"].ToString() + ",'"
+ DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
+ "','Dear Customer, battery has been reconnected in your vehicle. "
+ session.VehicleName + " is " + address + " on "
+ Convert.ToDateTime(lstvalues[i].GpsDateTime).AddMinutes(330).ToString("yyyy-MM-dd HH:mm:ss")
+ "','" + lstvalues[i].Latitude + "','" + lstvalues[i].Longitude + "',0,'battery reconnected')";
                                        General.DML(insertQuery);

                                        string msg = "Dear Customer, battery has been reconnected in your vehicle " +
                           session.VehicleName + " at " + address + " on " + Convert.ToDateTime(lstvalues[i].GpsDateTime).AddMinutes(330).ToString("yyyy-MM-dd HH:mm:ss");

                                        General.SendNotification(Convert.ToInt32(session.UserId.ToString()), msg, Convert.ToInt32(dt.Rows[0]["tbl_alert_setting_id"].ToString()), lstvalues[i].Latitude, lstvalues[i].Longitude, msg).GetAwaiter().GetResult();
                                    }
                                }


                            }
                            else
                            {
                                DataTable dt = General.SelectQuery(is_notification_enable);



                                if (dt != null)
                                {
                                    string address = General.GetLocationFromLatLong(lstvalues[i].Latitude, lstvalues[i].Longitude).Result;
                                    string insertQuery = @"insert into tbl_alert_log  
(sys_service_id,alert_setting_id,sent_on,message,gps_latitude,gps_longitude,is_read,msg_status)  
values 
(" + session.ServiceId + "," + dt.Rows[0]["tbl_alert_setting_id"].ToString() + ",'"
+ DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
+ "','Dear Customer, battery has been reconnected in your vehicle. "
+ session.VehicleName + " is " + address + " on "
+ Convert.ToDateTime(lstvalues[i].GpsDateTime).AddMinutes(330).ToString("yyyy-MM-dd HH:mm:ss")
+ "','" + lstvalues[i].Latitude + "','" + lstvalues[i].Longitude + "',0,'battery reconnected')";
                                    General.DML(insertQuery);

                                    string msg = "Dear Customer, battery has been reconnected in your vehicle " +
                       session.VehicleName + " at " + address + " on " + Convert.ToDateTime(lstvalues[i].GpsDateTime).AddMinutes(330).ToString("yyyy-MM-dd HH:mm:ss");

                                    General.SendNotification(Convert.ToInt32(session.UserId.ToString()), msg, Convert.ToInt32(dt.Rows[0]["tbl_alert_setting_id"].ToString()), lstvalues[i].Latitude, lstvalues[i].Longitude, msg).GetAwaiter().GetResult();
                                }
                            }

                        }
                        catch (Exception ex)
                        {
                            //General.WriteToLogFile(ex.Message + Environment.NewLine + requestInfo.Body, GlobalVariable.m_folderpath, "exPanic.txt");
                        }
                    }


                   
                    if (lstvalues[i].packet_type == "TA")
                    {
                        try
                        {
                            
                                if (lstvalues[i].Latitude == 0.0 || lstvalues[i].Longitude == 0.0)
                                {

                                    continue;

                                }

                                string is_notification_enable = $@"select tas.id as tbl_alert_setting_id,* from tbl_alert_master  tam
                                                join tbl_alert_setting tas
                                                on tam.id = tas.alert_id
                                                where tas.alert_id = 41 and tas.service_id = {session.ServiceId} and tas.is_active=1 and tas.is_notification = 1";

                            string isnotificationsend = "SELECT top(1) sent_on FROM tbl_alert_log WHERE sys_service_id = " + session.ServiceId + " AND message LIKE '%Dear Customer, tamper alert detected. GPS cover has been opened in your vehicle.%' order by sent_on desc;";
                            DataTable issended = General.SelectQuery(isnotificationsend);
                            if (issended.Rows.Count > 0)
                            {
                                DateTime lastAlertTime = Convert.ToDateTime(issended.Rows[0]["sent_on"]);

                                if (DateTime.Now > lastAlertTime.AddMinutes(10))
                                {

                                    DataTable dt = General.SelectQuery(is_notification_enable);
                                    if (dt != null && dt.Rows.Count > 0)
                                    {
                                        string address = General.GetLocationFromLatLong(lstvalues[i].Latitude, lstvalues[i].Longitude).Result;
                                        string insertQuery = @"insert into tbl_alert_log  
(sys_service_id,alert_setting_id,sent_on,message,gps_latitude,gps_longitude,is_read,msg_status)  
values 
(" + session.ServiceId + "," + dt.Rows[0]["tbl_alert_setting_id"].ToString() + ",'"
                                        + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                                        + "','Dear Customer, tamper alert detected. GPS cover has been opened in your vehicle. "
                                        + session.VehicleName + " is " + address + " on "
                                        + Convert.ToDateTime(lstvalues[i].GpsDateTime).AddMinutes(330).ToString("yyyy-MM-dd HH:mm:ss")
                                        + "','" + lstvalues[i].Latitude + "','" + lstvalues[i].Longitude + "',0,'tamper alert')";
                                        General.DML(insertQuery);

                                        string msg = "Dear Customer, tamper alert detected. GPS cover has been opened in your vehicle " +
                          session.VehicleName + " at " + address + " on " + Convert.ToDateTime(lstvalues[i].GpsDateTime).AddMinutes(330).ToString("yyyy-MM-dd HH:mm:ss");

                                        General.SendNotification(Convert.ToInt32(session.UserId.ToString()), msg, Convert.ToInt32(dt.Rows[0]["tbl_alert_setting_id"].ToString()), lstvalues[i].Latitude, lstvalues[i].Longitude, msg).GetAwaiter().GetResult();
                                    }
                                }

                            }
                            else
                            {
                                
                                    DataTable dt = General.SelectQuery(is_notification_enable);
                                    if (dt != null && dt.Rows.Count > 0)
                                    {
                                        string address = General.GetLocationFromLatLong(lstvalues[i].Latitude, lstvalues[i].Longitude).Result;
                                        string insertQuery = @"insert into tbl_alert_log  
(sys_service_id,alert_setting_id,sent_on,message,gps_latitude,gps_longitude,is_read,msg_status)  
values 
(" + session.ServiceId + "," + dt.Rows[0]["tbl_alert_setting_id"].ToString() + ",'"
+ DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
+ "','Dear Customer, tamper alert detected. GPS cover has been opened in your vehicle. "
+ session.VehicleName + " is " + address + " on "
+ Convert.ToDateTime(lstvalues[i].GpsDateTime).AddMinutes(330).ToString("yyyy-MM-dd HH:mm:ss")
+ "','" + lstvalues[i].Latitude + "','" + lstvalues[i].Longitude + "',0,'tamper alert')";
                                        General.DML(insertQuery);

                                        string msg = "Dear Customer, tamper alert detected. GPS cover has been opened in your vehicle " +
                          session.VehicleName + " at " + address + " on " + Convert.ToDateTime(lstvalues[i].GpsDateTime).AddMinutes(330).ToString("yyyy-MM-dd HH:mm:ss");

                                        General.SendNotification(Convert.ToInt32(session.UserId.ToString()), msg, Convert.ToInt32(dt.Rows[0]["tbl_alert_setting_id"].ToString()), lstvalues[i].Latitude, lstvalues[i].Longitude, msg).GetAwaiter().GetResult();
                                    }
                                }
                            

                        }
                        catch (Exception ex)
                        {
                            //General.WriteToLogFile(ex.Message + Environment.NewLine + requestInfo.Body, GlobalVariable.m_folderpath, "exPanic.txt");
                        }
                    }


                    if (lstvalues[i].packet_type == "DT")
                    {
                        try
                        {
                           
                                if (lstvalues[i].Latitude == 0.0 || lstvalues[i].Longitude == 0.0)
                                {

                                    continue;

                                }

                                string is_notification_enable = $@"select tas.id as tbl_alert_setting_id,* from tbl_alert_master  tam
                                                join tbl_alert_setting tas
                                                on tam.id = tas.alert_id
                                                where tas.alert_id = 43 and tas.service_id = {session.ServiceId} and tas.is_active=1 and tas.is_notification = 1";

                            string isnotificationsend = "SELECT top(1) sent_on FROM tbl_alert_log WHERE sys_service_id = " + session.ServiceId + " AND message LIKE '%Dear Customer, panic wire cut has been detected in your vehicle.%' order by sent_on desc;";
                            DataTable issended = General.SelectQuery(isnotificationsend);
                            if (issended.Rows.Count > 0)
                            {
                                DateTime lastAlertTime = Convert.ToDateTime(issended.Rows[0]["sent_on"]);

                                if (DateTime.Now > lastAlertTime.AddMinutes(10))
                                {


                                    DataTable dt = General.SelectQuery(is_notification_enable);
                                    if (dt != null)
                                    {
                                        string address = General.GetLocationFromLatLong(lstvalues[i].Latitude, lstvalues[i].Longitude).Result;
                                        string insertQuery = @"insert into tbl_alert_log  
(sys_service_id,alert_setting_id,sent_on,message,gps_latitude,gps_longitude,is_read,msg_status)  
values 
(" + session.ServiceId + "," + dt.Rows[0]["tbl_alert_setting_id"].ToString() + ",'"
+ DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
+ "','Dear Customer, panic wire cut has been detected in your vehicle. "
+ session.VehicleName + " is " + address + " on "
+ Convert.ToDateTime(lstvalues[i].GpsDateTime).AddMinutes(330).ToString("yyyy-MM-dd HH:mm:ss")
+ "','" + lstvalues[i].Latitude + "','" + lstvalues[i].Longitude + "',0,'panic wire cut')";
                                        General.DML(insertQuery);

                                        string msg = "Dear Customer, panic wire cut has been detected in your vehicle " +
                           session.VehicleName + " at " + address + " on " + Convert.ToDateTime(lstvalues[i].GpsDateTime).AddMinutes(330).ToString("yyyy-MM-dd HH:mm:ss");

                                        General.SendNotification(Convert.ToInt32(session.UserId.ToString()), msg, Convert.ToInt32(dt.Rows[0]["tbl_alert_setting_id"].ToString()), lstvalues[i].Latitude, lstvalues[i].Longitude, msg).GetAwaiter().GetResult();
                                    }
                                }


                            }
                            else
                            {
                                DataTable dt = General.SelectQuery(is_notification_enable);
                                if (dt != null)
                                {
                                    string address = General.GetLocationFromLatLong(lstvalues[i].Latitude, lstvalues[i].Longitude).Result;
                                    string insertQuery = @"insert into tbl_alert_log  
(sys_service_id,alert_setting_id,sent_on,message,gps_latitude,gps_longitude,is_read,msg_status)  
values 
(" + session.ServiceId + "," + dt.Rows[0]["tbl_alert_setting_id"].ToString() + ",'"
+ DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
+ "','Dear Customer, panic wire cut has been detected in your vehicle. "
+ session.VehicleName + " is " + address + " on "
+ Convert.ToDateTime(lstvalues[i].GpsDateTime).AddMinutes(330).ToString("yyyy-MM-dd HH:mm:ss")
+ "','" + lstvalues[i].Latitude + "','" + lstvalues[i].Longitude + "',0,'panic wire cut')";
                                    General.DML(insertQuery);

                                    string msg = "Dear Customer, panic wire cut has been detected in your vehicle " +
                       session.VehicleName + " at " + address + " on " + Convert.ToDateTime(lstvalues[i].GpsDateTime).AddMinutes(330).ToString("yyyy-MM-dd HH:mm:ss");

                                    General.SendNotification(Convert.ToInt32(session.UserId.ToString()), msg, Convert.ToInt32(dt.Rows[0]["tbl_alert_setting_id"].ToString()), lstvalues[i].Latitude, lstvalues[i].Longitude, msg).GetAwaiter().GetResult();
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            //General.WriteToLogFile(ex.Message + Environment.NewLine + requestInfo.Body, GlobalVariable.m_folderpath, "exPanic.txt");
                        }
                    }



                    if (lstvalues[i].packet_type == "OS")
                    {
                        try
                        {
                              if (lstvalues[i].Latitude == 0.0 || lstvalues[i].Longitude == 0.0)
                                {

                                    continue;

                                }
                                 string is_notification_enable = $@"select tas.id as tbl_alert_setting_id,* from tbl_alert_master  tam
                                                join tbl_alert_setting tas
                                                on tam.id = tas.alert_id
                                                where tas.alert_id = 7 and tas.service_id = {session.ServiceId} and tas.is_active=1 and tas.is_notification = 1";

                            string isnotificationsend = "SELECT top(1) sent_on FROM tbl_alert_log WHERE sys_service_id = " + session.ServiceId + " AND message LIKE '%Dear Customer, over speed has been detected in your vehicle%' order by sent_on desc;";
                            DataTable issended = General.SelectQuery(isnotificationsend);
                            if (issended.Rows.Count > 0)
                            {
                                DateTime lastAlertTime = Convert.ToDateTime(issended.Rows[0]["sent_on"]);

                                if (!(DateTime.Now > lastAlertTime.AddMinutes(10)))
                                {


                                    DataTable dt = General.SelectQuery(is_notification_enable);
                                    if (dt != null && dt.Rows.Count > 0)
                                    {
                                        double speedKnot = 0;

                                        double.TryParse(lstvalues[i].Speed_knot.ToString(), out speedKnot);

                                        double speedKmh = Math.Round(speedKnot * 1.852, 2);


                                        string address = General.GetLocationFromLatLong(lstvalues[i].Latitude, lstvalues[i].Longitude).Result;
                                        string insertQuery = @"insert into tbl_alert_log ( sys_service_id, alert_setting_id, sent_on, message, gps_latitude, gps_longitude, is_read, msg_status ) values ( " + session.ServiceId + ", " + dt.Rows[0]["tbl_alert_setting_id"].ToString() + ", '" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + @"', 'Dear Customer, over speed has been detected in your vehicle " + session.VehicleName + " at " + address + " with speed " + speedKmh + " km/h on " + Convert.ToDateTime(lstvalues[i].GpsDateTime).AddMinutes(330).ToString("yyyy-MM-dd HH:mm:ss") + @"', '" + lstvalues[i].Latitude + @"', '" + lstvalues[i].Longitude + @"', 0, 'Over Speed' )";
                                        General.DML(insertQuery);

                                       
                                        string msg = "Dear Customer, over speed has been detected in your vehicle "
                                        + session.VehicleName
                                        + " at "
                                        + address
                                        + " with speed "
                                        + speedKmh
                                        + " km/h on "
                                        + Convert.ToDateTime(lstvalues[i].GpsDateTime)
                                            .AddMinutes(330)
                                            .ToString("yyyy-MM-dd HH:mm:ss");



                                        General.SendNotification(Convert.ToInt32(session.UserId.ToString()), msg, Convert.ToInt32(dt.Rows[0]["tbl_alert_setting_id"].ToString()), lstvalues[i].Latitude, lstvalues[i].Longitude, msg).GetAwaiter().GetResult();
                                    }
                                }


                            }
                            else
                            {
                                DataTable dt = General.SelectQuery(is_notification_enable);
                                if (dt != null && dt.Rows.Count > 0)
                                {
                                    double speedKnot = 0;
                                    double.TryParse(lstvalues[i].Speed_knot.ToString(), out speedKnot);

                                    double speedKmh = Math.Round(speedKnot * 1.852, 2);

                                    string address = General.GetLocationFromLatLong(lstvalues[i].Latitude, lstvalues[i].Longitude).Result;
                                    string insertQuery = @"insert into tbl_alert_log ( sys_service_id, alert_setting_id, sent_on, message, gps_latitude, gps_longitude, is_read, msg_status ) values ( " + session.ServiceId + ", " + dt.Rows[0]["tbl_alert_setting_id"].ToString() + ", '" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + @"', 'Dear Customer, over speed has been detected in your vehicle " + session.VehicleName + " at " + address + " with speed " + speedKmh + " km/h on " + Convert.ToDateTime(lstvalues[i].GpsDateTime).AddMinutes(330).ToString("yyyy-MM-dd HH:mm:ss") + @"', '" + lstvalues[i].Latitude + @"', '" + lstvalues[i].Longitude + @"', 0, 'Over Speed' )";
                                    General.DML(insertQuery);


                                    string msg = "Dear Customer, over speed has been detected in your vehicle "
                                    + session.VehicleName
                                    + " at "
                                    + address
                                    + " with speed "
                                    + speedKmh
                                    + " km/h on "
                                    + Convert.ToDateTime(lstvalues[i].GpsDateTime)
                                        .AddMinutes(330)
                                        .ToString("yyyy-MM-dd HH:mm:ss");

                                    General.SendNotification(Convert.ToInt32(session.UserId.ToString()), msg, Convert.ToInt32(dt.Rows[0]["tbl_alert_setting_id"].ToString()), lstvalues[i].Latitude, lstvalues[i].Longitude, msg).GetAwaiter().GetResult();
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            //General.WriteToLogFile(ex.Message + Environment.NewLine + requestInfo.Body, GlobalVariable.m_folderpath, "exPanic.txt");
                        }
                    }
                   
                    if (lstvalues[i].packet_type == "IN")
                    {
                        try
                        {
                            if (lstvalues[i].Latitude == 0.0 || lstvalues[i].Longitude == 0.0)
                            {
                                continue;
                            }

                            string is_notification_enable = $@"
        select tas.id as tbl_alert_setting_id,* 
        from tbl_alert_master tam
        join tbl_alert_setting tas
        on tam.id = tas.alert_id
        where tas.alert_id = 3
        and tas.service_id = {session.ServiceId}
        and tas.is_active = 1
        and tas.is_notification = 1";

                            DataTable dt = General.SelectQuery(is_notification_enable);

                            if (dt != null && dt.Rows.Count > 0)
                            {
                                string address =
                                    General.GetLocationFromLatLong(
                                        lstvalues[i].Latitude,
                                        lstvalues[i].Longitude
                                    ).Result;

                                string alertTime =
                                    Convert.ToDateTime(lstvalues[i].GpsDateTime)
                                    .AddMinutes(330)
                                    .ToString("yyyy-MM-dd HH:mm:ss");

                                string msg =
                                    "Dear Customer, ignition on has been detected in your vehicle "
                                    + session.VehicleName
                                    + " at "
                                    + address
                                    + " on "
                                    + alertTime;

                                string insertQuery = @"
            insert into tbl_alert_log
            (
                sys_service_id,
                alert_setting_id,
                sent_on,
                message,
                gps_latitude,
                gps_longitude,
                is_read,
                msg_status
            )
            values
            (
                " + session.ServiceId + @",
                " + dt.Rows[0]["tbl_alert_setting_id"].ToString() + @",
                '" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + @"',
                '" + msg.Replace("'", "''") + @"',
                '" + lstvalues[i].Latitude + @"',
                '" + lstvalues[i].Longitude + @"',
                0,
                'Ignition ON'
            )";

                                General.DML(insertQuery);

                                General.SendNotification(
                                    Convert.ToInt32(session.UserId.ToString()),
                                    msg,
                                    Convert.ToInt32(dt.Rows[0]["tbl_alert_setting_id"].ToString()),
                                    lstvalues[i].Latitude,
                                    lstvalues[i].Longitude,
                                    msg
                                ).GetAwaiter().GetResult();
                            }
                        }
                        catch (Exception ex)
                        {
                            //General.WriteToLogFile(
                            //    ex.Message,
                            //    GlobalVariable.m_folderpath,
                            //    "IgnitionON.txt"
                            //);
                        }
                    }

               
                    if (lstvalues[i].packet_type == "IF")
                    {
                        try
                        {
                            if (lstvalues[i].Latitude == 0.0 || lstvalues[i].Longitude == 0.0)
                            {
                                continue;
                            }

                            string is_notification_enable = $@"
        select tas.id as tbl_alert_setting_id,* 
        from tbl_alert_master tam
        join tbl_alert_setting tas
        on tam.id = tas.alert_id
        where tas.alert_id = 3
        and tas.service_id = {session.ServiceId}
        and tas.is_active = 1
        and tas.is_notification = 1";

                            DataTable dt = General.SelectQuery(is_notification_enable);

                            if (dt != null && dt.Rows.Count > 0)
                            {
                                string address =
                                    General.GetLocationFromLatLong(
                                        lstvalues[i].Latitude,
                                        lstvalues[i].Longitude
                                    ).Result;

                                string alertTime =
                                    Convert.ToDateTime(lstvalues[i].GpsDateTime)
                                    .AddMinutes(330)
                                    .ToString("yyyy-MM-dd HH:mm:ss");

                                string msg =
                                    "Dear Customer, ignition off has been detected in your vehicle "
                                    + session.VehicleName
                                    + " at "
                                    + address
                                    + " on "
                                    + alertTime;

                                string insertQuery = @"
            insert into tbl_alert_log
            (
                sys_service_id,
                alert_setting_id,
                sent_on,
                message,
                gps_latitude,
                gps_longitude,
                is_read,
                msg_status
            )
            values
            (
                " + session.ServiceId + @",
                " + dt.Rows[0]["tbl_alert_setting_id"].ToString() + @",
                '" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + @"',
                '" + msg.Replace("'", "''") + @"',
                '" + lstvalues[i].Latitude + @"',
                '" + lstvalues[i].Longitude + @"',
                0,
                'Ignition OFF'
            )";

                                General.DML(insertQuery);

                                General.SendNotification(
                                    Convert.ToInt32(session.UserId.ToString()),
                                    msg,
                                    Convert.ToInt32(dt.Rows[0]["tbl_alert_setting_id"].ToString()),
                                    lstvalues[i].Latitude,
                                    lstvalues[i].Longitude,
                                    msg
                                ).GetAwaiter().GetResult();
                            }
                        }
                        catch (Exception ex)
                        {
                            //General.WriteToLogFile(
                            //    ex.Message,
                            //    GlobalVariable.m_folderpath,
                            //    "IgnitionOFF.txt"
                            //);
                        }
                    }

                  
                    if (lstvalues[i].packet_type == "RT")
                    {
                        try
                        {
                            if (lstvalues[i].Latitude == 0.0 || lstvalues[i].Longitude == 0.0)
                            {
                                continue;
                            }
                            string is_notification_enable = $@"
        select tas.id as tbl_alert_setting_id,* 
        from tbl_alert_master tam
        join tbl_alert_setting tas
        on tam.id = tas.alert_id
        where tas.alert_id = 18
        and tas.service_id = {session.ServiceId}
        and tas.is_active = 1
        and tas.is_notification = 1";
                            DataTable dt = General.SelectQuery(is_notification_enable);

                            if (dt != null && dt.Rows.Count > 0)
                            {

                                string isnotificationsend = @"
        SELECT TOP(1) sent_on 
        FROM tbl_alert_log 
        WHERE sys_service_id = " + session.ServiceId + @"
        AND message LIKE '%Rash turning has been detected%'
        ORDER BY sent_on DESC";

                                DataTable issended = General.SelectQuery(isnotificationsend);

                                bool canSend = false;

                                if (issended.Rows.Count > 0)
                                {
                                    DateTime lastAlertTime =
                                        Convert.ToDateTime(issended.Rows[0]["sent_on"]);

                                    if (DateTime.Now >= lastAlertTime.AddMinutes(10))
                                    {
                                        canSend = true;
                                    }
                                }
                                else
                                {
                                    canSend = true;
                                }

                                if (canSend)
                                {
                                    string address =
                                        General.GetLocationFromLatLong(
                                            lstvalues[i].Latitude,
                                            lstvalues[i].Longitude
                                        ).Result;

                                    string alertTime =
                                        Convert.ToDateTime(lstvalues[i].GpsDateTime)
                                        .AddMinutes(330)
                                        .ToString("yyyy-MM-dd HH:mm:ss");

                                    string msg =
                                        "Dear Customer, Rash turning has been detected in your vehicle "
                                        + session.VehicleName
                                        + " at "
                                        + address
                                        + " on "
                                        + alertTime;

                                
string insertQuery = @"
insert into tbl_alert_log
(
    sys_service_id,
    sent_on,
    message,
    gps_latitude,
    gps_longitude,
    is_read,
    msg_status,
    alert_setting_id
)
values
(
    " + session.ServiceId + @",
    '" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + @"',
    '" + msg.Replace("'", "''") + @"',
    '" + lstvalues[i].Latitude + @"',
    '" + lstvalues[i].Longitude + @"',
    0,
    'Rash Turning',
    " + dt.Rows[0]["tbl_alert_setting_id"].ToString() + @"
)";



                                    General.DML(insertQuery);

                                    General.SendNotification(
                                        Convert.ToInt32(session.UserId.ToString()),
                                        msg,
                                        46,
                                        lstvalues[i].Latitude,
                                        lstvalues[i].Longitude,
                                        msg
                                    ).GetAwaiter().GetResult();
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            //General.WriteToLogFile(
                            //    ex.Message,
                            //    GlobalVariable.m_folderpath,
                            //    "RashTurn.txt"
                            //);
                        }
                    }


                    if (lstvalues[i].packet_type == "HA")
                    {
                        try
                        {
                            if (lstvalues[i].Latitude == 0.0 || lstvalues[i].Longitude == 0.0)
                            {
                                continue;
                            }
                            string is_notification_enable = $@"
        select tas.id as tbl_alert_setting_id,* 
        from tbl_alert_master tam
        join tbl_alert_setting tas
        on tam.id = tas.alert_id
        where tas.alert_id = 18
        and tas.service_id = {session.ServiceId}
        and tas.is_active = 1
        and tas.is_notification = 1";
                            DataTable dt = General.SelectQuery(is_notification_enable);

                            if (dt != null && dt.Rows.Count > 0)
                            {

                                string isnotificationsend = @"
SELECT TOP(1) sent_on 
FROM tbl_alert_log 
WHERE sys_service_id = " + session.ServiceId + @"
AND message LIKE '%Harsh acceleration has been detected%'
ORDER BY sent_on DESC";

                                DataTable issended = General.SelectQuery(isnotificationsend);

                                bool canSend = false;

                                if (issended.Rows.Count > 0)
                                {
                                    DateTime lastAlertTime =
                                        Convert.ToDateTime(issended.Rows[0]["sent_on"]);

                                    if (DateTime.Now >= lastAlertTime.AddMinutes(10))
                                    {
                                        canSend = true;
                                    }
                                }
                                else
                                {
                                    canSend = true;
                                }

                                if (canSend)
                                {
                                    string address =
                                        General.GetLocationFromLatLong(
                                            lstvalues[i].Latitude,
                                            lstvalues[i].Longitude
                                        ).Result;

                                    string alertTime =
                                        Convert.ToDateTime(lstvalues[i].GpsDateTime)
                                        .AddMinutes(330)
                                        .ToString("yyyy-MM-dd HH:mm:ss");

                                    string msg =
                                        "Dear Customer, Harsh acceleration has been detected in your vehicle "
                                        + session.VehicleName
                                        + " at "
                                        + address
                                        + " on "
                                        + alertTime;

                                 
string insertQuery = @"
insert into tbl_alert_log
(
    sys_service_id,
    sent_on,
    message,
    gps_latitude,
    gps_longitude,
    is_read,
    msg_status,
    alert_setting_id
)
values
(
    " + session.ServiceId + @",
    '" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + @"',
    '" + msg.Replace("'", "''") + @"',
    '" + lstvalues[i].Latitude + @"',
    '" + lstvalues[i].Longitude + @"',
    0,
    'Harsh acceleration',
    " + dt.Rows[0]["tbl_alert_setting_id"].ToString() + @"
)";



                                    General.DML(insertQuery);

                                    General.SendNotification(
                                        Convert.ToInt32(session.UserId.ToString()),
                                        msg,
                                        47,
                                        lstvalues[i].Latitude,
                                        lstvalues[i].Longitude,
                                        msg
                                    ).GetAwaiter().GetResult();
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            //General.WriteToLogFile(
                            //    ex.Message,
                            //    GlobalVariable.m_folderpath,
                            //    "HarshAcceleration.txt"
                            //);
                        }
                    }

                    if (lstvalues[i].packet_type == "HB")
                    {
                        try
                        {
                            if (lstvalues[i].Latitude == 0.0 || lstvalues[i].Longitude == 0.0)
                            {
                                continue;
                            }

                            string is_notification_enable = $@"
        select tas.id as tbl_alert_setting_id,* 
        from tbl_alert_master tam
        join tbl_alert_setting tas
        on tam.id = tas.alert_id
        where tas.alert_id = 18
        and tas.service_id = {session.ServiceId}
        and tas.is_active = 1
        and tas.is_notification = 1";
                            DataTable dt = General.SelectQuery(is_notification_enable);

                            if (dt != null && dt.Rows.Count > 0)
                            {
                                string isnotificationsend = @"
SELECT TOP(1) sent_on 
FROM tbl_alert_log 
WHERE sys_service_id = " + session.ServiceId + @"
AND message LIKE '%Harsh braking has been detected%'
ORDER BY sent_on DESC";

                                DataTable issended = General.SelectQuery(isnotificationsend);

                                bool canSend = false;

                                if (issended.Rows.Count > 0)
                                {
                                    DateTime lastAlertTime =
                                        Convert.ToDateTime(issended.Rows[0]["sent_on"]);

                                    if (DateTime.Now >= lastAlertTime.AddMinutes(10))
                                    {
                                        canSend = true;
                                    }
                                }
                                else
                                {
                                    canSend = true;
                                }

                                if (canSend)
                                {
                                    string address =
                                        General.GetLocationFromLatLong(
                                            lstvalues[i].Latitude,
                                            lstvalues[i].Longitude
                                        ).Result;

                                    string alertTime =
                                        Convert.ToDateTime(lstvalues[i].GpsDateTime)
                                        .AddMinutes(330)
                                        .ToString("yyyy-MM-dd HH:mm:ss");

                                    string msg =
                                        "Dear Customer, Harsh braking has been detected in your vehicle "
                                        + session.VehicleName
                                        + " at "
                                        + address
                                        + " on "
                                        + alertTime;

                                   
                                
string insertQuery = @"
insert into tbl_alert_log
(
    sys_service_id,
    sent_on,
    message,
    gps_latitude,
    gps_longitude,
    is_read,
    msg_status,
    alert_setting_id
)
values
(
    " + session.ServiceId + @",
    '" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + @"',
    '" + msg.Replace("'", "''") + @"',
    '" + lstvalues[i].Latitude + @"',
    '" + lstvalues[i].Longitude + @"',
    0,
   'Harsh Braking',
    " + dt.Rows[0]["tbl_alert_setting_id"].ToString() + @"

)";



                                    General.DML(insertQuery);

                                    General.SendNotification(
                                        Convert.ToInt32(session.UserId.ToString()),
                                        msg,
                                        48,
                                        lstvalues[i].Latitude,
                                        lstvalues[i].Longitude,
                                        msg
                                    ).GetAwaiter().GetResult();
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            //General.WriteToLogFile(
                            //    ex.Message,
                            //    GlobalVariable.m_folderpath,
                            //    "HarshBraking.txt"
                            //);
                        }
                    }





                   if ( lstvalues[i].packet_type.Contains("FI"))
                    {
                        try
                        {
                            
                                if (lstvalues[i].Latitude == 0.0 || lstvalues[i].Longitude == 0.0)
                                {

                                    continue;

                                }
                                string is_notification_enable = $@"select tas.id as tbl_alert_setting_id,* from tbl_alert_master  tam
                                                join tbl_alert_setting tas
                                                on tam.id = tas.alert_id
                                                where tas.alert_id = 9 and tas.service_id = {session.ServiceId} and tas.is_active=1 and tas.is_notification = 1";

                            string isnotificationsend = "SELECT top(1) sent_on FROM tbl_alert_log WHERE sys_service_id = " + session.ServiceId + " AND message LIKE '%has entered the Geo-Fence area associated with Geo-Fence ID%' order by sent_on desc;";
                            DataTable issended = General.SelectQuery(isnotificationsend);
                            if (issended.Rows.Count > 0)
                            {
                                DateTime lastAlertTime = Convert.ToDateTime(issended.Rows[0]["sent_on"]);

                                if (DateTime.Now > lastAlertTime.AddMinutes(10))
                                {


                                    DataTable dt = General.SelectQuery(is_notification_enable);

                                    if (dt != null && dt.Rows.Count > 0)
                                    {
                                        string x = lstvalues[i].packet_type.Substring(2);

                                        string address = General.GetLocationFromLatLong(lstvalues[i].Latitude, lstvalues[i].Longitude).Result;
                                      
string insertQuery = @"
insert into tbl_alert_log
(
    sys_service_id,
    alert_setting_id,
    sent_on,
    message,
    gps_latitude,
    gps_longitude,
    is_read,
    msg_status
)
values
(
    " + session.ServiceId + @",
    " + dt.Rows[0]["tbl_alert_setting_id"].ToString() + @",
    '" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + @"',
    'Dear Customer, your vehicle "
    + session.VehicleName
    + " is inside the Geo-Fence area at location (Lat: "
    + lstvalues[i].Latitude
    + ", Long: "
    + lstvalues[i].Longitude
    + ") near "
    + address
    + " on "
    + Convert.ToDateTime(lstvalues[i].GpsDateTime)
        .AddMinutes(330)
        .ToString("yyyy-MM-dd HH:mm:ss")
    + @"',
    '" + lstvalues[i].Latitude + @"',
    '" + lstvalues[i].Longitude + @"',
    0,
    'Geo-Fence In'
)";


                                        General.DML(insertQuery);

                                        string msg = "Dear Customer, your vehicle "
+ session.VehicleName
+ " has entered the Geo-Fence area at location (Lat: "
+ lstvalues[i].Latitude
+ ", Long: "
+ lstvalues[i].Longitude
+ ") on "
+ Convert.ToDateTime(lstvalues[i].GpsDateTime)
    .AddMinutes(330)
    .ToString("yyyy-MM-dd HH:mm:ss");
                                        General.SendNotification(Convert.ToInt32(session.UserId.ToString()), msg, Convert.ToInt32(dt.Rows[0]["tbl_alert_setting_id"].ToString()), lstvalues[i].Latitude, lstvalues[i].Longitude, msg).GetAwaiter().GetResult();
                                    }

                                }

                            }
                            else
                            {
                                DataTable dt = General.SelectQuery(is_notification_enable);

                                if (dt != null && dt.Rows.Count > 0)
                                {
                                    string x = lstvalues[i].packet_type.Substring(2);

                                    string address = General.GetLocationFromLatLong(lstvalues[i].Latitude, lstvalues[i].Longitude).Result;
                                    string insertQuery = @"
insert into tbl_alert_log
(
    sys_service_id,
    alert_setting_id,
    sent_on,
    message,
    gps_latitude,
    gps_longitude,
    is_read,
    msg_status
)
values
(
    " + session.ServiceId + @",
    " + dt.Rows[0]["tbl_alert_setting_id"].ToString() + @",
    '" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + @"',
    'Dear Customer, your vehicle "
     + session.VehicleName
     + " is inside the Geo-Fence area at location (Lat: "
     + lstvalues[i].Latitude
     + ", Long: "
     + lstvalues[i].Longitude
     + ") near "
     + address
     + " on "
     + Convert.ToDateTime(lstvalues[i].GpsDateTime)
         .AddMinutes(330)
         .ToString("yyyy-MM-dd HH:mm:ss")
     + @"',
    '" + lstvalues[i].Latitude + @"',
    '" + lstvalues[i].Longitude + @"',
    0,
    'Geo-Fence In'
)";
                                    General.DML(insertQuery);

                                    string msg = "Dear Customer, your vehicle "
 + session.VehicleName
 + " has entered the Geo-Fence area at location (Lat: "
 + lstvalues[i].Latitude
 + ", Long: "
 + lstvalues[i].Longitude
 + ") on "
 + Convert.ToDateTime(lstvalues[i].GpsDateTime)
     .AddMinutes(330)
     .ToString("yyyy-MM-dd HH:mm:ss");

                                    General.SendNotification(Convert.ToInt32(session.UserId.ToString()), msg, Convert.ToInt32(dt.Rows[0]["tbl_alert_setting_id"].ToString()), lstvalues[i].Latitude, lstvalues[i].Longitude, msg).GetAwaiter().GetResult();
                                }
                            }

                        }
                        catch (Exception ex)
                        {
                            //General.WriteToLogFile(ex.Message + Environment.NewLine + requestInfo.Body, GlobalVariable.m_folderpath, "exPanic.txt");
                        }
                    }

                    if (lstvalues[i].packet_type.Contains("FO"))
                    {
                        try
                        {
                            
                                if (lstvalues[i].Latitude == 0.0 || lstvalues[i].Longitude == 0.0)
                                {

                                    continue;

                                }

                                string is_notification_enable = $@"select tas.id as tbl_alert_setting_id,* from tbl_alert_master  tam
                                                join tbl_alert_setting tas
                                                on tam.id = tas.alert_id
                                                where tas.alert_id = 9 and tas.service_id = {session.ServiceId} and tas.is_active=1 and tas.is_notification = 1";

                            string isnotificationsend = "SELECT top(1) sent_on FROM tbl_alert_log WHERE sys_service_id = " + session.ServiceId + " AND message LIKE '%has exited the Geo-Fence area associated with Geo-Fence ID%' order by sent_on desc;";
                            DataTable issended = General.SelectQuery(isnotificationsend);
                            if (issended.Rows.Count > 0)
                            {
                                DateTime lastAlertTime = Convert.ToDateTime(issended.Rows[0]["sent_on"]);

                                if (DateTime.Now > lastAlertTime.AddMinutes(10))
                                {

                                    DataTable dt = General.SelectQuery(is_notification_enable);

                                    if (dt != null && dt.Rows.Count > 0)
                                    {
                                        string x = lstvalues[i].packet_type.Substring(2);

                                        string address = General.GetLocationFromLatLong(lstvalues[i].Latitude, lstvalues[i].Longitude).Result;
                                     
string insertQuery = @"
insert into tbl_alert_log
(
    sys_service_id,
    alert_setting_id,
    sent_on,
    message,
    gps_latitude,
    gps_longitude,
    is_read,
    msg_status
)
values
(
    " + session.ServiceId + @",
    " + dt.Rows[0]["tbl_alert_setting_id"].ToString() + @",
    '" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + @"',
    'Dear Customer, your vehicle "
    + session.VehicleName
    + " has exited the Geo-Fence area at location (Lat: "
    + lstvalues[i].Latitude
    + ", Long: "
    + lstvalues[i].Longitude
    + ") near "
    + address
    + " on "
    + Convert.ToDateTime(lstvalues[i].GpsDateTime)
        .AddMinutes(330)
        .ToString("yyyy-MM-dd HH:mm:ss")
    + @"',
    '" + lstvalues[i].Latitude + @"',
    '" + lstvalues[i].Longitude + @"',
    0,
    'Geo-Fence Out'
)";


                                        General.DML(insertQuery);

                                        string msg = "Dear Customer, your vehicle " + session.VehicleName + " has exited the Geo-Fence area at location (Lat: " + lstvalues[i].Latitude + ", Long: " + lstvalues[i].Longitude + ") on " + Convert.ToDateTime(lstvalues[i].GpsDateTime).AddMinutes(330).ToString("yyyy-MM-dd HH:mm:ss");

                                        General.SendNotification(Convert.ToInt32(session.UserId.ToString()), msg, Convert.ToInt32(dt.Rows[0]["tbl_alert_setting_id"].ToString()), lstvalues[i].Latitude, lstvalues[i].Longitude, msg).GetAwaiter().GetResult();
                                    }
                                }

                            }
                            else
                            {
                                DataTable dt = General.SelectQuery(is_notification_enable);

                                if (dt != null && dt.Rows.Count > 0)
                                {
                                    string x = lstvalues[i].packet_type.Substring(2);

                                    string address = General.GetLocationFromLatLong(lstvalues[i].Latitude, lstvalues[i].Longitude).Result;
                                    
string insertQuery = @"
insert into tbl_alert_log
(
    sys_service_id,
    alert_setting_id,
    sent_on,
    message,
    gps_latitude,
    gps_longitude,
    is_read,
    msg_status
)
values
(
    " + session.ServiceId + @",
    " + dt.Rows[0]["tbl_alert_setting_id"].ToString() + @",
    '" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + @"',
    'Dear Customer, your vehicle "
    + session.VehicleName
    + " has exited the Geo-Fence area at location (Lat: "
    + lstvalues[i].Latitude
    + ", Long: "
    + lstvalues[i].Longitude
    + ") near "
    + address
    + " on "
    + Convert.ToDateTime(lstvalues[i].GpsDateTime)
        .AddMinutes(330)
        .ToString("yyyy-MM-dd HH:mm:ss")
    + @"',
    '" + lstvalues[i].Latitude + @"',
    '" + lstvalues[i].Longitude + @"',
    0,
    'Geo-Fence Out'
)";


                                    General.DML(insertQuery);

                                    string msg = "Dear Customer, your vehicle " + session.VehicleName + " has exited the Geo-Fence area at location (Lat: " + lstvalues[i].Latitude + ", Long: " + lstvalues[i].Longitude + ") on " + Convert.ToDateTime(lstvalues[i].GpsDateTime).AddMinutes(330).ToString("yyyy-MM-dd HH:mm:ss");

                                    General.SendNotification(Convert.ToInt32(session.UserId.ToString()), msg, Convert.ToInt32(dt.Rows[0]["tbl_alert_setting_id"].ToString()), lstvalues[i].Latitude, lstvalues[i].Longitude, msg).GetAwaiter().GetResult();
                                }
                            }

                        }
                        catch (Exception ex)
                        {
                            //General.WriteToLogFile(ex.Message + Environment.NewLine + requestInfo.Body, GlobalVariable.m_folderpath, "exPanic.txt");
                        }
                    }


                    string stableName = "tbl_telemetry_" + lstvalues[i].GpsDateTime.ToString("MMMyy");
                   
                    if (lstvalues[i].packet_type == "IN" || lstvalues[i].packet_type=="IF")
                    {
                        lstvalues[i].valid = "A";


                        string query = "insert into " + stableName + "(sys_service_id,sys_proc_time,gps_time,gps_validity,gps_latitude,latitude_direction,gps_longitude,longitude_direction,gps_speed,gps_orientation";
                        query += ",variation,checksum,I1,I2,I3,I4,I5,I6,I7,I8,I9,I10,I11,I12,I13,I14,I15,tel_odometer,mobile_country_code,mobile_network_code,location_area_code,cell_id,sys_msg_type,";
                        query += "firmware_version,visible_satellite,altitude,address_from_device,";
                        query += "tank_capacity,battery_voltage,signal_strength,tel_fuel)";
                        query += "values('" + session.ServiceId + "',GetDate()" +
                        ",'" + lstvalues[i].GpsDateTime.ToString("yyyy-MM-dd HH:mm:ss") +
                        "','" + lstvalues[i].valid +
                        "','" + lstvalues[i].Latitude +
                        "','" + lstvalues[i].NorthandSouth +
                        "','" + lstvalues[i].Longitude +
                        "','" + lstvalues[i].EastandWest +
                        "','" + lstvalues[i].Speed_knot +
                        "','" + lstvalues[i].Angle_of_motion +
                        "','0','None'," +

                        lstvalues[i].MainPower + "," +      // I1
                        lstvalues[i].Ignition + "," +       // I2 --> ignition
                        lstvalues[i].LowPanic + "," +       // I3
                        "0," +       // I4
                        "0,0,0,0,0,0," +                    // I5-I10

                        "'" + lstvalues[i].Harsh_braking + "'," +   // I11
                        "'" + lstvalues[i].arm + "'," +             // I12
                        "'0'," +                                    // I13
                        "'0'," +                                    // I14
                        "'" + lstvalues[i].Harshturn + "'," +             // I15

                        "'" + lstvalues[i].Odometer + "','" +
                        lstvalues[i].Mobile_country_code + "','" +
                        lstvalues[i].Mobile_Network_code + "','" +
                        lstvalues[i].Location_area_code + "','" +
                        lstvalues[i].Cell_id + "','0','0',0,'0','','0','" +
                        lstvalues[i].Battery + "','" +
                        lstvalues[i].Signal_Strength + "','" +
                        0 + "')";
                        bool Result = General.DML(query);
                        if (Result == false)
                        {
                            General.WriteToLogFile("Packet Received \n" + "Failed to Add to telemetry", GlobalVariable.m_folderpath, "WrongProtocol\\ErrorMessage" + ".txt");

                        }
                    }

                    if (lstvalues[i].packet_type == "HB")
                    {
                        lstvalues[i].valid = "A";


                        string query = "insert into " + stableName + "(sys_service_id,sys_proc_time,gps_time,gps_validity,gps_latitude,latitude_direction,gps_longitude,longitude_direction,gps_speed,gps_orientation";
                        query += ",variation,checksum,I1,I2,I3,I4,I5,I6,I7,I8,I9,I10,I11,I12,I13,I14,I15,tel_odometer,mobile_country_code,mobile_network_code,location_area_code,cell_id,sys_msg_type,";
                        query += "firmware_version,visible_satellite,altitude,address_from_device,";
                        query += "tank_capacity,battery_voltage,signal_strength,tel_fuel)";
                        query += "values('" + session.ServiceId + "',GetDate()" +
                         ",'" + lstvalues[i].GpsDateTime.ToString("yyyy-MM-dd HH:mm:ss") +
                        "','" + lstvalues[i].valid +
                        "','" + lstvalues[i].Latitude +
                        "','" + lstvalues[i].NorthandSouth +
                        "','" + lstvalues[i].Longitude +
                        "','" + lstvalues[i].EastandWest +
                        "','" + lstvalues[i].Speed_knot +
                        "','" + lstvalues[i].Angle_of_motion +
                        "','0','None'," +

                        lstvalues[i].MainPower + "," +      // I1
                        lstvalues[i].Ignition + "," +       // I2
                        lstvalues[i].LowPanic + "," +       // I3
                        "0," +       // I4
                        "0,0,0,0,0,1," +                    // I5-I10

                        "'" + lstvalues[i].Harsh_braking + "'," +   // I11
                        "'" + lstvalues[i].arm + "'," +             // I12
                        "'0'," +                                    // I13
                        "'0'," +                                    // I14
                        "'" + lstvalues[i].Harshturn + "'," +             // I15

                        "'" + lstvalues[i].Odometer + "','" +
                        lstvalues[i].Mobile_country_code + "','" +
                        lstvalues[i].Mobile_Network_code + "','" +
                        lstvalues[i].Location_area_code + "','" +
                        lstvalues[i].Cell_id + "','0','0',0,'0','','0','" +
                        lstvalues[i].Battery + "','" +
                        lstvalues[i].Signal_Strength + "','" +
                        0 + "')";
                        bool Result = General.DML(query);
                        if (Result == false)
                        {
                            General.WriteToLogFile("Packet Received \n" + "Failed to Add to telemetry", GlobalVariable.m_folderpath, "WrongProtocol\\ErrorMessage" + ".txt");

                        }
                    }

                    if (lstvalues[i].packet_type=="RT")
                    {
                        lstvalues[i].valid = "A";


                        string query = "insert into " + stableName + "(sys_service_id,sys_proc_time,gps_time,gps_validity,gps_latitude,latitude_direction,gps_longitude,longitude_direction,gps_speed,gps_orientation";
                        query += ",variation,checksum,I1,I2,I3,I4,I5,I6,I7,I8,I9,I10,I11,I12,I13,I14,I15,tel_odometer,mobile_country_code,mobile_network_code,location_area_code,cell_id,sys_msg_type,";
                        query += "firmware_version,visible_satellite,altitude,address_from_device,";
                        query += "tank_capacity,battery_voltage,signal_strength,tel_fuel)";
                        query += "values('" + session.ServiceId + "',GetDate()" +
                        ",'" + lstvalues[i].GpsDateTime.ToString("yyyy-MM-dd HH:mm:ss") +
                        "','" + lstvalues[i].valid +
                        "','" + lstvalues[i].Latitude +
                        "','" + lstvalues[i].NorthandSouth +
                        "','" + lstvalues[i].Longitude +
                        "','" + lstvalues[i].EastandWest +
                        "','" + lstvalues[i].Speed_knot +
                        "','" + lstvalues[i].Angle_of_motion +
                        "','0','None'," +

                        lstvalues[i].MainPower + "," +      // I1
                        lstvalues[i].Ignition + "," +       // I2
                        lstvalues[i].LowPanic + "," +       // I3
                        "0," +       // I4
                        "0,0,0,0,0,0," +                    // I5-I10

                        "'" + lstvalues[i].Harsh_braking + "'," +   // I11
                        "'" + lstvalues[i].arm + "'," +             // I12
                        "'0'," +                                    // I13
                        "'0'," +                                    // I14
                        "'" + "1" + "'," +             // I15

                        "'" + lstvalues[i].Odometer + "','" +
                        lstvalues[i].Mobile_country_code + "','" +
                        lstvalues[i].Mobile_Network_code + "','" +
                        lstvalues[i].Location_area_code + "','" +
                        lstvalues[i].Cell_id + "','0','0',0,'0','','0','" +
                        lstvalues[i].Battery + "','" +
                        lstvalues[i].Signal_Strength + "','" +
                        0 + "')";
                        bool Result = General.DML(query);
                        if (Result == false)
                        {
                            General.WriteToLogFile("Packet Received \n" + "Failed to Add to telemetry", GlobalVariable.m_folderpath, "WrongProtocol\\ErrorMessage" + ".txt");

                        }
                    }

                    if (lstvalues[i].packet_type == "HA")
                    {
                        lstvalues[i].valid = "A";


                        string query = "insert into " + stableName + "(sys_service_id,sys_proc_time,gps_time,gps_validity,gps_latitude,latitude_direction,gps_longitude,longitude_direction,gps_speed,gps_orientation";
                        query += ",variation,checksum,I1,I2,I3,I4,I5,I6,I7,I8,I9,I10,I11,I12,I13,I14,I15,tel_odometer,mobile_country_code,mobile_network_code,location_area_code,cell_id,sys_msg_type,";
                        query += "firmware_version,visible_satellite,altitude,address_from_device,";
                        query += "tank_capacity,battery_voltage,signal_strength,tel_fuel)";
                        query += "values('" + session.ServiceId + "',GetDate()" +
                        ",'" + lstvalues[i].GpsDateTime.ToString("yyyy-MM-dd HH:mm:ss") +
                        "','" + lstvalues[i].valid +
                        "','" + lstvalues[i].Latitude +
                        "','" + lstvalues[i].NorthandSouth +
                        "','" + lstvalues[i].Longitude +
                        "','" + lstvalues[i].EastandWest +
                        "','" + lstvalues[i].Speed_knot +
                        "','" + lstvalues[i].Angle_of_motion +
                        "','0','None'," +

                        lstvalues[i].MainPower + "," +      // I1
                        lstvalues[i].Ignition + "," +       // I2
                        lstvalues[i].LowPanic + "," +       // I3
                        "0," +       // I4
                        "0,0,0,0,1,0," +                    // I5-I10    i9=1 for the harsh acceleration alert

                        "'" + lstvalues[i].Harsh_braking + "'," +   // I11
                        "'" + lstvalues[i].arm + "'," +             // I12
                        "'0'," +                                    // I13
                        "'0'," +                                    // I14
                        "'" + lstvalues[i].Harshturn + "'," +             // I15

                        "'" + lstvalues[i].Odometer + "','" +
                        lstvalues[i].Mobile_country_code + "','" +
                        lstvalues[i].Mobile_Network_code + "','" +
                        lstvalues[i].Location_area_code + "','" +
                        lstvalues[i].Cell_id + "','0','0',0,'0','','0','" +
                        lstvalues[i].Battery + "','" +
                        lstvalues[i].Signal_Strength + "','" +
                        0 + "')";
                        bool Result = General.DML(query);
                        if (Result == false)
                        {
                            General.WriteToLogFile("Packet Received \n" + "Failed to Add to telemetry", GlobalVariable.m_folderpath, "WrongProtocol\\ErrorMessage" + ".txt");

                        }
                    }



                    if (lstvalues[i].valid == "1")
                    {
                        lstvalues[i].valid = "A";


                        string query = "insert into " + stableName + "(sys_service_id,sys_proc_time,gps_time,gps_validity,gps_latitude,latitude_direction,gps_longitude,longitude_direction,gps_speed,gps_orientation";
                        query += ",variation,checksum,I1,I2,I3,I4,I5,I6,I7,I8,I9,I10,I11,I12,I13,I14,I15,tel_odometer,mobile_country_code,mobile_network_code,location_area_code,cell_id,sys_msg_type,";
                        query += "firmware_version,visible_satellite,altitude,address_from_device,";
                        query += "tank_capacity,battery_voltage,signal_strength,tel_fuel)";
                        query += "values('" + session.ServiceId + "',GetDate()" +
                        ",'" + lstvalues[i].GpsDateTime.ToString("yyyy-MM-dd HH:mm:ss") +
                        "','" + lstvalues[i].valid +
                        "','" + lstvalues[i].Latitude +
                        "','" + lstvalues[i].NorthandSouth +
                        "','" + lstvalues[i].Longitude +
                        "','" + lstvalues[i].EastandWest +
                        "','" + lstvalues[i].Speed_knot +
                        "','" + lstvalues[i].Angle_of_motion +
                        "','0','None'," +

                        lstvalues[i].MainPower + "," +      // I1
                        lstvalues[i].Ignition + "," +       // I2
                        lstvalues[i].LowPanic + "," +       // I3
                        "0," +       // I4
                        "0,0,0,0,0,0," +                    // I5-I10

                        "'" + lstvalues[i].Harsh_braking + "'," +   // I11
                        "'" + lstvalues[i].arm + "'," +             // I12
                        "'0'," +                                    // I13
                        "'0'," +                                    // I14
                        "'" + lstvalues[i].Harshturn + "'," +             // I15

                        "'" + lstvalues[i].Odometer + "','" +
                        lstvalues[i].Mobile_country_code + "','" +
                        lstvalues[i].Mobile_Network_code + "','" +
                        lstvalues[i].Location_area_code + "','" +
                        lstvalues[i].Cell_id + "','0','0',0,'0','','0','" +
                        lstvalues[i].Battery + "','" +
                        lstvalues[i].Signal_Strength + "','" +
                        0 + "')";
                        bool Result = General.DML(query);
                        if (Result == false)
                        {
                            General.WriteToLogFile("Packet Received \n" + "Failed to Add to telemetry", GlobalVariable.m_folderpath, "WrongProtocol\\ErrorMessage" + ".txt");

                        }
                    }
                }

            }
            catch (Exception ex)
            {
                General.WriteToLogFile("Packet Received \n" + ex.Message, GlobalVariable.m_folderpath, "WrongProtocol\\ErrorMessage" + ".txt");



            }

        }

        private void CheckCommand(string service_id, CustomSession session,General gen)
        {
            try
            {
                DataTable dt = General.SelectQuery("select top 1 command ,id from tbl_send_command_log where imei = '" + session.IMEI + "' and is_pending =1");

                if (dt != null)
                {
                    if (dt.Rows.Count > 0)
                    {

                        string command = dt.Rows[0]["command"].ToString();
                        int id = Convert.ToInt32(dt.Rows[0]["id"]);
                        session.command_id = id;
                        session.Send(command);
                        Thread.Sleep(100);
                        General.WriteToLogFile("Command Sent To Device:"+command, GlobalVariable.m_folderpath, "ReceivedData\\" + session.IMEI + ".txt");
                        General.DML("update tbl_send_command_log set send_time =getdate(),is_pending=0 where id=" + id);
                    }
                }
            }
            catch (Exception e)
            {
                General.WriteToLogFile(e.Message, GlobalVariable.m_folderpath, "cmd_err.txt");

            }
        }


        private void insertResponse(string response, CustomSession session,General gen)
        {
            try
            {
                if (session.command_id != 0)
                {

                    General.WriteToLogFile("RESPONSE FROM DEVICE: '" + response + "'", GlobalVariable.m_folderpath, "ReceivedData\\" + session.IMEI + ".txt");
                    string query = "update tbl_send_command_log set response = '" + response + "' , response_time =getdate(),is_pending =0 where id =" + session.command_id;
                    General.DML(query);
                    session.command_id = 0;

                }
            }
            catch (Exception e)
            {
                General.WriteToLogFile(e.Message, GlobalVariable.m_folderpath, "cmd_err.txt");

            }
        }


    }
}


