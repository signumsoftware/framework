using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Signum.Utilities
{
    public class ElapsedTime
    {
        public static Dictionary<string, ElapsedTimeEnty> IdentifiedElapseds =
            new Dictionary<string, ElapsedTimeEnty>();

        public IDisposable Start(string identifier)
        {
            Stopwatch sp = new Stopwatch();
            sp.Start();

            return new Disposable(() => { sp.Stop(); InsertEnty(sp.ElapsedMilliseconds, identifier); });
        }

        private void InsertEnty(long milliseconds, string identifier)
        {
            lock (IdentifiedElapseds)
            {
                ElapsedTimeEnty entry;
                if (!IdentifiedElapseds.TryGetValue(identifier, out entry))
                {
                    entry = new ElapsedTimeEnty();
                    IdentifiedElapseds.Add(identifier, entry); 
                }

                entry.LastTime = milliseconds;
                entry.TotalTime += milliseconds;
                entry.Times++;

                if (milliseconds < entry.MinTime) entry.MinTime = milliseconds;
                if (milliseconds > entry.MaxTime) entry.MaxTime = milliseconds;
                Debug.WriteLine(identifier + " - " + entry);
            }
        }
    }

    public class ElapsedTimeEnty
    {
        public long LastTime = 0;
        public long TotalTime = 0;
        public int Times = 0;

        public long MinTime = long.MaxValue;
        public long MaxTime = long.MinValue;
        public double Average
        {
            get { return (TotalTime / Times); }
        }

        public override string ToString()
        { 
            return "Last: {0}ms, Min: {1}ms, Med: {2}ms, Max: {3}ms, Calls: {4}".Formato(
                LastTime,
                MinTime,
                Average,
                MaxTime,
                Times);
            ;
        }
    }
}
