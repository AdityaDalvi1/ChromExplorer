using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Modas.Grpc.Models
{
    public class DsapHeaterConfig 
    {
        public double DsapPower { get; set; }
        public bool AutoEnabled { get; set; }

        public bool IsVisible { get; set; } = true;

        public bool Enabled { get; set; }

        public int MaxEnabledDurationSec { get; set; } = 600;

      
       
    }
}

