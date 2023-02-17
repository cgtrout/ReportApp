using CefSharp;
using ReportApp.API;
using ReportApp.Console;
using ReportApp.Model;
using ReportApp.Utility;
using System;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Xceed.Wpf.AvalonDock;

namespace ReportApp
{
     /// <summary>
     /// Interaction logic for Window1.xaml
     /// </summary>
     public partial class MainWindow : Window
     {
          private readonly DispatcherTimer personUpdateTimer;
          private readonly DispatcherTimer accessUpdateTimer;
          private readonly DispatcherTimer clockTimer;
          private readonly DispatcherTimer statusTimer;

          private const int personTimerLength = 80;  //in minutes
          private const int accessTimerLength = 80; //in milli

          private DateTime personLastModified;
          private DateTime accessLastModified;

          public MainWindow()
          {
               InitializeComponent();
               TraceEx.PrintLog("Starting application");
               WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;

               //DispatcherHelper.GetDispatcher().BeginInvoke(new Action(() => {
               //     var tc = new Utility.TimeChanger();
               //     tc.ChangeTime();
               //}));

               var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
               DateTime buildDate = new DateTime(2000, 1, 1)
                    .AddDays(version.Build)
                    .AddSeconds(version.Revision * 2);
               Title = String.Format("ReportApp: Version {1} ({0})", buildDate.ToString("D"), Version.Default.VersionString);

#if OFFLINE
               Title += " OFFLINE MODE";
#endif

               //setup person timer
               personUpdateTimer = new DispatcherTimer();
               personUpdateTimer.Tick += new EventHandler(personUpdateTimer_Tick);
               personUpdateTimer.Interval = new TimeSpan(0, 0, 10, 0, 0);//set timer to zero so it runs immediately

               //setup access timer
               accessUpdateTimer = new DispatcherTimer();
               accessUpdateTimer.Tick += new EventHandler(accessUpdateTimer_Tick);
               accessUpdateTimer.Interval = new TimeSpan(0, 0, 0, 0, accessTimerLength);

               //setup access timer
               clockTimer = new DispatcherTimer();
               clockTimer.Tick += new EventHandler(clockTimer_Tick);
               clockTimer.Interval = new TimeSpan(0, 0, 0, 1, 0);
               //NtpTimer.Start();

               statusTimer = new DispatcherTimer();
               statusTimer.Tick += StatusTimer_Tick;
               statusTimer.Interval = new TimeSpan(0, 0, 0, 0, 16);
               statusTimer.Start();

               //set back just to ensure no person's are missed in updaet
               personLastModified = DateTime.Now.AddMinutes(-5);
               accessLastModified = DateTime.Now;

#if !OFFLINE
               personUpdateTimer.Start();
               accessUpdateTimer.Start();
               clockTimer.Start();
#endif
               ConsoleSystem consoleSystem = ConsoleSystem.ConsoleSystemInstance;
          }

          private void StatusTimer_Tick(object sender, EventArgs e)
          {
               UpdateUpdateText();
          }

          private async void personUpdateTimer_Tick(object sender, EventArgs e)
          {
               await personTimerHandler();
          }

          private async void accessUpdateTimer_Tick(object sender, EventArgs e)
          {
               await accessTimerHandler();
          }

          //Stopwatch NtpTimer = new Stopwatch();

          private void clockTimer_Tick(object sender, EventArgs e)
          {
               TextBoxTime.Text = DateTime.Today.ToString("dddd MMMM-dd | ") + DateTime.Now.ToString("HH:mm:ss");
               TextboxRuntime.GetBindingExpression(TextBox.TextProperty).UpdateTarget();
          }

          private static bool firstLoad = true;

          private async Task personTimerHandler()
          {
               personUpdateTimer.Stop();
               API.API_Loader loader = new ReportApp.API.API_Loader();
               DBLoadStatus.Clear();
               DBLoadStatus.ResetTimer();

               DBLoadStatus.WriteLine("::::BeforeUpdatePerson");
               DBLoadStatus.IsLoadingPerson = true;
               try {
                    //at midnight clear any unused persons with no credentials
                    bool deleteEmptyCredential = false;
                    var midnight = DateTime.Today;
                    if (personLastModified < midnight && DateTime.Now >= midnight) {
                         TraceEx.PrintLog("MainWindow:: deleteEmptyCred set");
                         deleteEmptyCredential = true;

                         //also run time sync
                         TraceEx.PrintLog("Executing time sync");
                         ConsoleSystem.ConsoleSystemInstance.ExecuteCommand("timechange");
                         ConsoleSystem.ConsoleSystemInstance.WriteLine($"{DateTime.Now}: Executing Netbox Time Change");
                    }

                    await API_Loader.UpdatePerson(PathSettings.Default.DatabasePath, firstLoad, deleteEmptyCredential);
                    if (firstLoad == true) {
                         personUpdateTimer.Interval = new TimeSpan(0, 0, personTimerLength, 0, 0);
                         firstLoad = false;
                    }

                    //await loader.UpdateAccess("c:\\CTApp\\DB\\Data.sqlite");
               }
               catch (WebException we) {
                    MessageBox.Show("Warning: could not connect to API\nMessage=" + we.Message);
               }
               DBLoadStatus.WriteLine("::::AfterUpdatePerson");
               DBLoadStatus.IsLoadingPerson = false;

               personLastModified = DateTime.Now;
               personUpdateTimer.Start();
          }

