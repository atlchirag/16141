using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GPSTrackerListeners.ORSAC
{
    class DecryptResponse
    {

        // this Gone be decrypt the Tracking Responce
        public static List<CordinateValue> AssignValue(string[] Messages)
        {
            List<CordinateValue> lstCordinate = new List<CordinateValue>();
            List<string> lstMessage = new List<string>();
            int j = 1;

            //for (int i = 0; i < Messages.Length; i++)
            //{
            //    try
            //    {

            //        string sFrequancy = Messages[i].ToString();
            //        string sfrequ = Convert.ToString(Messages[j]);
            //        string sformattedstring = sFrequancy + sfrequ;
            //        if (sformattedstring.Contains(",,"))
            //        {
            //            sformattedstring = sformattedstring.Replace(",,", ",");
            //        }
            //        lstMessage.Add(sformattedstring);
            //        i++;
            //        j = j + 2;
            //    }
            //    catch (Exception ex)
            //    {
            //        break;
            //    }

            //}

            try
            {

                for (int x = 0; x < Messages.Length; x++)
                {
                    if (x == 0)
                    {
                        continue;
                    }
                    string ilstString = Messages[x].ToString();
                    string[] SSpiltbyCom = ilstString.Split(',');

                    CordinateValue oCordinateValue = new CordinateValue();

                    oCordinateValue.packet_type = SSpiltbyCom[2].ToString();
                    oCordinateValue.MemoryType = SSpiltbyCom[4].ToString();
                    oCordinateValue.Alert = SSpiltbyCom[2].ToString();
                    //    oCordinateValue.Signature = SSpiltbyCom[1].ToString();
                    oCordinateValue.IMEI = SSpiltbyCom[5].ToString();
                    oCordinateValue.GpsDateTime = General.GetGPSDateTime(SSpiltbyCom[8], SSpiltbyCom[9]);
                    oCordinateValue.valid = SSpiltbyCom[7].ToString();
                    string slatitude = SSpiltbyCom[10].ToString();
                    string slogtitude = SSpiltbyCom[12].ToString();

                    if (slatitude.Contains("-"))
                    {
                        slatitude = slatitude.Replace("-", " ");
                        double dlatitude = Convert.ToDouble("-" + (General.GetLatitude(slatitude)).ToString());
                        oCordinateValue.Latitude = dlatitude;


                    }
                    else
                    {
                        oCordinateValue.Latitude = Convert.ToDouble(slatitude);

                    }
                    oCordinateValue.NorthandSouth = SSpiltbyCom[11].ToString();
                    oCordinateValue.Longitude = Convert.ToDouble(slogtitude);
                    oCordinateValue.EastandWest = SSpiltbyCom[13].ToString();
                    string sSpeed = SSpiltbyCom[14].ToString();

                    oCordinateValue.Speed_knot = sSpeed;
                    oCordinateValue.Angle_of_motion = SSpiltbyCom[15].ToString();

                    oCordinateValue.Ignition = SSpiltbyCom[21].ToString();
                    oCordinateValue.MainPower = SSpiltbyCom[22].ToString();
                    oCordinateValue.Battery = Convert.ToDouble(SSpiltbyCom[24]);
                    oCordinateValue.Signal_Strength = Convert.ToInt32(SSpiltbyCom[27]);
                    oCordinateValue.Mobile_country_code = SSpiltbyCom[28].ToString();
                    oCordinateValue.Mobile_Network_code = SSpiltbyCom[29].ToString();
                    oCordinateValue.Location_area_code = SSpiltbyCom[30].ToString();
                    char[] cCharValue = SSpiltbyCom[44].ToCharArray();

                    oCordinateValue.Harshturn = Convert.ToInt16(cCharValue[1].ToString());//ac value 2march2020

                    if (cCharValue.Length >= 4)
                    {
                        oCordinateValue.LowPanic =
                            cCharValue[2].ToString();

                        oCordinateValue.HighPanic =
                            cCharValue[3].ToString();
                    }

                    try
                    {
                        oCordinateValue.Odometer = Convert.ToDouble(SSpiltbyCom[47]);
                    }
                    catch(Exception ex) 
                    {

                        General.WriteToLogFile(ex.Message, "E:/ListenerData/AtlantaNew/16141", "ODOMETER ERROR.txt");
                        //continue;
                    }




                    // oCordinateValue.Odometer = Convert.ToDouble(SSpiltbyCom[11]);
                    //   oCordinateValue.Cell_id = SSpiltbyCom[17].ToString();
                    //   string sHatshvaules = SSpiltbyCom[18].ToString();
                    //if (sHatshvaules.Contains("#"))
                    //{
                    //    char[] cCharValue = sHatshvaules.ToCharArray();
                    //    oCordinateValue.arm = cCharValue[2].ToString();
                    //    string ibrakingvalue = cCharValue[3].ToString();
                    //    switch (ibrakingvalue)
                    //    {
                    //        case "0":
                    //            oCordinateValue.Harsh_braking = ibrakingvalue;
                    //            break;

                    //        case "1":
                    //            oCordinateValue.Event = ibrakingvalue;


                    //            break;
                    //        case "2":
                    //            oCordinateValue.Acceleration_Braking = ibrakingvalue;

                    //            break;

                    //        case "3":
                    //            oCordinateValue.None = ibrakingvalue;
                    //            break;
                    //    }




                    lstCordinate.Add(oCordinateValue);

                }
                return lstCordinate;

            }
            catch (Exception ex)
            {


            }
            return lstCordinate;


        }

        // This gone decrypt obd Cordinates
        public static List<ObdCordinate> Assignvalues(string[] svalues)
        {
            List<ObdCordinate> lstvalues = new List<ObdCordinate>();

            for (int i = 0; i < svalues.Length; i++)
            {
                int iabc = 0;
                ObdCordinate oObdCordinate = new ObdCordinate();

                string sString = svalues[i].ToString();
                if (sString.Length > 20)
                {

                    string[] ssplitedString = sString.Split(',');

                    for (int j = 0; j < ssplitedString.Length; j++)
                    {
                        string sGotString = ssplitedString[j];
                        if (sGotString == "H" || sGotString == "L")
                        {
                            oObdCordinate.data_status = sGotString;
                        }
                        if (sGotString.Contains(":"))
                        {
                            DecodePid.GetdataType(sGotString, oObdCordinate);
                        }
                        if (sGotString.Contains("CAN"))
                        {
                            oObdCordinate.obdprotocol = "CAN";
                        }
                        else
                        {

                            if (sGotString.Length > 12 && !sGotString.Contains(":"))
                            {
                                oObdCordinate.imei = sGotString.ToString();
                            }
                            if (j == 3)
                            {
                                if (iabc == 0)
                                {
                                    string stime = ssplitedString[3];
                                    string sDate = ssplitedString[4];
                                    DateTime datetime = General.GetGPSDateTime(sDate, stime);
                                    oObdCordinate.gps_time = datetime;
                                }
                                iabc++;


                            }

                        }
                    }
                    lstvalues.Add(oObdCordinate);
                }
            }
            return lstvalues;
        }

    }
}
