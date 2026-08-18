using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GPSTrackerListeners.ORSAC
{
   public class CordinateValue
    {
        public string MemoryType  {get;set;}

        public string packet_type { get; set; }
       
       public string Signature { get; set; }
       
       public string IMEI { get; set; }
       
       public  DateTime GpsDateTime { get; set; }
       
       public string valid { get; set; }
       
       public double Latitude { get; set; }

       
       public string NorthandSouth { get; set; }
       
       public double Longitude { get; set; }
       
       public string EastandWest { get; set; }
       
       public string Speed_knot { get; set; }
       
       public string Angle_of_motion { get; set; }
       

       public double Odometer { get; set; }
       
       public double  Battery { get; set; }
       
       public int  Signal_Strength { get; set; }

        public string Mobile_country_code { get; set; }

       
       public string Mobile_Network_code { get; set; }
      
       public string Location_area_code { get; set; }

        public string Cell_id { get; set; }
        public int Harshturn { get; set; }

        public string Ignition { get; set; }

        public string arm { get; set; }

        public string Harsh_braking { get; set; }

        public string Acceleration_Braking { get; set; }

        public string Event = "0";

       public string None = "0";

       public string Battery_status = "1";
       public string MainPower = "1";
        public string Alert { get; set; }

        public string LowPanic { get; set; }

        public string HighPanic { get; set; }
        public string TemperAlert { get; set; }
      
    }
   public class ObdCordinate
   {
       public int sys_service_id { get; set; }
       public string imei { get; set; }
       public DateTime gps_time { get; set; }
       public string data_status { get; set; }
       public string obdprotocol { get; set; }
       public int mil =  -1;
       public int dtc_cnt = -1;
       public int ignition_type  = -1;
       public int monitor_type =  -1;
       public int fuel_system_status1 { get; set; }
       public int fuel_system_status2 { get; set; }
       public double engine_load { get; set; }
       public int engine_coolant_temp { get; set; }
       public int    fuel_presure { get; set; }
       public int intake_manifold { get; set; }
       public double engine_rpm { get; set; }
       public int  vehicle_speed { get; set; }
       public double time_advance { get; set; }
       public string intake_air_temepreture { get; set; }
       public double maf_air_flow_rate { get; set; }
       public double trolled_position { get; set; }

       public int obd_standard_ID { get; set; }
       public int run_time_since_engine_start { get; set; }
       public int distance_travelled_with_mil_on { get; set; }
       public double fuel_rail_presure { get; set; }
       public double fuel_rail_gauge_presure { get; set; }

       public double fule_tank_level_input { get; set; }
       public double evap_system_vapor_pressure { get; set; }

       public int absolute_barometeric_pressure { get; set; }
       public double absolute_load_value { get; set; }
       public double relative_throttle_postion { get; set; }
       public int ambient_air_temperature { get; set; }
       public double absolute_throttle_position_B { get; set; }
       public double absolute_throttle_position_C { get; set; }
       public double absolute_throttle_position_D { get; set; }
       public double absolute_throttle_position_E { get; set; }
       public double absolute_throttle_position_F { get; set; }

       public double Commanded_throttle_actuator { get; set; }
       public int fuel_type_id { get; set; }
       public double relative_accelerator_pedal_position { get; set; }
       public double hybrid_battery_pack_remaining_life { get; set; }
   
       public int Engine_oil_temperature { get; set; }
       public double fuel_injection_timing { get; set; }
       public double engine_fuel_rate { get; set; }

       public int driver_demand_engine_percent_torque { get; set; }
       public int actual_engine_percent_torque { get; set; }
       public int engine_reference_torque { get; set; }
       public int engine_percent_torque_data_idle { get; set; }
       public int engine_percent_torque_data_point1 { get; set; }
       public int engine_percent_torque_data_point2 { get; set; }

       public int engine_percent_torque_data_point3 { get; set; }
       public int engine_percent_torque_data_point4 { get; set; }
       public string fuel_mileage { get; set; }
       public double fuel_consumption { get; set; }
   
   
   }
   
  
}
