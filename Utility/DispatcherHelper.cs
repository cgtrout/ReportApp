using System.Windows;
using System.Windows.Threading;

namespace ReportApp.Utility
{
     /// <summary>
     /// Description of DispatcherHelper.
     /// </summary>
     public static class DispatcherHelper
     {
          /// <summary>
          /// Returns a dispatcher for multi-threaded scenarios
          /// </summary>
          /// <returns></returns>

          #region Methods

          public static Dispatcher GetDispatcher()
          {
               //use the application's dispatcher by default
               if (Application.Current != null)
                    return Application.Current.Dispatcher;

               //fallback for WinForms environments
               //if (source.Dispatcher != null) return source.Dispatcher;

               //ultimatively use the thread's dispatcher
               return Dispatcher.CurrentDispatcher;
          }

          #endregion Methods
     }
}