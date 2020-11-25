using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;

namespace RateLimiter
{
    class Bucket
    {
        public int Timestamp { get; set; }
        public int Count { get; set; }

        public Bucket(int count, int timestamp)
        {
            Count = count;
            Timestamp = timestamp;
        }
    }
    class Deque
    {
        public LinkedList<Bucket> Buckets { get; set; }
        public int SumOfAllElementsInBuckets { get; set; }

        public Deque(LinkedList<Bucket> buckets, int sumOfAllElementsInBuckets)
        {
            this.Buckets = buckets;
            this.SumOfAllElementsInBuckets = sumOfAllElementsInBuckets;
        }
    }
    class RateLimit<K> // Generics.
    {
        static int Now
        {
            get
            {
                return (int)DateTime.Now.Ticks / 1000; // EveryTime Now is accessed we will Get new Value in seconds.
            }
        }
        public ConcurrentDictionary<K, Deque> RateLimitMap { get; }
        static StreamWriter file = new StreamWriter(@"C:\Users\pawan\source\repos\RateLimiter\Log.txt");
        static object _lock = new object();
        public RateLimit()
        {
            RateLimitMap = new ConcurrentDictionary<K, Deque>();
        }

        public int GetRequest(K Key)
        {
            string line = null;
            if (!RateLimitMap.ContainsKey(Key)) // Doesn't contain the Key - add the key.
            {
                RateLimitMap.GetOrAdd(Key, new Deque(new LinkedList<Bucket>(), sumOfAllElementsInBuckets: 0));

                lock (_lock) // Only one thread allowed to update the LinkedList;
                {
                    RateLimitMap[Key].Buckets.AddLast(new Bucket(1, timestamp: Now));
                    
                    // logging
                    line = $"Key = {Key}, Value = { RateLimitMap[Key].SumOfAllElementsInBuckets} , ThreadId = {Thread.CurrentThread.ManagedThreadId}\n";
                    file.Write(line);
                }
               
                return 1;
            }

            int currentTime = Now;
            var deque = RateLimitMap[Key].Buckets;
            int removedCount = 0;
            lock (_lock) // Only one thread allowed to update the LinkedList;
            {
                while (deque.First != null && currentTime - deque.First.Value.Timestamp > TimeSpan.FromMinutes(1).TotalMilliseconds / 1000) // [Simplify]
                {
                    removedCount += deque.First.Value.Count;
                    deque.RemoveFirst();
                }
            }

            int LastElementTime = RateLimitMap[Key].Buckets.Last?.Value.Timestamp ?? 0;
            lock (_lock) // Only one thread allowed to update the LinkedList;
            {
                if (currentTime == LastElementTime) // Last Element Matched ?
                    RateLimitMap[Key].Buckets.Last.Value.Count += 1;
                else
                    RateLimitMap[Key].Buckets.AddLast(new Bucket(1, timestamp: Now));

                RateLimitMap[Key].SumOfAllElementsInBuckets -= (removedCount - 1);

                // Logging.
                line = $"Key = {Key}, Value = { RateLimitMap[Key].SumOfAllElementsInBuckets} , ThreadId = {Thread.CurrentThread.ManagedThreadId}\n";
                file.Write(line);
            }
           
            return RateLimitMap[Key].SumOfAllElementsInBuckets;
        }
    }
    class Program
    {
        public static string RandomIp
        {
            get
            {
                var data = new byte[4];
                new Random().NextBytes(data);
                IPAddress ip = new IPAddress(data);
                return ip.ToString();
            }
        }
        public static string RandomBrowser
        {
            get
            {
                string[] randomBrowsers = new string[] { "Chrome", "safari", "Edge", "UC", "MobileBrowser", "Firefox" };
                int randomNumber = new Random().Next();
                return randomBrowsers[randomNumber % randomBrowsers.Length];
            }
        }
        static void Main(string[] args)
        {
            RateLimit<string> IpAddress = new RateLimit<string>();
            RateLimit<string> BrowserAddress = new RateLimit<string>();
            
            for(int i = 0;i < 5; i++)
            {
                new Thread(() =>
                {
                    while (true)
                    {
                        IpAddress.GetRequest(RandomIp);
                        BrowserAddress.GetRequest(RandomBrowser);
                        Thread.Sleep(20);
                    }
                }).Start();
            }

            new Thread(() =>
            {
                while (true)
                {
                    foreach(var pair in IpAddress.RateLimitMap)
                    {
                        Console.WriteLine("Key = {0}, Value = {1}", pair.Key, pair.Value.SumOfAllElementsInBuckets);
                    }
                    Console.WriteLine("--------------------------------");
                }
            });

            // we Got to Join these Threads but we are blocking main thread as a HACK.
            Console.ReadLine();
        }
    }
}
