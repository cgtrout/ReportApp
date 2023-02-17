using CefSharp;
using System;
using System.Windows;

namespace ReportApp
{
     /// <summary>
     /// Description of Startup.
     /// </summary>
     public class Startup
     {
          #region Methods

          [STAThread]
          public static void Main(string[] args)
          {
               try {
                    using (new SingleGlobalInstance(1000)) {
                         //init CEF
                         var settings = new CefSettings();
                         settings.SetOffScreenRenderingBestPerformanceArgs();
                         if (!Cef.Initialize(settings, shutdownOnProcessExit: true, performDependencyCheck: true)) {
                              throw new Exception("Unable to init Cef");
                         }

                         App app = new App();
                         app.ShutdownMode = ShutdownMode.OnMainWindowClose;
                         app.Run();
                    }
               }
               catch (TimeoutException) {
                    MessageBox.Show("Can not run more than one instance of ReportApp at a time.  Please close any running instances of ReportApp.", "Error", MessageBoxButton.OK, MessageBoxImage.Stop);
               }
          }

          #endregion Methods
     }
}