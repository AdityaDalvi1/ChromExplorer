using System.Collections.Generic;

namespace Serial.Utils
{
    public class ReceiveBuffer<T> : Queue<T>
    {
        public int Size { get; private set; }

        public ReceiveBuffer(int size)
        {
            Size = size;
        }

        public new void Enqueue(T obj)
        {
            Last = obj;
            base.Enqueue(obj);

            while (base.Count > Size)
            {
                T outObj;
                base.Dequeue();
            }
        }

        public T Last { get; private set; }
    }
}