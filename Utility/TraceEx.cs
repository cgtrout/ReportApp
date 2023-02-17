using System;
using System.Diagnostics;

namespace ReportApp.Utility
{
     /// <summary>
     /// Description of TraceEx.
     /// </summary>
     public static class TraceEx
     {
          #region Methods

          public static void PrintLog(string message)
          {
               var now = DateTime.Now;
               Trace.WriteLine(now + " " + now.Millisecond.ToString() + " " + message);
          }

          #endregion Methods
     }
}