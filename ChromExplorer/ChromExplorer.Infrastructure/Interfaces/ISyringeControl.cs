using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChromExplorer.Infrastructure.Interfaces
{
    public interface ISyringeControl : INotifyPropertyChanged
    {
        bool IsConnected { get; }

        bool IsRunning { get; }

        void VerifyRequest();

        void Write(string msg);

        void Connect();

        void Start(double diameter, double rate, double volume);

        void Stop();
    }
}
