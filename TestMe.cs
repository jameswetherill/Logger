using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Logger
{
    class Program
    {

        static void Main(string[] args)
        {
            Console.WriteLine("Today: {0}", DateTime.Today);

            DateTime y = GetYesterday();
            Console.WriteLine("Yesterday: {0}", y);

            Console.WriteLine("logic {0}", true & true);
            Console.WriteLine("logic {0}", true & false);
            Console.WriteLine("logic {0}", false & true);
            Console.WriteLine("logic {0}", false & false);

            Logger log = new Logger("c:/temp", "logFile", Logger.LEVEL.FATAL, Logger.ROLLOVER.CIRCULAR);
            int count = 0;
            while (count < 10000)
            {
                log.LogMessage(Logger.LEVEL.FATAL, "help me do something better with my life!!");
               // System.Threading.Thread.Sleep(500);
            }


        }

        /// <summary>
        /// Gets the previous day to the current day.
        /// </summary>
        static DateTime GetYesterday()
        {
            // Add -1 to now.
            return DateTime.Today.AddDays(-1);
        }

    }
}
