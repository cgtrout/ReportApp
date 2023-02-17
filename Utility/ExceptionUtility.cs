using System;

namespace ReportApp.Utility
{
     /// <summary>
     /// Description of ExceptionUtility.
     /// </summary>
     public static class ExceptionUtility
     {
          #region Methods

          /// <summary>
          /// Process exception and convert to readable form
          ///  - can handle AggregateExceptions
          /// </summary>
          /// <param name="e"></param>
          /// <returns></returns>
          public static string GetExceptionText(Exception e)
          {
               string outstr = "";
               var type = e.GetType();
               if (type == typeof(AggregateException)) {
                    var aggregateException = e as AggregateException;
                    outstr += "AGGREGATE EXCEPTION::" + "\n";
                    foreach (var ae in aggregateException.InnerExceptions) {
                         outstr += GetExceptionText(ae);
                    }
               } else {
                    outstr += e.GetType().ToString() + "\n";
                    outstr += e.Message + "\n";
                    outstr += e.StackTrace;
               }
               return outstr;
          }

          #endregion Methods
     }
}