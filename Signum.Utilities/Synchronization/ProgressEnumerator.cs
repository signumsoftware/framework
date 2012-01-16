using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Diagnostics;

namespace Signum.Utilities.Synchronization
{
    public interface IProgressInfo
    {
        double Percentage{get;}

        double Ratio{get;}

        TimeSpan Elapsed{get;}

        TimeSpan Remaining{get;}
        DateTime EstimatedFinish{get;}
    }

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

        public double Percentage
        {
            get { return 100 * Ratio; }
        }

        public double Ratio
        {
            get { return count == 0 ? 0 : current  / (double)count; }
        }

        public TimeSpan Elapsed
        {
            get { return DateTime.Now - start; }
        }

        public TimeSpan Remaining
        {
            get
            { 
                double ratio = Ratio;
                return ratio == 0 ? TimeSpan.Zero : new TimeSpan((long)((Elapsed.Ticks / ratio) * (1 - ratio)));
            }
        }

        public DateTime EstimatedFinish
        {
            get { return DateTime.Now + Remaining; }
        }

        public override string ToString()
        {
            return "{0:0.00}% | {1} (->{2}) ".Formato(Percentage, Remaining, EstimatedFinish);
        }
    }
}
