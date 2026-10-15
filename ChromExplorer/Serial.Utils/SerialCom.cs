// Decompiled with JetBrains decompiler
// Type: Serial.Utils.SerialCom
// Assembly: Serial.Utils, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 5208FE5E-AD16-434C-933A-D4CBFD049132
// Assembly location: C:\Program Files (x86)\MassTech\Modas-MTE30\Serial.Utils.dll

using Microsoft.Win32.SafeHandles;
using System;
using System.IO;
using System.IO.Ports;
using System.Runtime.InteropServices;
using System.Text;
using System.Timers;

namespace Serial.Utils
{

    public class SerialCom : IDisposable
    {

        private readonly System.Timers.Timer _timer;
        #region Fields

        /// <summary>
        /// The baud rate at which the communications device operates.
        /// </summary>
        public int BaudRate { get; set; }


        /// <summary>
        /// The number of bits in the bytes to be transmitted and received.
        /// </summary>
        public byte DataBits { get; set; }


        /// <summary>
        /// The system handle to the serial port connection ('file' handle).
        /// </summary>
        private SafeFileHandle _handle;


        /// <summary>
        /// The parity scheme to be used.
        /// </summary>
        public Parity Parity { get; set; }


        /// <summary>
        /// The name of the serial port to connect to.
        /// </summary>
        public string PortName { get; set; }


        /// <summary>
        /// The number of bits in the bytes to be transmitted and received.
        /// </summary>
        public StopBits StopBits { get; set; }

        #endregion

        public bool IsOpen { get; private set; }

        public event EventHandler<EventArgs> DataReceived;

        protected virtual void OnDataReceived(EventArgs e)
        {
            var handler = DataReceived;
            if (handler != null) handler(this, e);

        }

        public SerialCom(string portName, int baudRate, StopBits stopBits, Parity parity, byte byteSize)
        {
            if (stopBits == StopBits.None)
                throw new ArgumentException("stopBits cannot be StopBits.None", "stopBits");

            if (byteSize < 5 || byteSize > 8)
                throw new ArgumentOutOfRangeException("The number of data bits must be 5 to 8 bits.", "byteSize");

            if (baudRate < 110 || baudRate > 256000)
                throw new ArgumentOutOfRangeException("Invalid baud rate specified.", "baudRate");

            if ((byteSize == 5 && stopBits == StopBits.Two) || (stopBits == StopBits.OnePointFive && byteSize > 5))
                throw new ArgumentException("The use of 5 data bits with 2 stop bits is an invalid combination, " +
                                            "as is 6, 7, or 8 data bits with 1.5 stop bits.");


            this.PortName = portName;
            this.BaudRate = baudRate;
            this.DataBits = byteSize;
            this.StopBits = stopBits;
            this.Parity = parity;
            IsOpen = false;

            _timer = new Timer();
            _timer.Elapsed += new ElapsedEventHandler(OnTimedEvent);
            _timer.Interval = 100;
            _timer.Enabled = false;
        }

        private int _reported = -1;
        private void OnTimedEvent(object sender, ElapsedEventArgs e)
        {
            if (_handle.IsInvalid || _handle.IsClosed)
                return;
            int bytes = BytesToRead;

            if (bytes > 0 && bytes != _reported)
            {
                _reported = bytes;
                OnDataReceived(EventArgs.Empty);
            }

        }


        public SerialCom(string portName, int baudRate, StopBits stopBits, Parity parity)
            : this(portName, baudRate, stopBits, parity, 8)
        {
        }

        public SerialCom()
        {
            DataBits = 8;
        }


        public void Dispose()
        {
            _timer.Enabled = false;
            _timer.Dispose();


            if (_handle != null)
            {

                _handle.Close();
                //Thread.Sleep(3000);

            }
        }


        /// <summary>
        /// Flushes the serial I/O buffers.
        /// </summary>
        /// <returns>Whether or not the operation succeeded</returns>
        public bool Flush()
        {
            FailIfNotConnected();
            const int PURGE_RXCLEAR = 0x0008; // input buffer
            const int PURGE_TXCLEAR = 0x0004; // output buffer
            return PurgeComm(_handle, PURGE_RXCLEAR | PURGE_TXCLEAR);
        }


        /// <summary>
        /// Opens and initializes the serial connection.
        /// </summary>
        /// <returns>Whether or not the operation succeeded</returns>
        public bool Open()
        {
            _handle = CreateFile(@"\\.\" + this.PortName, FileAccess.ReadWrite, FileShare.None,
                                IntPtr.Zero, FileMode.Open, 0, IntPtr.Zero);

            //if (handle == IntPtr.Zero || handle < IntPtr.Zero) return false;
            if (!_handle.IsInvalid)
            {
                if (ConfigureSerialPort())
                {
                    IsOpen = true;
                    _timer.Enabled = true;
                    return true;
                }
                else
                {

                    Dispose();
                    return false;
                }
            }
            else
            {
                WinIoError();
            }


            return false;


        }


