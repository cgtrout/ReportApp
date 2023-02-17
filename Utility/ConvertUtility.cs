using System;

namespace ReportApp.Utility
{
     /// <summary>
     /// Description of ConvertUtility.
     /// </summary>
     public static class ConvertUtility
     {
          //takes input string - if null it will return empty string

          #region Methods

          /// <summary>
          /// Converts null to empty string, otherwise returns original string
          /// </summary>
          /// <param name="s"></param>
          /// <returns></returns>
          public static string ConvertStrHandleNull(string s)
          {
               if (string.IsNullOrEmpty(s)) {
                    return string.Empty;
               } else {
                    return s;
               }
          }

          public static DateTime ConvertStrToDateTime(string s)
          {
               if (string.IsNullOrEmpty(s)) {
                    return new DateTime();
               } else {
                    DateTime dt;
                    bool valid = DateTime.TryParse(s, out dt);
                    if (valid) {
                         return dt;
                    } else {
                         return new DateTime();
                    }
               }
          }

          public static int ConvertStrToInt32(string s)
          {
               if (string.IsNullOrEmpty(s)) {
                    return 0;
               } else
                    return Convert.ToInt32(s);
          }

          //can handle null/empty strings
          public static long ConvertStrToInt64(string s)
          {
               try {
                    if (string.IsNullOrEmpty(s)) {
                         return 0;
                    } else
                         return Convert.ToInt64(s);
               }
               catch (FormatException) {
                    return -1;
               }
          }

          public static string EmptyStringIfDateZero(DateTime date)
          {
               if (date.Year == 2001 && date.Day == 1 && date.Month == 1)
                    return "";
               else
                    return date.ToString("yyyy-M-dd");
          }

          /// <summary>
          /// Copies string if not null.  If it is null returns null itself
          /// </summary>
          /// <param name="str"></param>
          /// <returns></returns>
          public static string NullStringCopy(string str) => (str != null) ? string.Copy(str) : null;

          #endregion Methods
     }
}