using System;
using System.Runtime.InteropServices;

namespace PFC.Timers
{
	/// <summary>
	/// Descripción breve de QPTimer.
	/// </summary>


	public class FastTimer  
	{
		[System.Security.SuppressUnmanagedCodeSecurity]
		[DllImport("kernel32")]
		private static extern bool QueryPerformanceFrequency(ref long PerformanceFrequency);
		[System.Security.SuppressUnmanagedCodeSecurity]
		[DllImport("kernel32")]
		private static extern bool QueryPerformanceCounter(ref long PerformanceCount);

		private static long tickFreq;

        static FastTimer()
		{
			if (QueryPerformanceFrequency(ref tickFreq) == false)
			{
				throw new ApplicationException("Failed to query for the performance frequency!");
			}

			tickFreq/= 1000;
		}


		public static double TickToMiliseconds(long value)
		{

			double elapsed = (value) / (double)tickFreq;

			return elapsed;
		}

        public static long TickToMilisecondsLong(long value)
        {

            long elapsed = value / tickFreq;

            return elapsed;
        }

		public static long Now
		{
			get
			{
				long tickCount = 0;
				if (QueryPerformanceCounter(ref tickCount) == false)
				{
					throw new ApplicationException("Failed to query performance counter!");
				}
				return tickCount;
			}
		}

	}


}
