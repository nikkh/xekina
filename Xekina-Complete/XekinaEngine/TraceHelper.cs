﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XekinaEngine
{
    
    public class TraceHelper
    {
        
        public static void TraceInformation(string message)
        {
            Trace.TraceInformation("@@ "+ message);
            Console.WriteLine("@@ info: " + message, ConsoleColor.Yellow);
        }

        public static void TraceVerbose(string message)
        {
            Trace.WriteLine("@@ " + message);
            Console.WriteLine("@@ " + message, ConsoleColor.Yellow);
        }

        public static void TraceError(string message)
        {
            Trace.TraceError("@@ " + message);
            Console.WriteLine("@@ ERROR: " + message, ConsoleColor.Yellow);
        }

        public static void TraceWarning(string message)
        {
            Trace.TraceWarning("@@ " + message);
            Console.WriteLine("@@ WARNING: " + message, ConsoleColor.Yellow);
        }
    }
}
