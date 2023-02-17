using ReportApp.Utility;
using System;
using System.Diagnostics;
using System.IO;

namespace ReportApp.Model
{
     /// <summary>
     /// Description of DBLoadStatus.
     /// </summary>
     public static class DBLoadStatus
     {
          #region Fields

          private static System.Text.StringBuilder _statusText;

          private static Object StatusTextLock = new Object();

          #endregion Fields

          #region Constructors

          static DBLoadStatus()
          {
               StatusText = new System.Text.StringBuilder();
               StartTime = DateTime.Now;
               IsLoadingAccess = false;
               IsLoadingPerson = false;
               IsLoadingDatabase = false;
          }

          #endregion Constructors

          #region Properties

          public static bool DirectToOutputWindow { get; set; }

          public static bool DirectToTraceOutput { get; set; }

          //is the db being loaded or not?
          public static bool IsLoadingAccess { get; set; }

          public static bool IsLoadingDatabase { get; set; }

          public static bool IsLoadingPerson { get; set; }

          public static int PersonPage { get; set; }

          public static string LastUpdated { get; set; }

          public static DateTime StartTime { get; private set; }

          public static System.Text.StringBuilder StatusText
          {
               get
               {
                    lock (StatusTextLock) {
                         return _statusText;
                    }
               }
               set
               {
                    _statusText = value;
               }
          }

          #endregion Properties

          #region Methods

          public static void Clear()
          {
               StatusText.Clear();
          }

          public static string GetStatusText()
          {
               lock (StatusTextLock) {
                    return StatusText.ToString();
               }
          }

          public static void ResetTimer()
          {
               StartTime = DateTime.Now;
          }

          public static void SaveToFile()
          {
               using (var writer = File.CreateText("DBLoadLog " + DateTime.Now.ToString("d-HH-mm-ss-fff") + ".txt")) {
                    lock (StatusTextLock) {
                         writer.WriteLine(StatusText);
                    }
               }
          }

          [Conditional("DBOUTPUT")]
          public static void WriteLine(string line)
          {
               TimeSpan time = DateTime.Now - StartTime;

               lock (StatusTextLock) {
                    StatusText.AppendLine(String.Format("{0}:  {1}", time, line));
               }

               if (DirectToOutputWindow) {
                    Debug.WriteLine(line);
               }
               if (DirectToTraceOutput) {
                    TraceEx.PrintLog(line);
               }
          }

          #endregion Methods
     }
}