          private static bool firstAccess = true;

          private async Task accessTimerHandler()
          {
               accessUpdateTimer.Stop();
               API.API_Loader loader = new ReportApp.API.API_Loader();
               DateTime startTime = DateTime.Now;

               DBLoadStatus.WriteLine("::::BeforeUpdateAccess");
               DBLoadStatus.IsLoadingAccess = true;

               try {
                    await loader.UpdateAccess(false, firstAccess);
                    firstAccess = false;
               }
               catch (WebException we) {
                    MessageBox.Show("Warning: could not connect to API\nMessage=" + we.Message);
               }
               DBLoadStatus.WriteLine("::::AfterUpdateAccess");
               DBLoadStatus.IsLoadingAccess = false;

               DateTime endTime = DateTime.Now;

               accessLastModified = DateTime.Now;
               accessUpdateTimer.Start();
          }

          private void UpdateUpdateText()
          {
               DatabaseIsUpdatingText.Text = GenerateUpdateText();
          }

          //Generates text to show on status bar of main window
          private string GenerateUpdateText()
          {
               string outString = "API: ";
               outString += DBLoadStatus.IsLoadingAccess ? "A " : "  ";
               //outString += "| ";
               outString += DBLoadStatus.IsLoadingDatabase ? "D " : "  ";
               //outString += "| ";
               outString += DBLoadStatus.IsLoadingPerson ? $"P{DBLoadStatus.PersonPage} " : "  ";

               return outString;
          }

          protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
          {
               var mb = MessageBox.Show("Are you sure you want to close ReportApp?", "Close Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);
               if (mb == MessageBoxResult.Yes) {
                    GlobalScheduler.PrintAllTasks();
                    Cef.Shutdown();
                    Environment.Exit(0);
               } else {
                    e.Cancel = true;
               }
          }

          private void MenuItem_Click(object sender, RoutedEventArgs e)
          {
               this.Close();
          }

          private void dockingManager_IsMouseCapturedChanged(object sender, DependencyPropertyChangedEventArgs e)
          {
          }

          //these two called when resizing border
          private void dockingManager_IsMouseCaptureWithinChanged(object sender, DependencyPropertyChangedEventArgs e)
          { }

          private void dockingManager_LostMouseCapture(object sender, MouseEventArgs e)
          { }

          private void dockingManager_ManipulationStarted(object sender, ManipulationStartedEventArgs e)
          {
          }

          //possibly called in error condition
          private void dockingManager_QueryContinueDrag(object sender, QueryContinueDragEventArgs e)
          {
          }

          private void dockingManager_DragLeave(object sender, DragEventArgs e)
          {
          }

          private void dockingManager_DragLeave_1(object sender, DragEventArgs e)
          {
          }

          private void dockingManager_DragEnter(object sender, DragEventArgs e)
          {
          }

          private void dockingManager_FocusableChanged(object sender, DependencyPropertyChangedEventArgs e)
          {
          }

          //this is called just before error condition
          private void dockingManager_LostFocus(object sender, RoutedEventArgs e)
          {
          }

          //called when clicking document, but not tool
          private void dockingManager_GotMouseCapture(object sender, MouseEventArgs e)
          {
          }

          //called at open, but not when moving tools around
          private void dockingManager_LayoutChanging(object sender, EventArgs e)
          { }

          private void dockingManager_LayoutChanged(object sender, EventArgs e)
          { }

          //constantlly called, even when doing nothing.  Moving windows does not doing anything different
          private void dockingManager_LayoutUpdated(object sender, EventArgs e)
          {
          }

          //called when clicking a open window/tab, but not when ending a drag operation
          private void dockingManager_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
          {
               //TraceEx.PrintLog("Docking MouseLeftButtonUp");
          }

          private void Window_KeyUp(object sender, KeyEventArgs e)
          {
               switch (e.Key) {
                    case Key.F5:
                         ToggleRollCall(null, null);
                         break;

                    case Key.F6:
                         ToggleVehicle(null, null);
                         break;

                    case Key.F7:
                         accessEntries.IsActive = true;
                         break;
               }
          }

          private void ToggleRollCall(object sender, RoutedEventArgs e)
          {
               TraceEx.PrintLog("Shortcut ToggleRollCall");
               rollCallAnchorable.ToggleAutoHide();
          }

          private void ToggleVehicle(object sender, RoutedEventArgs e)
          {
               TraceEx.PrintLog("Shortcut ToggleVehicle");
               vehicleAnchorable.ToggleAutoHide();
          }

          private void dockingManager_ActiveContentChanged(object sender, EventArgs e)
          {
          }

          private void dockingManager_DocumentClosed(object sender, DocumentClosedEventArgs e)
          {
          }

          private void dockingManager_DocumentClosing(object sender, DocumentClosingEventArgs e)
          {
          }

          private void dockingManager_LostFocus_1(object sender, RoutedEventArgs e)
          {
          }

          private void MenuItem_Click_1(object sender, RoutedEventArgs e)
          {
          }
     }
}