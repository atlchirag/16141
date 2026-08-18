using System;
using SuperSocket.SocketBase;
using SuperSocket.SocketBase.Protocol;
using System.Data.SqlClient;
using SuperSocket.SocketBase.Config;
using MongoDB.Driver;
using MongoDB.Bson;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace GPSTrackerListeners.ORSAC
{
   public class ORSACServer : AppServer<CustomSession>
    {

       private string m_DBType;
       private  string m_FolderPath;
       private string m_userName;

        public ORSACServer()
            : base(new DefaultReceiveFilterFactory<CustomReceiveFilter, StringRequestInfo>())
        {
            
            this.NewRequestReceived += new RequestHandler<CustomSession, StringRequestInfo>(server_NewRequestReceived);
           
        }

        protected override bool Setup(IRootConfig rootConfig, IServerConfig config)
        {
            m_DBType = "SQLServer";
            m_FolderPath = @"E:\ListenerData\ORSAC\2525";
           

            //m_DBType = config.Options.GetValues("dbType").GetValue(0).ToString();
            //m_FolderPath = config.Options.GetValues("logFolder").GetValue(0).ToString();
            //m_userName = config.Options.GetValues("userName").GetValue(0).ToString();

            return true;
        }
        
        private void server_NewRequestReceived(CustomSession session, StringRequestInfo requestInfo)
        {
            
            General.HandleNewRequest(session, requestInfo, m_FolderPath);
            
            
            //if(string.IsNullOrEmpty(session.ServiceId))
            //{
            //    string serviceId = General.GetServiceID(requestInfo.Key);

            //    if(serviceId!="0")
            //        session.ServiceId = General.GetServiceID(requestInfo.Key);
            //}

            if (!string.IsNullOrEmpty(session.ServiceId))
                InsertIntoDB(requestInfo, session);

        }
        
        private void InsertIntoDB(StringRequestInfo requestInfo, CustomSession session)
        {
            int counter = 0;
            foreach (string data in requestInfo.Parameters)
            {
                counter += 1;
                if (counter == 1)
                    continue;

                string[] fieldsArray = data.Trim().Split(',');
                string header = fieldsArray[0];
                string dataStatus = fieldsArray[4];

                string gpsValidity = fieldsArray[7];

                if (gpsValidity == "1")
                    gpsValidity = "A";
                else
                    gpsValidity = "V";

                //GPRMC Data
                string gpsTime = General.GetGPSDateTime(fieldsArray[8], fieldsArray[9]).ToString("yyyy-MM-dd HH:mm:ss");
                if (Convert.ToDateTime(gpsTime) == new DateTime())
                {
                    General.WriteToLogFile(requestInfo.Body,m_FolderPath, "exDate.txt");
                    continue;
                }

                double latitude = Convert.ToDouble(fieldsArray[10]);
                string latitudeDirection = fieldsArray[11];
                double longitude = Convert.ToDouble(fieldsArray[12]);

                if (latitude == 0 || longitude == 0 || requestInfo.Key.Length < 15)
                {
                    General.WriteToLogFile(requestInfo.Body, m_FolderPath, "exInvalidPacket.txt");
                    continue;
                }

                string longitudeDirection = fieldsArray[13];
                double gpsSpeed = string.IsNullOrEmpty(fieldsArray[14]) ? 0 : Convert.ToDouble(fieldsArray[14]);
                //gpsSpeed = gpsSpeed * 1.852; // In kilometers
                string gpsOrientation_trueCourse = (fieldsArray[15] == "") ? "0.000000000" : fieldsArray[15];
              //  string variation = fieldsArray[10];
              //  string checksum = fieldsArray[12];
               // string location = "";
                //int locationFlag = 0;
                //// Locaction enabled or not 
                //if (checksum.Contains("$LOC"))
                //{
                //    checksum = checksum.Substring(0, checksum.IndexOf('$'));
                //    location = fieldsArray[13];
                //    locationFlag = 1;
                //}
                // Alerts of various I/O. skipping '#' in first place
               // char[] IOAlertsArray = fieldsArray[24].ToCharArray();

                int ignition_i2 = Convert.ToInt16(fieldsArray[21]);
               int mainPower_i1 = Convert.ToInt16(fieldsArray[22]);
                //int sos_i3 = Convert.ToInt16(IOAlertsArray[3].ToString());
                //int i4 = Convert.ToInt16(IOAlertsArray[4].ToString());
                //int i5 = Convert.ToInt16(IOAlertsArray[5].ToString());
                //int ac_i6 = Convert.ToInt16(IOAlertsArray[6].ToString());
                //int i7 = Convert.ToInt16(IOAlertsArray[7].ToString());
                //int mainPower_i1 = Convert.ToInt16(IOAlertsArray[8].ToString());
                //int harshSpeeding_i9 = Convert.ToInt16(IOAlertsArray[9].ToString());
                //int harshBreaking_i10 = Convert.ToInt16(IOAlertsArray[10].ToString());
                //int armDisarm_i11 = Convert.ToInt16(IOAlertsArray[11].ToString());
                //int sleepStatus_i12 = Convert.ToInt16(IOAlertsArray[12].ToString());
                //int generalRelayStatus_i13 = Convert.ToInt16(IOAlertsArray[13].ToString());
                //int accelerometerStatus_i14 = Convert.ToInt16(IOAlertsArray[14].ToString());

                //try
                //{
                //    // Panic Alert
                //    if (sos_i3 == 0)
                //    {


                //        if (session.IsPanicAlert)
                //        {
                //            session.PanicCount += 1;

                //            if (session.PanicCount < 3)
                //            {

                //                string insertQuery = @"insert into panicalert_log  (sys_service_id,lastAlertTime,alert_msg,readFlag)  values (" + session.ServiceId + ",'" + Convert.ToDateTime(gpsTime).AddMinutes(330).ToString("yyyy-MM-dd HH:mm:ss") + "','Warning: Panic button has been pressed on vehicle " + session.VehicleName + "',0)";
                //                General.DML(insertQuery);

                //                if (General.IsAlertTime(Convert.ToDateTime(session.PanicAlert.FromAlertTime.ToString()), Convert.ToDateTime(session.PanicAlert.ToAlertTime.ToString())))
                //                {
                //                    if (!string.IsNullOrEmpty(session.PanicAlert.Mobile))
                //                        General.SendSms("Warning: Panic button has been pressed on vehicle " + session.VehicleName, session.PanicAlert.Mobile);

                //                    if (!string.IsNullOrEmpty(session.PanicAlert.Mobile1))
                //                        General.SendSms("Warning: Panic button has been pressed on vehicle " + session.VehicleName, session.PanicAlert.Mobile1);

                //                    if (!string.IsNullOrEmpty(session.PanicAlert.Mobile2))
                //                        General.SendSms("Warning: Panic button has been pressed on vehicle " + session.VehicleName, session.PanicAlert.Mobile2);

                //                    List<string> cc = null;

                //                    if (!string.IsNullOrEmpty(session.PanicAlert.Email1))
                //                    {
                //                        cc.Add(session.PanicAlert.Email1);
                //                    }

                //                    if (!string.IsNullOrEmpty(session.PanicAlert.Email2))
                //                        cc.Add(session.PanicAlert.Email2);

                //                    General.SendEmail("Panic Alert", "Warning: Panic button has been pressed on vehicle " + session.VehicleName, session.PanicAlert.Email, cc);

                //                    // Task.Factory.StartNew(() => General.SendSms("Warning: Panic button has been pressed on vehicle " + session.VehicleName, session.PanicAlert.Mobile));
                //                    //Task.Factory.StartNew(() => General.SendEmail("Panic Alert", "Warning: Panic button has been pressed on vehicle " + session.VehicleName, session.PanicAlert.Email));
                //                }
                //            }
                //            else
                //            {
                //                General.DML(@"update panicalert set is_active=0 where sys_service_id=" + session.ServiceId);
                //                General.SendEmail("SOS ISSUE IN APN", "There may be problem with SOS in vehicle " + session.VehicleName, "support@atlantasys.com");
                //                session.IsPanicAlert = false;
                //            }

                //        }
                //    }
                //}
                //catch (Exception ex)
                //{
                //    General.WriteToLogFile(ex.Message + Environment.NewLine + requestInfo.Body, m_FolderPath, "exPanic.txt");
                //}


                // Other ALERTS
                // string voltageForFuel = fieldsArray[14 + locationFlag];
                //// string fuel = fieldsArray[15 + locationFlag];
                // string tankCapacity = fieldsArray[16 + locationFlag];
                // double odometer = (fieldsArray[17 + locationFlag]) == "" ? 0 : Convert.ToDouble(fieldsArray[17 + locationFlag]);

                //double distance = 0;


                //  if (Convert.ToDateTime(gpsTime) >= session.LastGPSTime)
                //  {

                //      distance = General.HaversineInKM(session.LastLat, session.LastLng, latitude, longitude);

                //      session.LastLat = latitude;
                //      session.LastLng = longitude;
                //      session.LastGPSTime = Convert.ToDateTime(gpsTime);
                //  }

                ////  double odometer = session.LastOdometer + distance;
                //  session.LastOdometer = odometer;


                int sattelliteCount = Convert.ToInt32(fieldsArray[16]);
                //double temperature = (string.IsNullOrEmpty(fieldsArray[18 + locationFlag])) ? 0 : Convert.ToDouble(fieldsArray[18 + locationFlag]);
                string firmwareVersion = fieldsArray[2];
               // string deviceInternalVoltageBattery = fieldsArray[19 + locationFlag];
                string gsmSignalStrength = fieldsArray[25];
                string mobileCountryCode = fieldsArray[26];
                string mobileNetworkCode = fieldsArray[27];
                string locationAreaCode = fieldsArray[28];
                //string cellIdWithSignature = fieldsArray[(i * validDataCount) + (validDataCount - 1)];
                string cellId = fieldsArray[29];
                
                // Database extra mandatory fields
                string sysDateTime = DateTime.Now.ToString("yyy-MM-dd HH:mm:ss");
                string machineName = Environment.MachineName;

                string tableMonth= Convert.ToDateTime(gpsTime).AddMinutes(session.UTCOffset).ToString("MMMyy").ToLower();
                

                string query = "insert into telemetry_" + tableMonth + "(signature,sys_service_id,sys_proc_time,gps_time,gps_validity,gps_latitude,latitude_direction,gps_longitude," +
                         "longitude_direction,gps_speed,gps_orientation,variation,checksum,I1," +
                         "I2,I3,I4,I5,I6,I7,I8,I9," +
                         "I10,I11,I12,I13,I14,I15," +
                         "serial1_voltage,tel_fuel,tank_capacity,tel_odometer,tel_temperature,battery_voltage," +
                         "signal_strength,mobile_country_code,mobile_network_code,location_area_code,cell_id," +
                         "sys_msg_type,sys_proc_host,data_status,vehicle_status,firmware_version,visible_satellite,altitude,address_from_device,jny_distance)" +
                         "values(" +
                         "'ATLSRS','" + session.ServiceId + "','" + sysDateTime + "','" + gpsTime + "','" + gpsValidity + "','" + latitude + "','" + latitudeDirection + "','" + longitude + "','" +
                         longitudeDirection + "','" + gpsSpeed + "','" + gpsOrientation_trueCourse + "','0','','" + mainPower_i1 + "','" +
                         ignition_i2 + "','1','0','0','0','0','1','0','" +
                         "0','0','0','0','0','0','" +
                         "0','0','0','0','0','0','" +
                         gsmSignalStrength + "','" + mobileCountryCode + "','" + mobileNetworkCode + "','" + locationAreaCode + "','" + cellId + "','" +
                         "1','" + Environment.MachineName + "','','','" + firmwareVersion + "','0','','',0)";

                try
                {

                    
                        General.DML(query);
                   

                    //// Direct address insertion in database
                    //if (locationFlag == 1)
                    //{
                    //    if (session.PrevLatitude != latitude.ToString() && session.PrevLongitude != longitude.ToString())
                    //    {
                    //        if (!string.IsNullOrEmpty(location.Trim()) && !location.Trim().ToLower().Contains("not found"))
                    //        {

                    //            //MongoServer server = MongoServer.Create(new MongoConnectionStringBuilder("Server=localhost:27017"));
                    //            //server.Connect();
                    //            //var database = server.GetDatabase("newtrack");
                    //            //var collection = database.GetCollection<BsonDocument>("tbl_geodata");

                    //            //var document = new BsonDocument 

                    //            //{

                    //            //     { "geo_street",location},
                    //            //    {"location", new BsonArray(new[] {longitude,latitude})}

                    //            //};
                    //            //collection.Insert(document);

                    //            //Mysql //General.DML("INSERT INTO tbl_geodata (geo_street,gps_latitude,gps_longitude) VALUES ('" + location + "','" + latitude + "','" + longitude + "')", true);

                                
                    //                General.DML("INSERT INTO tbl_geodata (geo_street,gps_latitude,gps_longitude) VALUES ('" + location + "'," + latitude + "," + longitude + ")");
                    //        }

                    //        session.PrevLatitude = latitude.ToString();
                    //        session.PrevLongitude = longitude.ToString();
                    //    }
                    //}
                    
                }
                
                catch (Exception ex)
                {

                    General.WriteToLogFile(ex.Message + Environment.NewLine + requestInfo.Body, m_FolderPath, "exDb.txt");

                }

            }
        }
    }
}
