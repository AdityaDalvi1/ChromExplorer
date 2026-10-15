using System.Collections.ObjectModel;

namespace Serial.Utils
{
    public class BaudRateDescriptor
    {
        //1200, 2400, 4800, 9600, 14400, 19200, 38400, 57600 and 115200 
        static BaudRateDescriptor()
        {
            BaudRates = new ObservableCollection<BaudRateDescriptor>
                            {
                                new BaudRateDescriptor() {DisplayName = "1200", BaudRate = 1200},
                                new BaudRateDescriptor() {DisplayName = "2400", BaudRate = 2400},
                                new BaudRateDescriptor() {DisplayName = "4800", BaudRate = 4800},
                                new BaudRateDescriptor() {DisplayName = "9600", BaudRate = 9600},
                                new BaudRateDescriptor() {DisplayName = "14400", BaudRate = 14400},
                                new BaudRateDescriptor() {DisplayName = "19200", BaudRate = 19200},
                                new BaudRateDescriptor() {DisplayName = "38400", BaudRate = 38400},
                                new BaudRateDescriptor() {DisplayName = "57600", BaudRate = 57600},
                                new BaudRateDescriptor() {DisplayName = "115200", BaudRate = 115200}
                            };

            DefaultValue = BaudRates[6];
        }

        public string DisplayName { get; set; }
        public int BaudRate { get; set; }

        public static ObservableCollection<BaudRateDescriptor> BaudRates { get; private set; }

        public static BaudRateDescriptor DefaultValue { get; private set; }

        public override bool Equals(System.Object obj)
        {
            // If parameter is null return false.
            if (obj == null)
            {
                return false;
            }

            // If parameter cannot be cast to Point return false.
            BaudRateDescriptor p = obj as BaudRateDescriptor;
            if ((System.Object) p == null)
            {
                return false;
            }

            return BaudRate.Equals(p.BaudRate);
        }

        public override int GetHashCode()
        {
            return BaudRate.GetHashCode();
        }
    }
}
