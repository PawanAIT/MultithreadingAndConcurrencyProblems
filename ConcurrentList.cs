using System;
using System.Threading;

namespace ConcurrentList
{
    class ConcurrentList<T>
    {
        int size;
        T[] arr;
        static object _readerCountLock = new object();
        static Semaphore _writerLock = new Semaphore(1,1);
        static int readerCount = 0;

        public ConcurrentList()
        {
            this.size = 0;
            this.arr = new T[16];
        }

        public void PushBack(T val)
        {
            _writerLock.WaitOne();

            TryGrow();
            arr[size] = val;
            size++;

            _writerLock.Release();
        }
        public void Remove(int index)
        {
            _writerLock.WaitOne();

            if (index < 0 || index >= arr.Length)
                throw new IndexOutOfRangeException();

            for (int i = index; i + 1 < arr.Length; i++)
            {
                arr[i] = arr[i + 1];
            }
            size--;

            _writerLock.Release();
        }

        public int Size
        {
            get 
            {
                return this.size;
            }
        }

        public T this[int index]
        {
            get 
            {
                if (index < 0 || index >= arr.Length)
                    throw new IndexOutOfRangeException();
                lock (_readerCountLock)
                {
                    readerCount++;
                    if(readerCount == 1)
                        _writerLock.WaitOne();
                }
             
                T val = arr[index];

                lock (_readerCountLock)
                {
                    readerCount--;
                    if (readerCount == 0)
                        _writerLock.Release();
                }
                return val;
            }
            set 
            {
                if (index < 0 || index >= arr.Length)
                    throw new IndexOutOfRangeException();
                _writerLock.WaitOne();

                arr[index] = value;
                
                _writerLock.Release();
            }
        }
        void TryGrow()
        {
            if (size < arr.Length)
                return;
                
            size *= 2;
            T[] temp = new T[size];
            for (int i = 0; i < arr.Length; i++)
                temp[i] = arr[i];
            arr = temp;
        }
    }
}
