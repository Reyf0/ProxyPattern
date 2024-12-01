using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Timers;
namespace ProxyPattern
{
    interface ISubject
    {
        string Request(string request);
    }

    public class RealSubject : ISubject
    {
        public string Request(string request)
        {
            Console.WriteLine($"RealSubject processing request: {request}");
            
            System.Threading.Thread.Sleep(2000); // Симулируем долгую операцию
            return $"RealSubject response to {request}";
        }
    }


    class Proxy : ISubject
    {
        private readonly ISubject realSubject;
        private readonly Dictionary<string, Tuple<string, DateTime>> cache = new Dictionary<string, Tuple<string, DateTime>>();
        private readonly System.Timers.Timer cacheTimer;
        private readonly TimeSpan cacheExpiration = TimeSpan.FromSeconds(5);
        private readonly string key = "$ecret C0DE";
        public string inputKey;
        private readonly object cacheLock = new object();
        public Proxy(ISubject realSubject, string inputKey)
        {
            this.realSubject = realSubject ?? throw new ArgumentNullException(nameof(realSubject));
            cacheTimer = new System.Timers.Timer(cacheExpiration.TotalMilliseconds);
            cacheTimer.Elapsed += CacheTimer;
            cacheTimer.AutoReset = true;
            cacheTimer.Start();
            this.inputKey = inputKey ?? throw new ArgumentNullException(nameof(inputKey));
        }

        public string Request(string request)
        {
            if (!HasAccess())
            {
                return "Access denied!";
            }

            lock (cacheLock) // Блокируем доступ к кэшу
            {
                if (cache.TryGetValue(request, out var cachedItem) && cachedItem.Item2 >= DateTime.Now - cacheExpiration)
                {
                    Console.WriteLine($"Returning cached response for {request}");
                    return cachedItem.Item1;
                }

                // Вызов реального объекта и сохранение результата в кэш
                string response = realSubject.Request(request);
                cache[request] = new Tuple<string, DateTime>(response, DateTime.Now);
                return response;
            }
        }

        private void CacheTimer(object sender, ElapsedEventArgs e)
        {
            lock (cacheLock) // Блокируем доступ к кэшу
            {
                List<string> keysToRemove = new List<string>();
                foreach (var kvp in cache)
                {
                    if (kvp.Value.Item2 < DateTime.Now - cacheExpiration)
                    {
                        keysToRemove.Add(kvp.Key);
                    }
                }
                foreach (var key in keysToRemove)
                {
                    cache.Remove(key);
                }
            }
        }

        private bool HasAccess()
        {
            return key == inputKey;
        }

    }

    internal class Program
    {
        static void Main(string[] args)
        {
            ISubject realSubject = new RealSubject();
            ISubject proxy = new Proxy(realSubject, "$ecret C0DE");

            Thread thread1 = new Thread(() => MakeRequests(proxy, "request1")) { Name = "Thread-1" };
            Thread thread2 = new Thread(() => MakeRequests(proxy, "request2")) { Name = "Thread-2" };

            thread1.Start();
            thread2.Start();

            thread1.Join();
            thread2.Join();

            // Дополнительный запрос после завершения потоков
            Console.Write("\n");
            Console.WriteLine(proxy.Request("request1"));

        }

        static void MakeRequests(ISubject proxy, string request)
        {
            string threadName = Thread.CurrentThread.Name;
            Console.WriteLine($"{threadName} - {proxy.Request(request)}");
            Thread.Sleep(3000); // Ждем 3 секунды, прежде чем сделать повторный запрос
            Console.WriteLine($"{threadName} - {proxy.Request(request)}");
        }
    }
}