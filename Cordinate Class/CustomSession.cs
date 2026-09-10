using SuperSocket.SocketBase;
using SuperSocket.SocketBase.Protocol;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Data;
using System.Timers;
using System.Net.Sockets;

namespace GPSTrackerListeners.ORSAC
{
    public class Stop
    {
        public int RouteId { get; set; }
        public string StopName { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string Via { get; set; }
        public int StopOrder { get; set; } 
    }
    public class DTCCode
    {
        public int Id               { get; set; }
        public string Code          { get; set; }
        public string Description   { get; set; }
    }
    public class CustomSession : AppSession<CustomSession>
    {
        protected override void HandleUnknownRequest(StringRequestInfo requestInfo)
        {

        }

        public string ServiceId { get; set; } = null;

        public string PrevLatitude { get; set; }

        public string PrevLongitude { get; set; }

        public bool IsPortUpdated { get; set; }

        public DateTime RequestTime { get; set; }

        public DateTime ResponseTime { get; set; }

        public bool IsTimeout { get; set; }

        public const int TimeoutPeriod = 2; // Minutes

        public bool IsResponsePending { get; set; }

        public string CommandRequestId { get; set; }

        public string IMEI { get; set; }

        public PanicAlert PanicAlert { get; set; }

        public bool IsPanicAlert { get; set; }

        public TcpClient TcpClient { get; set; }

        public int UTCOffset { get; set; }

        public int UserId { get; set; }

        public string VehicleName { get; set; }

        public bool IsAlertCheck { get; set; }

        public string Protocol { get; set; }

        public Dictionary<string, string> RFDetail { get; set; }

        public double FuelValue { get; set; }

        public double OdometerOffset { get; set; }

        public double LastOdometer { get; set; }
        
        public double LastLat { get; set; }

        public double LastLng { get; set; }

        public string MobileNo { get; set; }

        public byte IsODOmeterReset { get; set; }

        public DateTime LastGPSTime { get; set; }

        public int PanicCount { get; set; }

        public byte IsInside { get; set; }
        public int TripId { get; set; }
        public List<Stop> stops { get; set; }
        public int IsSend = 0;
        public int command_id { get; set; }

        public List<DTCCode> lstDtcCode { get; set; }

        /// <summary>
        /// This session's own connection to the raw traffic mirror. Created lazily on the
        /// first inbound packet and closed when the session closes. Never affects the
        /// session's normal processing.
        /// </summary>
        internal RawDataMirror RawMirror { get; set; }


        protected override void OnSessionStarted()
        {
            base.OnSessionStarted();
            FuelValue = -1;
            OdometerOffset = -1;
            LastOdometer = -1;

            LastLat = -1;
            LastLng = -1;

            IsODOmeterReset = 0;
            LastGPSTime = new DateTime(1900, 1, 1);
            IMEI = "";
        }

        public DateTime LastPanicTime { get; set; }

        public bool IsPanicSent { get; set; }
    }
}
