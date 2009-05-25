using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Diagnostics;

namespace Signum.Utilities
{
    public class ProgressEnumerator<T>: IEnumerable<T>, IEnumerator<T>, IProgressInfo
    {
        DateTime start = DateTime.Now;
 
        int count;
        int current = 0; 

        IEnumerator<T> enumerator;
     
        public ProgressEnumerator(IEnumerable<T> source, int count)
        {
            enumerator = source.GetEnumerator();
            this.count = count; 
        }

        public IEnumerator<T> GetEnumerator()
        {
            return this; 
        }
        
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this; 
        }

        public T Current
        {
            get { return enumerator.Current; }
        }

        object IEnumerator.Current
        {
            get { return current; }
        }

        public bool MoveNext()
        {
            if (enumerator.MoveNext())
            {
                current++;

                return true;
            }
            return false;
        }

        public void Reset()
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {

        }

        double IProgressInfo.Percentage
        {
            get { return 100 * ((IProgressInfo)this).Ratio; }
        }

        double IProgressInfo.Ratio
        {
            get { return count == 0 ? 0 : current  / (double)count; }
        }

        TimeSpan IProgressInfo.Elapsed
        {
            get { return DateTime.Now - start; }
        }

        TimeSpan IProgressInfo.Remaining
        {
            get
            {
                double ratio = ((IProgressInfo)this).Ratio;
                return ratio == 0 ? TimeSpan.Zero : new TimeSpan((long)((((IProgressInfo)this).Elapsed.Ticks / ratio) * (1 - ratio)));
            }
        }

        DateTime IProgressInfo.EstimatedFinish
        {
            get { return DateTime.Now + ((IProgressInfo)this).Remaining; }
        }

        public override string ToString()
        {
            IProgressInfo me = (IProgressInfo)this;
            TimeSpan ts = me.Remaining;
            return "{0:0.00}% | {1}h {2:D2}m {3:D2}s -> {4}".Formato(me.Percentage, ts.Hours, ts.Minutes, ts.Seconds, me.EstimatedFinish);
        }
    }

    public interface IProgressInfo
    {
        double Percentage { get; }

        double Ratio { get; }

        TimeSpan Elapsed { get; }

        TimeSpan Remaining { get; }
        DateTime EstimatedFinish { get; }
    }
}
