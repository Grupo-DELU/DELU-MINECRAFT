using System;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;

namespace DeluMc.Utils
{

    /// <summary>
    /// Class for managing multiple clocks/stopwatchs
    /// </summary>
    public static class Clocker
    {
        private static Dictionary<string, Stopwatch> watches;
        private static readonly long nanosecPerTick;


        /// <summary>
        /// Checks if a clock with a name already exists
        /// </summary>
        /// <param name="name">Clock name</param>
        /// <param name="msg">Clock status message enabled</param>
        /// <returns>True if the clock exists/False otherwise</returns>
        public static bool ContainsClock(string name, bool msg = false)
        {
            if (!watches.ContainsKey(name))
            {
                if (msg)
                    Console.WriteLine($"Watch {name} doesn't exist!");
                return false;
            }
            if (msg)
                Console.WriteLine($"Watch {name} exists!");
            return true;
        }    


        /// <summary>
        /// Adds a clock to the class if it doesn't exists already
        /// </summary>
        /// <param name="name">Name of the clock</param>
        public static void AddClock(string name)
        {
            if (ContainsClock(name))
            {
                Console.WriteLine($"Watch {name} already exist!");
                return;
            }
            watches[name] = new Stopwatch();
        }


        /// <summary>
        /// Adds a clock to the class and starts it
        /// </summary>
        /// <param name="name">Name of the clock</param>
        public static void AddAndStartClock(string name)
        {
            if (ContainsClock(name))
            {
                Console.WriteLine($"Watch {name} already exist!");
                return;
            }
            watches[name] = Stopwatch.StartNew();            
        }


        public static void RemoveClock(string name, bool s = false, bool ms = false, bool ns = false)
        {
            if (!ContainsClock(name, true))
                return;

            Stopwatch watch = watches[name];
            
            if (watch.IsRunning && (s || ms || ns))
            {
                Console.WriteLine($"\nClock {name} time elapsed");
                Console.WriteLine("=========================================");
                PrintTime(watch, s, ms, ns);
                Console.WriteLine("=========================================\n\n");
                // Stops it from counting anymore
                watch.Reset();
            }
            watches.Remove(name);
        }


        public static void StartClock(string name)
        {
            if (!ContainsClock(name, true))
                return;
            watches[name] = Stopwatch.StartNew();
        }


        public static void RestartAndRunClock(string name)
        {
            if (!ContainsClock(name, true))
                return;
            watches[name].Restart();
        }


        public static void PauseClock(string name)
        {
            if (!ContainsClock(name, true))
                return;

            Stopwatch watch = watches[name];
            if (!watch.IsRunning)
            {
                Console.WriteLine($"Clock {name} is already paused");
                return;
            }
            watch.Stop();            
        }

        
        public static void ResumeClock(string name)
        {
            if (!ContainsClock(name, true))
                return;
            
            Stopwatch watch = watches[name];
            if (watch.IsRunning)
            {
                Console.WriteLine($"Clock {name} is already running");
                return;
            }
            watch.Start();
        }
        
        public static void RestartClock(string name)
        {
            if (!ContainsClock(name, true))
                return;
            watches[name].Reset();
        }


        private static void PrintTime(Stopwatch watch, bool s = false, bool ms = false, bool ns = false)
        {
            if (!s && !ms && !ns)
            {
                Console.WriteLine("Use at least one time print option!");
                return;
            }

            // NOTE: 1 Tick = 100 nanoseconds
            TimeSpan time = watch.Elapsed;

            if (s)
                Console.WriteLine($"Elapsed time in seconds: {time.TotalSeconds}");
            if (ms)
                Console.WriteLine($"Elapsed time in miliseconds: {time.TotalMilliseconds}");
            if (ns && Stopwatch.IsHighResolution)
                Console.WriteLine($"Elapsed time in nanoseconds: {watch.ElapsedTicks * Clocker.nanosecPerTick}");
        }


        public static void PrintTime(string name, bool s = false, bool ms = false, bool ns = false)
        {
            if (!s && !ms && !ns)
            {
                Console.WriteLine("Use at least one time print option!");
                return;
            }
            if (!ContainsClock(name, true))
                return;

            Console.WriteLine($"\nClock {name} time elapsed");
            Console.WriteLine("=========================================");
            PrintTime(watches[name], s, ms, ns);
            Console.WriteLine("=========================================\n\n");
        }

        public static void PrintAllTime(bool s = false, bool ms = false, bool ns = false)
        {
            if (!s && !ms && !ns)
            {
                Console.WriteLine("Use at least one time print option!");
                return;
            }
            if (watches.Count == 0)
            {
                Console.WriteLine("There aren't watches!");
                return;
            }

            foreach (string name in watches.Keys)
                PrintTime(name, s, ms, ns);
        }


        static Clocker()
        {
            watches = new Dictionary<string, Stopwatch>();
                
            if (Stopwatch.IsHighResolution)
            {
                nanosecPerTick = (1000L*1000L*1000L) / Stopwatch.Frequency;
                Console.WriteLine("Operations timed using the system's high-resolution performance counter.");
            }
            else
            {
                Console.WriteLine("Operations timed using the DateTime class.");
            }
        }
    }
}