using System;
using Meadow.Foundation;

namespace NewCode {
    class Data {

        //WEB VARIABLES
        public static string IP = null;
        public static int Port = 2550;

        //ROUND VARIABLES
        public static string[] temp_max = { "18", "30" }; // In ºC
        public static string[] temp_min = { "16", "12" }; // In ºC
        public static int display_refresh = 1000; // In ms
        public static int refresh = 1000; // In ms
        public static String[] round_time = { "10", "10" }; // in s
        public static int current_round = 0;

        //START ROUND VARIABLES
        public static bool is_working = false;
        public static string temp_act = "0"; // In ºC
        public static int time_left; // in s
        public static int time_in_range_temp = 0; //In ms.
                                           
        //COLORS FOR DISPLAY
        public static Color[] colors = new Color[4]
        {
            Color.FromHex("#67E667"),
            Color.FromHex("#00CC00"),
            Color.FromHex("#269926"),
            Color.FromHex("#008500")
        };

        //RELAY VARIABLES
        public static bool on = true; // enfriando
    }
}
