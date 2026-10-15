using System;
using System.Collections.Generic;
using System.IO;

namespace ChromExplorer.DataModel.DAL
{
    public class IntensityStore : IDisposable
    {
        //private readonly SpectrumStore[] _spectrums;
        public IntensityStore(int size)
        {
            Spectrums = new List<SpectrumStore>(size);
        }

        public List<SpectrumStore> Spectrums;
     
        public int GetValue(int spectrum, int point)
        {
            return Spectrums[spectrum].Data[point];
        }

         #region IDisposable Members
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~IntensityStore()
        {
            Dispose(false);
        }

        

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                Spectrums.Clear();
            }

            //free unmanaged resources
        }
        #endregion
    }

    public class SpectrumStore
    {
        //private readonly int[] _data;
        public SpectrumStore(BinaryReader reader, int size)
        {
            Data = new int[size];

            for(int i = 0; i < size; i++)
            {
                Data[i] = reader.ReadInt32();
            }
        }

        public SpectrumStore(int[] data)
        {
            Data = data;
        }

        public int[] Data;
    }
}
