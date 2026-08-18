using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GPSTrackerListeners.ORSAC
{
    public class DecodePid
    {
        public static void GetdataType(string PiDtype, ObdCordinate oObdCordinate)
        {
            try
            {
                string[] sResponceString = PiDtype.Split(':');
                string sHasValue = Convert.ToString(sResponceString[1]);
                if (!string.IsNullOrEmpty(sHasValue))
                {
                    switch (sResponceString[0])
                    {
                        case "0101":
                            if (!sHasValue.Contains("XXXX"))
                            {

                                for (int i = 0; i < 4; i++)
                                {
                                    string sSub = sResponceString[1].Substring(i + i, 2);
                                    string bitCode = ConvertHexToBinary(sSub);
                                    char[] c = bitCode.ToCharArray();
                                    if (i == 0)
                                    {
                                        if (c[0].ToString() == "1" || c[0].ToString() == "0")
                                        {
                                            oObdCordinate.mil = Convert.ToInt16((c[0].ToString()));
                                            oObdCordinate.dtc_cnt = Convert.ToInt32(bitCode.Substring(1), 2);
                                        }
                                    }
                                    if (i == 1)
                                    {
                                        oObdCordinate.ignition_type = Convert.ToInt16(c[4].ToString());
                                    }
                                }
                            }

                            break;

                        case "0103":
                            string FuelSystem = Convert.ToString(sResponceString[1]);
                            if (!FuelSystem.Contains("XX"))
                            {


                                int fuelStatus1 = string.IsNullOrEmpty(sResponceString[0]) ? -1 : Convert.ToInt32(ConvertHexToDecimal(sResponceString[1].Substring(0, 2)));

                                if (!new int[] { 1, 2, 4, 8, 16 }.Contains(fuelStatus1))
                                    fuelStatus1 = -1;
                                oObdCordinate.fuel_system_status1 = fuelStatus1;

                                int fuelStatus2 = string.IsNullOrEmpty(sResponceString[0]) ? -1 : Convert.ToInt32(ConvertHexToDecimal(sResponceString[1].Substring(2, 2)));
                                if (!new int[] { 1, 2, 4, 8, 16 }.Contains(fuelStatus2))
                                    fuelStatus2 = -1;
                                oObdCordinate.fuel_system_status2 = fuelStatus2;
                            }
                            else
                            {
                                oObdCordinate.fuel_system_status1 = -1;
                                oObdCordinate.fuel_system_status2 = -1;
                            }
                            break;
                        case "0104":
                            // for engine Load 
                            string sengineLoad = sResponceString[1].ToString();
                            if (!sengineLoad.Contains("XX"))
                            {
                                double engineLoad = string.IsNullOrEmpty(sResponceString[0]) ? -1 : Convert.ToDouble((ConvertHexToDecimal(sResponceString[1])));
                                engineLoad = Math.Round(((engineLoad / 255) * 100), 3);
                                if (engineLoad < 0 || engineLoad > 100)
                                    engineLoad = -1;
                                oObdCordinate.engine_load = engineLoad;
                            }
                            else
                            {
                                oObdCordinate.engine_load = -1;
                            }
                            break;

                        case "0105":
                            string scollant = Convert.ToString(sResponceString[1]);
                            if (!scollant.Contains("XX"))
                            {
                                int CollantTemp = Convert.ToInt32(ConvertHexToDecimal(sResponceString[1]));
                                CollantTemp = CollantTemp - 40;
                                if (CollantTemp < -40 || CollantTemp > 215)
                                    CollantTemp = -1;
                                oObdCordinate.engine_coolant_temp = CollantTemp;
                            }
                            else
                            {
                                oObdCordinate.engine_coolant_temp = -1;


                            }
                            break;

                        case "010A":
                            string sfullpresure = sResponceString[1].ToString();
                            if (!sfullpresure.Contains("XX"))
                            {
                                int FullPresure = Convert.ToInt32(ConvertHexToDecimal(sResponceString[1]));

                                FullPresure = FullPresure * 3;
                                if (FullPresure < 0 || FullPresure > 765)
                                {
                                    FullPresure = -1;
                                }
                                oObdCordinate.fuel_presure = FullPresure;
                            }
                            else
                            {
                                oObdCordinate.fuel_presure = -1;
                            }
                            break;

                        case "010B":
                            string sabsolutePresure = Convert.ToString(sResponceString[1]);
                            if (!sabsolutePresure.Contains("XX"))
                            {
                                int absolutePresure = Convert.ToInt32(ConvertHexToDecimal(sResponceString[1]));
                                if (absolutePresure < 0 || absolutePresure > 255)
                                {
                                    absolutePresure = -1;
                                }
                                oObdCordinate.intake_manifold = absolutePresure;
                            }
                            else
                            {
                                oObdCordinate.intake_manifold = -1;
                            }

                            break;
                        case "010C":
                            string Enginerpm = sResponceString[1].ToString();
                            if (!Enginerpm.Contains("XX"))
                            {
                                double enginerpm1 = Convert.ToDouble(ConvertHexToDecimal(sResponceString[1].Substring(0, 2)));
                                double enginerpm2 = Convert.ToDouble(ConvertHexToDecimal(sResponceString[1].Substring(2, 2)));
                                double TotalRPM = Math.Round((((256 * enginerpm1) + enginerpm2) / 4), 2);
                                if (TotalRPM < 0 || TotalRPM > 16383.76)
                                {
                                    TotalRPM = -1;
                                }
                                oObdCordinate.engine_rpm = TotalRPM;
                            }
                            else
                            {
                                oObdCordinate.engine_rpm = -1;

                            }
                            break;

                        case "010D":
                            string sVehicalSpeed = Convert.ToString(sResponceString[1]);
                            if (!sVehicalSpeed.Contains("XX"))
                            {
                                int VehiclesSpeed = ConvertHexToDecimal(sResponceString[1]);
                                if (VehiclesSpeed < 0 || VehiclesSpeed > 255)
                                {
                                    VehiclesSpeed = -1;
                                }
                                oObdCordinate.vehicle_speed = VehiclesSpeed;
                            }

                            else
                            {
                                oObdCordinate.vehicle_speed = -1;

                            }

                            break;
                        case "010E":
                            string TimingAdvance = Convert.ToString(sResponceString[1]);
                            if (!TimingAdvance.Contains("XX"))
                            {
                                double timingAdvance = string.IsNullOrEmpty(sResponceString[1]) ? -1 : ConvertHexToDecimal(sResponceString[1]);

                                timingAdvance = Math.Round((timingAdvance / 2) - 64, 2);
                                if (timingAdvance < -64 || timingAdvance > 64)
                                    oObdCordinate.time_advance = timingAdvance;
                            }
                            else
                            {
                                oObdCordinate.time_advance = -1;
                            }
                            break;

                        case "010F":
                            string IntakeAirTemp = sResponceString[1];
                            if (!IntakeAirTemp.Contains("XX"))
                            {
                                int? intakeAirTemp = string.IsNullOrEmpty(sResponceString[1]) ? -1 : ConvertHexToDecimal(sResponceString[1]);
                                intakeAirTemp -= 40;
                                if (intakeAirTemp < -40 || intakeAirTemp > 216)
                                    intakeAirTemp = -1;
                                oObdCordinate.intake_air_temepreture = intakeAirTemp.ToString();
                            }
                            else
                            {
                                oObdCordinate.intake_air_temepreture = "-1";
                            }
                            break;
                        case "0110":
                            string Airflow = sResponceString[1].ToString();
                            if (!Airflow.Contains("XX"))
                            {
                                double airflow1 = string.IsNullOrEmpty(sResponceString[1]) ? -1 : ConvertHexToDecimal(sResponceString[1].Substring(0, 2));
                                double airflow2 = string.IsNullOrEmpty(sResponceString[1]) ? -1 : ConvertHexToDecimal(sResponceString[1].Substring(2, 2));
                                double totalairflowrate = (256 * airflow1 + airflow2) / 100;
                                totalairflowrate = Math.Round(totalairflowrate, 2);
                                if (totalairflowrate < 0 || totalairflowrate > 656)
                                    totalairflowrate = -1;
                                oObdCordinate.maf_air_flow_rate = totalairflowrate;
                            }
                            else
                            {
                                oObdCordinate.maf_air_flow_rate = -1;

                            }


                            break;
                        case "0111":
                            string strolledPosition = sResponceString[1];
                            if (!strolledPosition.Contains("XX"))
                            {
                                double trolledposition = string.IsNullOrEmpty(sResponceString[1]) ? -1 : ConvertHexToDecimal(sResponceString[1]);
                                trolledposition = Math.Round((100 / 255) * trolledposition, 2);
                                if (trolledposition < 0 || trolledposition > 100)
                                    trolledposition = -1;
                                oObdCordinate.trolled_position = trolledposition;

                            }
                            else
                            {
                                oObdCordinate.trolled_position = -1;

                            }

                            break;

                        case "011C":
                            //ObdStrand 

                            string Obdstandard = sResponceString[1];
                            if (!Obdstandard.Contains("XX"))
                            {


                                int? OBDStandard = string.IsNullOrEmpty(sResponceString[1]) ? -1 : ConvertHexToDecimal(sResponceString[1]);
                                if (OBDStandard < 0 || OBDStandard > 255)
                                    OBDStandard = -1;
                                oObdCordinate.obd_standard_ID = Convert.ToInt32(OBDStandard);
                            }
                            else
                            {
                                oObdCordinate.obd_standard_ID = -1;
                            }
                            break;
                        case "011F":

                            string runTime = sResponceString[1];

                            if (!runTime.Contains("XX"))
                            {

                                int runTime1 = string.IsNullOrEmpty(sResponceString[1]) ? -1 : ConvertHexToDecimal(runTime.Substring(0, 2));
                                int runTime2 = string.IsNullOrEmpty(sResponceString[1]) ? -1 : ConvertHexToDecimal(runTime.Substring(2, 2));

                                int? totalRunTime = 256 * runTime1 + runTime2;
                                if (totalRunTime < 0 || totalRunTime > 65535)
                                    totalRunTime = -1;
                                oObdCordinate.run_time_since_engine_start = Convert.ToInt32(totalRunTime);
                            }
                            else
                            {
                                oObdCordinate.run_time_since_engine_start = -1;
                            }
                            break;
                        case "0121":

                            string distanceWithLightOn = sResponceString[1];
                            if (!distanceWithLightOn.Contains("XX"))
                            {
                                int distanceWithLightOn1 = string.IsNullOrEmpty(sResponceString[1]) ? -1 : ConvertHexToDecimal(distanceWithLightOn.Substring(0, 2));
                                int distanceWithLightOn2 = string.IsNullOrEmpty(sResponceString[1]) ? -1 : ConvertHexToDecimal(distanceWithLightOn.Substring(2, 2));

                                int? totaldistanceWithLightOn = 256 * distanceWithLightOn1 + distanceWithLightOn2;
                                if (totaldistanceWithLightOn < 0 || totaldistanceWithLightOn > 65535)
                                    totaldistanceWithLightOn = -1;
                                oObdCordinate.distance_travelled_with_mil_on = Convert.ToInt32(totaldistanceWithLightOn);
                            }
                            else
                            {
                                oObdCordinate.distance_travelled_with_mil_on = -1;
                            }
                            

                            break;
                        case "0122":

                            string fuelrailpresure = sResponceString[1];
                            if (!fuelrailpresure.Contains("XX"))
                            {


                                int FuelrailPresure1 = string.IsNullOrEmpty(sResponceString[1]) ? -1 : ConvertHexToDecimal(fuelrailpresure.Substring(0, 2));
                                int FuelrailPresure2 = string.IsNullOrEmpty(sResponceString[1]) ? -1 : ConvertHexToDecimal(fuelrailpresure.Substring(2, 2));
                                double Totalrailpresure = 0.079 * (256 * FuelrailPresure1 + FuelrailPresure2);
                                Totalrailpresure = Math.Round(Totalrailpresure, 2);
                                if (Totalrailpresure > 0 || Totalrailpresure < 5178)
                                {
                                    Totalrailpresure = -1;
                                }
                                oObdCordinate.fuel_rail_gauge_presure = Totalrailpresure;
                            }
                            else
                            {
                                oObdCordinate.fuel_rail_gauge_presure = -1;
                            }

                            break;
                        case "012F":
                            string sTimingAdvance = Convert.ToString(sResponceString[1]);
                            if (!sTimingAdvance.Contains("XX"))
                            {

                                double fueltanklevel = string.IsNullOrEmpty(sResponceString[1]) ? -1 : Convert.ToDouble(ConvertHexToDecimal(sResponceString[1]));
                                double dFueltankLevel = (100 / 255) * fueltanklevel;
                                dFueltankLevel = Math.Round(dFueltankLevel, 2);
                                if (dFueltankLevel < 0 || dFueltankLevel > 100)
                                    dFueltankLevel = -1;
                                oObdCordinate.fule_tank_level_input = dFueltankLevel;
                            }
                            else
                            {
                                oObdCordinate.fule_tank_level_input = -1;
                            }
                            break;
                        case "0132":
                            string svaporpasour = sResponceString[1].ToString();
                            if (!svaporpasour.Contains("XX"))
                            {

                                string SystemVapourPressure = sResponceString[1];
                                double SystemVapourPressure1 = string.IsNullOrEmpty(sResponceString[1]) ? -1 : ConvertHexToDecimal(SystemVapourPressure.Substring(0, 2));
                                double SystemVapourPressure2 = string.IsNullOrEmpty(sResponceString[1]) ? -1 : ConvertHexToDecimal(SystemVapourPressure.Substring(2, 2));

                                double totalSystemVapourPressure = string.IsNullOrEmpty(sResponceString[1]) ? -1 : Math.Round((256 * SystemVapourPressure1 + SystemVapourPressure2) / 4, 2);
                                if (totalSystemVapourPressure < -8193 || totalSystemVapourPressure > 8192)
                                    totalSystemVapourPressure = -1;
                                oObdCordinate.evap_system_vapor_pressure = totalSystemVapourPressure;
                            }
                            else
                            {
                                oObdCordinate.evap_system_vapor_pressure = -1;
                            }
                            break;
                        case "0133":
                            string sbarometric  = sResponceString[1];
                            if (!sbarometric.Contains("XX"))
                            {
                                int? barometricPressure = string.IsNullOrEmpty(sResponceString[1]) ? -1 : ConvertHexToDecimal(sResponceString[1]);
                                if (barometricPressure < 0 || barometricPressure > 255)
                                    barometricPressure = -1;
                                oObdCordinate.absolute_barometeric_pressure = Convert.ToInt32(barometricPressure);
                            }
                            else
                            {
                                oObdCordinate.absolute_barometeric_pressure = -1;
                            }

                          

                            break;
                        case "0143":
                            string loadValue = sResponceString[1];
                            if (!loadValue.Contains("XX"))
                            {


                                double loadValue1 = string.IsNullOrEmpty(sResponceString[1]) ? -1 : ConvertHexToDecimal(loadValue.Substring(0, 2));
                                double loadValue2 = string.IsNullOrEmpty(sResponceString[1]) ? -1 : ConvertHexToDecimal(loadValue.Substring(2, 2));

                                double totalloadValue = ((256 * loadValue1 + loadValue2) * 0.392);
                                totalloadValue = Math.Round(totalloadValue, 2);
                                if (totalloadValue < 0 || totalloadValue > 25700)
                                    totalloadValue = -1;
                                oObdCordinate.absolute_load_value = totalloadValue;
                            }
                            else
                            {
                                oObdCordinate.absolute_load_value = -1;
                            }
                            break;
                        case "0145":
                            string relativethr = sResponceString[1];
                            if (!relativethr.Contains("XX"))
                            {


                                double relativeThrottlePos = string.IsNullOrEmpty(sResponceString[1]) ? -1 : Convert.ToDouble(ConvertHexToDecimal(sResponceString[1]));
                                relativeThrottlePos = Math.Round((100 / 255) * relativeThrottlePos, 2);
                                if (relativeThrottlePos < 0 || relativeThrottlePos > 100)
                                    relativeThrottlePos = -1;
                                oObdCordinate.relative_throttle_postion = relativeThrottlePos;
                            }
                            else
                            {
                                oObdCordinate.relative_throttle_postion = -1;
                            }
                            break;

                        case "0146":
                            string sAmbitentAirTempreture = sResponceString[1];
                            
                            if (!sAmbitentAirTempreture.Contains("XX"))
                            {
                                int? ambientAirTemp = string.IsNullOrEmpty(sResponceString[1]) ? -1 : ConvertHexToDecimal(sResponceString[1]);
                                ambientAirTemp -= 40;
                                if (ambientAirTemp < -40 || ambientAirTemp > 216)
                                    ambientAirTemp = -1;
                                oObdCordinate.ambient_air_temperature = Convert.ToInt32(ambientAirTemp);

                            }
                            else
                            {
                                oObdCordinate.ambient_air_temperature = -1;
                            }
                            break;
                        case "0147":
                            string throttleposition = sResponceString[1];
                            if (!throttleposition.Contains("XX"))
                            {
                                double absoluteThrottlePosB = string.IsNullOrEmpty(sResponceString[1]) ? -1 : Convert.ToDouble(ConvertHexToDecimal(sResponceString[1]));
                                absoluteThrottlePosB = Math.Round((100 * 255) / absoluteThrottlePosB, 2);
                                if (absoluteThrottlePosB < 0 || absoluteThrottlePosB > 100)
                                    absoluteThrottlePosB = -1;
                                oObdCordinate.absolute_throttle_position_B = absoluteThrottlePosB;
                            }
                            else
                            {
                                oObdCordinate.absolute_throttle_position_B = -1;
                            }



                            break;
                        case "0148":
                            string sthrottleposition = sResponceString[1];
                            if (!sthrottleposition.Contains("XX"))
                            {
                                double absoluteThrottlePosC = string.IsNullOrEmpty(sResponceString[1]) ? -1 : Convert.ToDouble(ConvertHexToDecimal(sResponceString[1]));
                                absoluteThrottlePosC = Math.Round((absoluteThrottlePosC * 255) / 100, 2);
                                if (absoluteThrottlePosC < 0 || absoluteThrottlePosC > 100)
                                    absoluteThrottlePosC = -1;
                                oObdCordinate.absolute_throttle_position_C = absoluteThrottlePosC;

                            }
                            else
                            {
                                oObdCordinate.absolute_throttle_position_C = -1;

                            }
                            break;
                        case "014B":

                            string PedalPos = Convert.ToString(sResponceString[1]);
                            if (!PedalPos.Contains("XX"))
                            {
                                double pedalPosF = string.IsNullOrEmpty(sResponceString[1]) ? -1 : Convert.ToDouble(ConvertHexToDecimal(sResponceString[1]));
                                pedalPosF = Math.Round((pedalPosF * 255) / 100, 2);
                                if (pedalPosF < 0 || pedalPosF > 100)
                                    pedalPosF = -1;
                            }
                            else
                            {
                                // pedalPosF = -1;
                            }
                            break;
                        case "014C":
                            string sthrottleActuator = sResponceString[1];
                            if (!sthrottleActuator.Contains("XX"))
                            {
                                double throttleActuator = string.IsNullOrEmpty(sResponceString[1]) ? -1 : Convert.ToDouble(ConvertHexToDecimal(sResponceString[1]));
                                throttleActuator = Math.Round((throttleActuator * 255) / 100, 2);
                                if (throttleActuator < 0 || throttleActuator > 100)
                                    throttleActuator = -1;
                                oObdCordinate.Commanded_throttle_actuator = throttleActuator;
                            }
                            else
                            {
                                oObdCordinate.Commanded_throttle_actuator = -1;

                            }

                            break;
                        case "0151":
                            string sfullType = sResponceString[1].ToString();
                            if (!sfullType.Contains("XX"))
                            {


                                int? fuelType = string.IsNullOrEmpty(sResponceString[1]) ? -1 : ConvertHexToDecimal(sResponceString[1]);
                                if (fuelType < 0 || fuelType > 23)
                                    fuelType = -1;
                                oObdCordinate.fuel_type_id = Convert.ToInt32(fuelType);
                            }
                            else
                            {
                                oObdCordinate.fuel_type_id = -1;

                            }
                            break;


                    }
                }
            }
            catch (Exception ex)
            {


            }

        }
        public static string ConvertHexToBinary(string hexValue)
        {

            try
            {
                return Convert.ToString(Convert.ToInt32(hexValue, 16), 2).PadLeft(hexValue.Length * 4, '0');

            }
            catch (Exception ex)
            {

            }
            return "";
        }
        public static int ConvertHexToDecimal(string sResponce) { return Convert.ToInt32(sResponce, 16); }
    }
}

