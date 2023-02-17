using ReportApp.Utility;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace ReportApp
{
     /// <summary>
     /// Interaction logic for App.xaml
     /// </summary>
     public partial class App : Application
     {
          #region Constructors

          public App()
          {
               AppDomain currentDomain = AppDomain.CurrentDomain;
               currentDomain.UnhandledException += new UnhandledExceptionEventHandler(UnhandledException);
          }

          #endregion Constructors

          #region Methods

          protected override void OnStartup(StartupEventArgs e)
          {
               base.OnStartup(e);

               ReportAppMain.InitializeReportApp();
          }

          //tell user that exception happened
          private void ShowUserException(string exceptionText)
          {
               MessageBox.Show($"Error: {exceptionText}\n\nProgram has shut down unexpectedly.\nYou will need to restart the program", $"ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
          }

          private void UnhandledException(object sender, UnhandledExceptionEventArgs args)
          {
               Exception e = (Exception)args.ExceptionObject;

               using (var f = File.CreateText(String.Format("{0} crash output.txt", DateTime.Now.ToString("MM dd y HH MM ss")))) {
                    string outputString = ExceptionUtility.GetExceptionText(e);
                    f.WriteLine(outputString);
                    TraceEx.PrintLog("App::UnhandledException");
                    Trace.TraceError(outputString);
                    ShowUserException($"{e.GetType().ToString()}\n{e.Message}");
               }
          }

          private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
          {
               using (var f = File.CreateText(String.Format("{0} crash output.txt", DateTime.Now.ToString("MM dd y HH MM ss")))) {
                    string outputString = ExceptionUtility.GetExceptionText(e.Exception);
                    f.WriteLine(outputString);
                    TraceEx.PrintLog("App:Application_DispatcherUnhandledException");
                    Trace.TraceError(outputString);
                    ShowUserException($"{e.Exception.GetType().ToString()}\n{e.Exception.Message}");
               }
          }

          #endregion Methods
     }
}