using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FSXRichPresenceApp
{
    class AircraftParse
    {
        private static Dictionary<String, String> aircraftMap = new Dictionary<String, String>();

        public AircraftParse()
        {
            aircraftMap.Add("b739", "b737");
            aircraftMap.Add("b738", "b737");
            aircraftMap.Add("b736", "b737");
            aircraftMap.Add("a321", "a320");
            aircraftMap.Add("a319", "a320");
            aircraftMap.Add("a318", "a320");
            aircraftMap.Add("a318cfm", "a320");
            aircraftMap.Add("a319cfm", "a320");
            aircraftMap.Add("a318iae", "a320");
            aircraftMap.Add("a319iae", "a320");
            aircraftMap.Add("a320cfm", "a320");
            aircraftMap.Add("a321cfm", "a320");
            aircraftMap.Add("a320iae", "a320");
            aircraftMap.Add("a321iae", "a320");
            aircraftMap.Add("b77w", "b777");
            aircraftMap.Add("b77l", "b777");
        }

        public String parsePlane(String key)
        {
            if(key.Length > 32)
            {
                key = key.Substring(0, 32);
            }

            String value;


            if(aircraftMap.TryGetValue(key, out value))
            {
                return value;
            } else {
                return key;
            }
        }
    }
}
