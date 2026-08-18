using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GPSTrackerListeners.ORSAC
{
   public class PanicAlert
    {
        public string Email { get; set; }

        public string Email1 { get; set; }

        public string Email2 { get; set; }

        public string Mobile { get; set; }
        public string Mobile1 { get; set; }
        public string Mobile2 { get; set; }


        public TimeSpan FromAlertTime { get; set; }

        public TimeSpan ToAlertTime { get; set; }

    }
}