        public int Read(byte[] data)
        {
            FailIfNotConnected();

            if (data == null) return 0;


            int bytesRead;

            if (ReadFile(_handle, data, data.Length, out bytesRead, 0))
            {
                _reported = -1;
                return bytesRead;
            }

            return -1;
        }


        public string ReadString(int maxBytesToRead)
        {
            if (maxBytesToRead < 1) throw new ArgumentOutOfRangeException("maxBytesToRead");


            byte[] bytes = new byte[maxBytesToRead];

            int numBytes = Read(bytes);

            string data = ASCIIEncoding.ASCII.GetString(bytes, 0, numBytes);

            return data;
        }


        public int Write(byte[] data)
        {
            FailIfNotConnected();

            if (data == null) return 0;
            int bytesWritten;
            if (WriteFile(_handle, data, data.Length, out bytesWritten, 0))
                return bytesWritten;
            return -1;
        }


        public int Write(string data)
        {
            FailIfNotConnected();

            // convert the string to bytes

            byte[] bytes;

            if (data == null) bytes = null;

            else bytes = ASCIIEncoding.ASCII.GetBytes(data);

            return Write(bytes);
        }

        public int WriteLine(string data)
        {
            if (data != null && !data.EndsWith("\r\n"))

                data += "\r\n";

            return Write(data);
        }

        public int BytesToRead
        {
            get
            {
                FailIfNotConnected();

                var stat = new Comstat();
                int commErrors = 0;
                if (ClearCommError(_handle, ref commErrors, ref stat))
                {
                    return (int)stat.cbInQue;
                }

                return -1;
            }
        }


        private static void WinIoError()
        {
            int errorCode = Marshal.GetLastWin32Error();
            throw new IOException(GetMessage(errorCode), MakeHrFromErrorCode(errorCode));
        }

        private static string GetMessage(int errorCode)
        {
            StringBuilder lpBuffer = new StringBuilder(0x200);
            if (
                FormatMessage(0x3200, new HandleRef(null, IntPtr.Zero), errorCode, 0, lpBuffer, lpBuffer.Capacity,
                              IntPtr.Zero) != 0)
            {
                return lpBuffer.ToString();
            }
            return "Unknown Error";
        }

        private static int MakeHrFromErrorCode(int errorCode)
        {
            return (int)(0x80070000 | (uint)errorCode);
        }

        #region Private Helpers

        private bool ConfigureSerialPort()
        {
            DCB serialConfig = new DCB();

            if (GetCommState(_handle, ref serialConfig))
            {
                // setup the DCB struct with the serial settings we need
                serialConfig.BaudRate = (uint)this.BaudRate;
                serialConfig.ByteSize = this.DataBits;
                serialConfig.fBinary = 1; // must be true
                serialConfig.fDtrControl = 1;
                // DTR_CONTROL_ENABLE "Enables the DTR line when the device is opened and leaves it on."
                serialConfig.fAbortOnError = 1; // false
                serialConfig.fTXContinueOnXoff = 0; // false
                serialConfig.fParity = 1; // true so that the Parity member is looked at

                switch (this.Parity)
                {
                    case Parity.Even:
                        serialConfig.Parity = 2;
                        break;

                    case Parity.Mark:
                        serialConfig.Parity = 3;
                        break;

                    case Parity.Odd:
                        serialConfig.Parity = 1;
                        break;

                    case Parity.Space:
                        serialConfig.Parity = 4;
                        break;

                    case Parity.None:

                    default:
                        serialConfig.Parity = 0;
                        break;
                }

                switch (this.StopBits)
                {
                    case StopBits.One:
                        serialConfig.StopBits = 0;
                        break;

                    case StopBits.OnePointFive:
                        serialConfig.StopBits = 1;
                        break;

                    case StopBits.Two:
                        serialConfig.StopBits = 2;
                        break;

                    case StopBits.None:

                    default:
                        throw new ArgumentException("stopBits cannot be StopBits.None");
                }


                if (SetCommState(_handle, ref serialConfig))
                {
                    // set the serial connection timeouts

                    COMMTIMEOUTS timeouts = new COMMTIMEOUTS();

                    timeouts.ReadIntervalTimeout = 1;
                    timeouts.ReadTotalTimeoutMultiplier = 0;
                    timeouts.ReadTotalTimeoutConstant = 0;
                    timeouts.WriteTotalTimeoutMultiplier = 0;
                    timeouts.WriteTotalTimeoutConstant = 0;

                    if (SetCommTimeouts(_handle, ref timeouts))
                        return true;
                    else return false;
                }
                else return false;
            }
            else return false;
        }

