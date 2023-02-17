using ReportApp.Data;
using ReportApp.Model;
using ReportApp.Utility;
using ReportApp.ViewModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Windows;

namespace ReportApp
{
     public static class ReportAppMain
     {
          #region Methods

          public static void InitializeReportApp()
          {
               BackupDatabase();
               ZipOldLogFiles();

               AddAllTests();

               CleanupTests();

               List<Person> personList = null;
               using (var db = NetboxDatabase.GetReadOnlyInstance()) {
                    personList = db.LoadTablePerson();
               }

               DataRepository.AddListToDict(PersonViewModel.ConvertList(personList));
               DataRepository.SetSortedList();
               //Report.ReportListToXML(reportsList, "Report.xml");

               var reportsList = new List<Report>();
               try {
                    reportsList = Report.XMLToReportList(PathSettings.Default.ReportSettingsPath);
                    Report.Reports = reportsList;
               }
               catch (System.IO.DirectoryNotFoundException) {
                    MessageBox.Show("Error: could not find report settings file. (Report.xml)");
               }
               catch (Exception ex) {
                    MessageBox.Show("Error loading report file: " + ex.Message);
               }

               //for some reason this has to be loaded here or application
               //will close before window opens
               TraceEx.PrintLog("Loading MainWindow");
               var viewModel = MainWindowViewModel.MainWindowInstance;
               MainWindow window = new MainWindow();
               Application.Current.MainWindow = window;

               // When the ViewModel asks to be closed,
               // close the window.
               EventHandler handler = null;
               handler = delegate {
                    viewModel.RequestClose -= handler;

                    //call dispose to ensure it is always closed
                    viewModel.Dispose();
                    window.Close();
                    //window.WindowState = WindowState.Minimized;
               };
               viewModel.RequestClose += handler;
               window.DataContext = viewModel;
               TraceEx.PrintLog("Window.Show()");
               window.Show();

               Utility.NetworkTools.PingLog();

          }

          private static void AddAllTests()
          {
               try {
                    TraceEx.PrintLog("Adding all tests");
                    RuntimeTestSystem.RunTimeTester.Instance.AddAllSuites();
               }
               catch (Exception e) {
                    Trace.TraceError($"Problem adding tests: {e.GetType()}: {e.Message} ");
               }
          }

          private static void CleanupTests()
          {
               try {
                    TraceEx.PrintLog("Cleaning up tests");
                    RuntimeTestSystem.RunTimeTester.Instance.RunCleanup();
               } catch( Exception e) {
                    Trace.TraceError($"Problem cleaning up test data: {e.GetType()}: {e.Message} ");
               }
          }

          private static void ZipOldLogFiles()
          {
               //cleanup old files
               if (!Directory.Exists("logs")) {
                    Directory.CreateDirectory("logs");
               } else {
                    foreach (var file in Directory.GetFiles(".\\logs")) {
                         try {
                              File.Delete(file);
                         }
                         catch (Exception) {
                              TraceEx.PrintLog($"error deleting file {file}");
                         }
                    }
               }
               foreach (var file in Directory.GetFiles(".")) {
                    if (file.Contains(".txt")) {
                         try {
                              File.Copy(file, $".\\logs\\{file}");
                         }
                         catch (Exception) {
                              TraceEx.PrintLog($"error copying file {file}");
                         }

                         try {
                              File.Delete(file);
                         }
                         catch (Exception) {
                              TraceEx.PrintLog($"error deleting file {file}");
                         }
                    }
               }

               try {
                    ZipFile.CreateFromDirectory(".\\logs", $".\\_logs {DateTime.Now.ToString("yyyy MM dd hh mm ss")}.zip");
               }
               catch (Exception) {
                    TraceEx.PrintLog("Problem creating zip file");
               }
          }

          private static void BackupDatabase()
          {
               if (!Directory.Exists("c:\\CTApp\\DB\\Backup\\DB\\")) {
                    Directory.CreateDirectory("c:\\CTApp\\DB\\Backup\\DB\\");
               }

               try {
                    File.Copy(@"c:\CTApp\DB\Data.sqlite", $"c:\\CTApp\\DB\\Backup\\DB\\Data Backup{DateTime.Now.ToString("yyyy MM dd hh mm ss")}.sqlite");
               }
               catch (Exception) {
                    TraceEx.PrintLog("Problem backing up database");
               }

               try {
                    ZipFile.CreateFromDirectory("c:\\CTApp\\DB\\Backup\\DB", $"c:\\CTApp\\DB\\Backup\\Backup {DateTime.Now.ToString("yyyy MM dd hh mm ss")}.zip");
               }
               catch (Exception) {
                    TraceEx.PrintLog("Database backup: Problem creating zip file");
               }

               foreach (var file in Directory.GetFiles("c:\\CTApp\\DB\\Backup\\DB")) {
                    try {
                         if (file.Contains(".sqlite")) {
                              File.Delete(file);
                         }
                    }
                    catch (Exception) {
                         TraceEx.PrintLog($"error deleting file {file}");
                    }
               }
          }

          #endregion Methods
     }
}