using System;
using System.Globalization;
using System.Windows.Data;

namespace ReportApp.Utility
{
     /// <summary>
     /// Description of NullableValueConverter.
     /// </summary>
     public class NullableValueConverter : IValueConverter
     {
          #region Methods

          public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
          {
               return value;
          }

          public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
          {
               if (value == null || string.IsNullOrEmpty(value.ToString())) {
                    return null;
               }
               return value;
          }

          #endregion Methods
     }
}