        private void FailIfNotConnected()
        {
            if (_handle.IsInvalid || _handle.IsClosed)
            {
                _timer.Enabled = false;
                IsOpen = false;
                throw new InvalidOperationException(
                    "You must be connected to the serial port before performing this operation.");
            }
        }

        #endregion

        #region Native Helpers

        // Used to get a handle to the serial port so that we can read/write to it.

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern SafeFileHandle CreateFile(string fileName,
                                                [MarshalAs(UnmanagedType.U4)] FileAccess fileAccess,
                                                [MarshalAs(UnmanagedType.U4)] FileShare fileShare,
                                                IntPtr securityAttributes,
                                                [MarshalAs(UnmanagedType.U4)] FileMode creationDisposition,
                                                int flags,
                                                IntPtr template);

        /*
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern SafeFileHandle CreateFile(string lpFileName, int dwDesiredAccess, int dwShareMode,
                                                        IntPtr securityAttrs, int dwCreationDisposition,
                                                        int dwFlagsAndAttributes, IntPtr hTemplateFile);*/

        // Used to close the handle to the serial port.

        //[DllImport("kernel32.dll", SetLastError = true)]
        //private static extern bool CloseHandle(SafeFileHandle hObject);


        /// <summary>
        /// Defines the control setting for a serial communications device.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct DCB
        {
            public int DCBlength;
            public uint BaudRate;
            public uint Flags;
            public ushort wReserved;
            public ushort XonLim;
            public ushort XoffLim;
            public byte ByteSize;
            public byte Parity;
            public byte StopBits;
            public sbyte XonChar;
            public sbyte XoffChar;
            public sbyte ErrorChar;
            public sbyte EofChar;
            public sbyte EvtChar;
            public ushort wReserved1;
            public uint fBinary;
            public uint fParity;
            public uint fOutxCtsFlow;
            public uint fOutxDsrFlow;
            public uint fDtrControl;
            public uint fDsrSensitivity;
            public uint fTXContinueOnXoff;
            public uint fOutX;
            public uint fInX;
            public uint fErrorChar;
            public uint fNull;
            public uint fRtsControl;
            public uint fAbortOnError;
        }


        // Used to get the state of the serial port so that we can configure it.

        [DllImport("kernel32.dll")]
        private static extern bool GetCommState(SafeFileHandle hFile, ref DCB lpDCB);


        // Used to configure the serial port.

        [DllImport("kernel32.dll")]
        private static extern bool SetCommState(SafeFileHandle hFile, [In] ref DCB lpDCB);


        /// <summary>
        /// Contains the time-out parameters for a communications device.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct COMMTIMEOUTS
        {
            public uint ReadIntervalTimeout;
            public uint ReadTotalTimeoutMultiplier;
            public uint ReadTotalTimeoutConstant;
            public uint WriteTotalTimeoutMultiplier;
            public uint WriteTotalTimeoutConstant;
        }


        // Used to set the connection timeouts on our serial connection.

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetCommTimeouts(SafeFileHandle hFile, ref COMMTIMEOUTS lpCommTimeouts);


        // Used to read bytes from the serial connection.

        [DllImport("kernel32.dll")]
        private static extern bool ReadFile(SafeFileHandle hFile, byte[] lpBuffer,
                                            int nNumberOfBytesToRead, out int lpNumberOfBytesRead, int lpOverlapped);


        // Used to write bytes to the serial connection.

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool WriteFile(SafeFileHandle hFile, byte[] lpBuffer,
                                             int nNumberOfBytesToWrite, out int lpNumberOfBytesWritten, int lpOverlapped);


        // Used to flush the I/O buffers.

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool PurgeComm(SafeFileHandle hFile, int dwFlags);


        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool ClearCommError(SafeFileHandle hFile, ref int lpErrors, ref Comstat lpStat);


        [StructLayout(LayoutKind.Sequential)]
        private struct Comstat
        {
            public readonly uint Flags;
            public readonly uint cbInQue;
            public readonly uint cbOutQue;
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int FormatMessage(int dwFlags, HandleRef lpSource, int dwMessageId, int dwLanguageId,
                                                StringBuilder lpBuffer, int nSize, IntPtr arguments);

        #endregion
    }
}