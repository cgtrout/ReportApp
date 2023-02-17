using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ReportApp.Utility
{
     internal class BooleanToVisibilityConverter : IValueConverter
     {
          #region Methods

          public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
          {
               bool boolValue = (bool)value;
               boolValue = (parameter != null) ? !boolValue : boolValue;
               return boolValue ? Visibility.Visible : Visibility.Hidden;  //this behavior is different from built in ms provided
          }

          public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
          {
               if (value is Visibility) {
                    return (Visibility)value == Visibility.Visible;
               } else {
                    return false;
               }
          }

          #endregion Methods
     }
}