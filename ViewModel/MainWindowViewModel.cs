using API_Interface;
using ReportApp.Console;
using ReportApp.Data;
using ReportApp.Model;
using ReportApp.RuntimeTestSystem;
using ReportApp.Utility;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Speech.Synthesis;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

//using ReportApp.

namespace ReportApp.ViewModel
{
     /// <summary>
     /// The ViewModel for the application's main window.
     /// </summary>
     public class MainWindowViewModel : WorkspaceViewModel
     {
          private SpeechSynthesizer synth = new SpeechSynthesizer();

          //panel on left will show a list of reports
          private ObservableCollection<WorkspaceViewModel> _workspaces;

          private ObservableCollection<WorkspaceViewModel> _anchorables;

          public static MainWindowViewModel MainWindowInstance
          {
               get
               {
                    if (_mainWindowInstance == null) {
                         _mainWindowInstance = new MainWindowViewModel();
                    }

                    return _mainWindowInstance;
               }
          }

          private static MainWindowViewModel _mainWindowInstance;

          public MainWindowViewModel()
          {
               base.DisplayName = "Report App";
               driftTimeSpanList = new Queue<TimeSpan>();

               SetupConsoleCommands();

#if BENCHMARK
               benchmarkTimer.Tick += BenchmarkTimer_Tick;
               benchmarkTimer.Interval = new TimeSpan(2, 0, 0);
               benchmarkTimer.Start();
#endif
               runtimeStopwatch.Start();
          }

#if BENCHMARK
          //timer that will print benchmarker stats every 20 minutes to see how the timing changes over the day
          private DispatcherTimer benchmarkTimer = new DispatcherTimer();

          private void BenchmarkTimer_Tick(object sender, EventArgs e)
          {
               TraceEx.PrintLog($"Printing Benchmark Results:");
               Trace.Write(Benchmarker.GetString());
          }
#endif

          public int GetAddPersonFormsOpen()
          {
               int count = 0;
               foreach (var workSpace in Workspaces) {
                    if (workSpace.GetType() == typeof(AddPersonViewModel)) {
                         var vm = workSpace as AddPersonViewModel;
                         if (!vm.EditMode) {
                              count++;
                         }
                    }
               }
               return count;
          }

          ConsoleSystem console => ConsoleSystem.ConsoleSystemInstance;

          private void SetupConsoleCommands()
          {
               console.AddCommand(new ConsoleCommand("TQ", "Opens TaskQueue", OpenTaskQueue));
               console.AddCommand(new ConsoleCommand("Test.Run", "Runs all test suites or given test suite", async (x) => { await RunTimeTester.Instance.RunSuites(x); }));
               console.AddCommand(new ConsoleCommand("Test.GetName", "Gets names of all test suites.", GetTestSuiteNames));
               console.AddCommand(new ConsoleCommand("Test.Clean", "Clean up test data", CleanData));
               console.AddCommand(new ConsoleCommand("Crash", "Purposely crashes program", Crash));
               console.AddCommand(new ConsoleCommand("CheckData", "Verifies data in API matches data in Repos", async () => await CheckData()));
               console.AddCommand(new ConsoleCommand("Print", "Print data from one person.", (x) => PrintPerson(x)));
               console.AddCommand(new ConsoleCommand("PrintXML", "Print xml from one person.", async (x) => await PrintXML(x)));
               console.AddCommand(new ConsoleCommand("Fix", "Reload data (using parameter) from API and place in DB and Repos", async (x) => await FixPerson(x)));
               console.AddCommand(new ConsoleCommand("FixShiftEntry", "Fixes shift entries for given date paramater", (x) => FixShiftEntry(x)));
               console.AddCommand(new ConsoleCommand("GetAccessLog", "Print access entry by id based on parameter", async (x) => await GetAccessLog(x)));
               console.AddCommand(new ConsoleCommand("CBMessageIn", "Fake a new message for specified person", async (x) => await FakeMessage(x, "CB", "IN")));
               console.AddCommand(new ConsoleCommand("AdminMessageIn", "Fake a new message for specified person", async (x) => await FakeMessage(x, "ADMIN", "IN")));
               console.AddCommand(new ConsoleCommand("CBMessageOut", "Fake a new message for specified person", async (x) => await FakeMessage(x, "CB", "OUT")));
               console.AddCommand(new ConsoleCommand("AdminMessageOut", "Fake a new message for specified person", async (x) => await FakeMessage(x, "ADMIN", "OUT")));
               console.AddCommand(new ConsoleCommand("ResetMessageInOut", "Resets access id for Message commands", () => ResetMessageInOut()));
               console.AddCommand(new ConsoleCommand("GC", "Force Garbage Collect", GarbageCollect));
               console.AddCommand(new ConsoleCommand("mem", "show memory statics", MemoryStats));
               console.AddCommand(new ConsoleCommand("browser", "Open web browser", OpenBrowser));
               console.AddCommand(new ConsoleCommand("timechange", "Run time sync from Netbox", TimeChange));
               console.AddCommand(new ConsoleCommand("speak", "Say something", Speak));
               console.AddCommand(new ConsoleCommand("changeCompanyName", "ChangeCompanyName \"old_name\" \"new_name\"", async (x) => await ChangeCompanyName(x)));
               console.AddCommand(new ConsoleCommand("ping", "Ping network and print results", PingNetwork));
#if BENCHMARK
               console.AddCommand(new ConsoleCommand("PrintBenchMarks", "Get benchmark results", printBenchmarks));
#endif
          }

          private void PingNetwork()
          {
               Utility.NetworkTools.PingLog(executeTimeCheck:false);
          }

          private void CleanData()
          {
               RunTimeTester.Instance.RunCleanup();
          }

          private void GetTestSuiteNames()
          {
               console.WriteLine(RunTimeTester.Instance.GetAllSuiteNames());
          }

          private async Task ChangeCompanyName(object x)
          {
               if (x == null) return;

               string input = x as string;

               //find quote positions
               var q1 = input.IndexOf('\"');
               var q2 = input.IndexOf('\"', q1 + 1);
               var q3 = input.IndexOf('\"', q2 + 1);
               var q4 = input.IndexOf('\"', q3 + 1);

               //error check positions
               if (q1 == -1 || q2 == -1 || q3 == -1 || q4 == -1) {
                    console.WriteLine("Error must be in format \"old_name\" \"new_name\"");
                    return;
               }

               //get first string
               string oldName = input.Substring(q1 + 1, q2 - q1 - 1);

               //get second string
               string newName = input.Substring(q3 + 1, q4 - q3 - 1);

               console.WriteLine($"old name={oldName}");
               console.WriteLine($"new name={newName}");

               //get names from database
               using (var db = NetboxDatabase.GetWriteInstance()) {
                    var query = db.GetContext().People.Where(y => (y.Company == oldName));

                    foreach (var p in query) {
                         if (p.LastName == "Administrator") {
                              continue;
                         }
                         try {
                              var person = DataRepository.PersonDict[p.PersonId].Copy() as PersonViewModel;
                              person.Company = newName;
                              p.Company = newName;

                              //save to API
                              await API_Interaction.ModifyPerson(person.InternalPerson, true);

                              //save to local repos
                              FixPersonRepos(p.PersonId, person.InternalPerson);

                              console.WriteLine($"Changed {p.PersonId} {p.LastName} {p.FirstName}");
                         }
                         catch (Exception e) {
                              console.WriteLine($"Exception:{e.Message}");
                         }
                    }
                    //save to db
                    db.GetContext().SubmitChanges();
               }

               console.WriteLine("Company Name change completed!");
          }

          private void Speak(object x)
          {
               if (x == null) return;

               string input = x as string;

               PromptBuilder pb1 = new PromptBuilder();

               synth.Rate = -10;
               synth.SelectVoiceByHints(VoiceGender.Male);
               pb1.StartStyle(new PromptStyle { Emphasis = PromptEmphasis.Strong, Rate = PromptRate.Slow });
               pb1.AppendText(input);
               pb1.EndStyle();

               synth.Speak(pb1);
          }

          private void OpenBrowser()
          {
               WebBrowserViewModel vm = new WebBrowserViewModel();
               this.ShowView(vm);
          }

          private void TimeChange()
          {
               WebBrowserViewModel vm = WebBrowserViewModel.Instance;
               vm.Initialize(WebBrowserViewModel.BrowserPageMode.Time);

               this.ShowView(vm);
          }

          private void MemoryStats()
          {
               console.WriteLine($"{Process.GetCurrentProcess().PrivateMemorySize64 * 0.000001} mb being used.");
          }

          private void GarbageCollect()
          {
               GC.Collect();
               console.WriteLine("Garbage collect forced.");
          }

          private static long static_id = System.Int64.MaxValue;

          private static readonly object fakeMessageLock = new Object();

          private async Task FakeMessage(object x, string readerType, string inOut)
          {
               if (x == null) return;
               string input = x as string;

               var splits = input.Split(',');

               await Task.Factory.StartNew(() => {
                    foreach (var id in splits) {
                         var newid = id.Trim();
                         //make sure id exists
                         if (DataRepository.PersonDict.ContainsKey(newid) == false) {
                              console.WriteLine($"{newid} does not exist!");
                         }

                         AccessEntry entry = new AccessEntry() {
                              DtTm = DateTime.Now,
                              PersonId = newid,
                              LogId = static_id--,
                              Type = Model.TypeCode.ValidAccess,
                              Reason = 0,
                         };

                         if (readerType == "CB" && inOut == "IN") {
                              entry.ReaderKey = ReaderKeyEnum.ControlIn;
                              entry.Reader = "Control Building IN";
                         } else if (readerType == "CB" && inOut == "OUT") {
                              entry.ReaderKey = ReaderKeyEnum.ControlOut;
                              entry.Reader = "Control Building OUT";
                         } else if (readerType == "ADMIN" && inOut == "IN") {
                              entry.ReaderKey = ReaderKeyEnum.AdminIn;
                              entry.Reader = "Admin IN";
                         } else if (readerType == "ADMIN" && inOut == "OUT") {
                              entry.ReaderKey = ReaderKeyEnum.AdminOut;
                              entry.Reader = "Admin OUT";
                         }

                         CollectionChangedEventArgs e = new CollectionChangedEventArgs() {
                              ChangedValue = entry,
                              ChangeType = CollectionChangeType.Add
                         };
                         DispatcherHelper.GetDispatcher().Invoke(new Action(() => {
                              lock (fakeMessageLock) {
                                   DataRepository.AccessLogMessageHandler.OnCollectionChanged(e);
                              }
                         }), DispatcherPriority.Render);
                    }
               });
          }

          private void ResetMessageInOut()
          {
               static_id = Int64.MaxValue;
          }

          private void OpenTaskQueue()
          {
               this.ShowView(new GlobalSchedulerViewModel());
          }

          private void Crash()
          {
               GlobalScheduler.DBQueue.QueueTaskEnd("Crash", new Task(() => { throw new Exception(); }));
               //DispatcherHelper.GetDispatcher().BeginInvoke(new Action(() => { throw null; }));
          }

          private async Task CheckData()
          {
               console.WriteLine("Check data Test running...");
               console.WriteLine("Getting data from API");

               //API_Interface.API_Interaction api = new API_Interface.API_Interaction();
               var personList = await API_Interaction.LoadPersonDetails(API_Interface.DeleteType.All);
               console.WriteLine("Done: Getting data from API");
               int unequalCount = 0;

               console.WriteLine("Checking against Repos...");
               using (var db = NetboxDatabase.GetWriteInstance()) {
                    //now check against repos
                    foreach (var apiPerson in personList) {
                         try {
                              var dataReposPerson = DataRepository.PersonDict[apiPerson.PersonId];
                              if (dataReposPerson.Equals(new PersonViewModel(apiPerson)) == false) {
                                   console.WriteLine($"Invalid match in DataRepository: {apiPerson.PersonId}");
                                   console.WriteLine(dataReposPerson.InternalPerson.CompareAndPrint(apiPerson, "this", "other"));
                                   FixPersonRepos(apiPerson.PersonId, apiPerson);
                                   unequalCount++;
                              }
                         }
                         catch (KeyNotFoundException) {
                              console.WriteLine($"Could not find person={apiPerson.PersonId} in DataRepository");
                         }
                         catch (Exception e) {
                              console.WriteLine($"General exception: {e.Message}");
                         }
                    }
                    console.WriteLine("Checking against Database...");
                    //check against database

                    foreach (var apiPerson in personList) {
                         try {
                              var dbQuery = db.GetContext().People.Where(x => x.PersonId == apiPerson.PersonId);
                              if (dbQuery.Any()) {
                                   Person dbfirst = dbQuery.ToList().First();

                                   //load db vehicles into person so we can compare
                                   Person p = dbfirst.Clone();
                                   var vehQuery = db.GetContext().Vehicles.Where(x => x.PersonId == apiPerson.PersonId);
                                   foreach (var v in vehQuery) {
                                        p.VehicleList.Add(v);
                                   }

                                   if (p.Equals(apiPerson) == false) {
                                        console.WriteLine($"Invalid match in Database: {apiPerson.PersonId}");
                                        console.WriteLine(p.CompareAndPrint(apiPerson, "this", "other"));
                                        unequalCount++;
                                        FixPersonDb(apiPerson.PersonId, apiPerson);
                                   }
                              } else {
                                   console.WriteLine($"Could not find {apiPerson.PersonId} in DB");

                                   db.GetContext().People.InsertOnSubmit(apiPerson);
                                   db.GetContext().SubmitChanges();
                              }
                         }
                         catch (Exception e) {
                              console.WriteLine($"General exception: {e.Message}");
                         }
                    }
               }
               console.WriteLine("CheckData command - done!");
               if (unequalCount > 0) {
                    console.WriteLine($"Found {unequalCount} bad matches.");
               } else {
                    console.WriteLine("No issues found.");
               }
          }

          public void PrintPerson(object param)
          {
               var paramString = param as string;

               try {
                    console.WriteLine($"{DataRepository.PersonDict[paramString]}");
               }
               catch {
                    console.WriteLine("Problem looking up id given.");
               }
          }

          public async Task PrintXML(object param)
          {
               var paramString = param as string;

               var xdoc = await API_Interface.API_Interaction.LoadSinglePersonXDoc(paramString);
               console.WriteLine(xdoc.ToString());
          }

          private async Task FixPerson(object param)
          {
               var paramString = param as string;
               var apiPerson = await API_Interface.API_Interaction.LoadSinglePerson(paramString);

               if(apiPerson == null) {
                    console.WriteLine("FixPerson: error loading person from API");
                    return;
               }

               FixPersonRepos(paramString, apiPerson);
               FixPersonDb(paramString, apiPerson);
          }

          private void FixPersonRepos(string id, Person apiPerson)
          {
               if (DataRepository.PersonDict.ContainsKey(id)) {
                    DataRepository.PersonDict[id].CopyFromOther(new PersonViewModel(apiPerson));
               } else {
                    DataRepository.AddPerson(new PersonViewModel(apiPerson));
               }
          }

          private void FixPersonDb(string id, Person apiPerson)
          {
               using (var db = NetboxDatabase.GetWriteInstance()) {
                    var dbQuery = db.GetContext().People.Where(x => x.PersonId == apiPerson.PersonId);
                    if (dbQuery.Any()) {
                         var first = dbQuery.ToList().First();
                         first.CopyFromOther(apiPerson);
                         db.GetContext().SubmitChanges();
                         console.WriteLine("Fix Person completed");
                    } else {
                         //add since we couldn't find it
                         db.UpdatePerson(apiPerson);
                         console.WriteLine("Added person to db.");
                    }
               }
          }

          private void FixShiftEntry(object dt)
          {
               var datestr = dt as String;
               DateTime date = new DateTime();
               if (DateTime.TryParse(datestr, out date) == false) {
                    console.WriteLine("Invalid date entered.  Must be in format yyyy-mm-dd");
                    return;
               }

               using (var db = NetboxDatabase.GetWriteInstance()) {
                    var context = db.GetContext();

                    var accessQuery = from entry in context.AccessEntries
                                      where (entry.DtTm > date && entry.DtTm < date.AddHours(32))
                                      select entry;

                    var shiftEntryQuery = from entry in context.ShiftEntries
                                          where (entry.InTime > date && entry.InTime < date.AddHours(32))
                                          select entry;

                    var accessList = accessQuery.ToList();
                    var shiftEntryList = shiftEntryQuery.ToList();
                    var tuple = API_Interaction.CreateShiftList(accessList, shiftEntryList);
                    var createdShiftEntryList = tuple.Item1;
                    var updateQuery = createdShiftEntryList.Where(x => x.IsChanged);
                    if (updateQuery.Any()) {
                         db.UpdateFromList(createdShiftEntryList);
                    }
               }
          }

          private async Task GetAccessLog(object x)
          {
               var str = x as string;
               try {
                    long num = System.Convert.ToInt64(x);
                    var accessLog = await API_Interface.API_Interaction.GetAccessDataLog(num);
                    console.WriteLine($"accessLog={accessLog}");
               }
               catch (FormatException) {
                    console.WriteLine($"Invalid number entered");
               }
          }

          private void printBenchmarks()
          {
               var console = ConsoleSystem.ConsoleSystemInstance;
               console.WriteLine(Benchmarker.GetString());
          }

          public void SetNetboxDriftTime(TimeSpan ts)
          {
               driftTimeSpanList.Enqueue(ts);
               if (driftTimeSpanList.Count > driftListCount) {
                    driftTimeSpanList.Dequeue();
               }
               OnPropertyChanged(nameof(DriftTime));
               OnPropertyChanged(nameof(DriftTimeToolTip));
          }

          //estimated drift between netbox time and local computer
          private Queue<TimeSpan> driftTimeSpanList;

          private const int driftListCount = 5;

          public string StatusText
          {
               get
               {
                    return _statusText;
               }
               private set
               {
                    _statusText = value;
                    OnPropertyChanged(nameof(StatusText));
               }
          }

          public Brush StatusTextColor
          {
               get
               {
                    return _statusTextColor;
               }
               private set
               {
                    _statusTextColor = value;
                    OnPropertyChanged(nameof(StatusTextColor));
               }
          }

          private string _statusText;

          private TimeSpan CalculateAverageTimeSpan()
          {
               TimeSpan total = TimeSpan.FromTicks(0);
               if (!driftTimeSpanList.Any()) {
                    return TimeSpan.FromTicks(0);
               } else {
                    foreach (var ts in driftTimeSpanList) {
                         total += ts;
                    }
                    return TimeSpan.FromTicks(total.Ticks / driftTimeSpanList.Count);
               }
          }

          private bool userDriftTimeWarned = false;

          public string DriftTime
          {
               get
               {
                    var avgTimeSpan = CalculateAverageTimeSpan();

                    //warn user if drift time is too high
                    if (userDriftTimeWarned == false
                       && Math.Abs(avgTimeSpan.TotalMinutes) >= 5) {
                         MessageBox.Show("Drift time is getting too high.  It is recommended that you set the times on both this computer and the Netbox system", "WARNING", MessageBoxButton.OK, MessageBoxImage.Warning);
                         userDriftTimeWarned = true;
                    }
                    return $"Drift: {avgTimeSpan.TotalSeconds.ToString("0.00")}s";
               }
          }

          public string DriftTimeToolTip
          {
               get
               {
                    var timespan = CalculateAverageTimeSpan();
                    string verb = "";
                    verb = (timespan < TimeSpan.Zero) ? "behind" : "ahead of";
                    return $"(Estimated Netbox Time) - (This Computer Time)= Drift Time \nDrift time is the estimated time difference between this computer and the Netbox system.\n The Netbox time is {Math.Abs(timespan.TotalSeconds).ToString("0.00")} seconds {verb} this computer.";
               }
          }

          //ResetDriftTime
          public ICommand ResetDriftTimeCommand
          {
               get
               {
                    if (_resetDriftTimeCommand == null) {
                         _resetDriftTimeCommand = new RelayCommand(x => {
                              driftTimeSpanList = new Queue<TimeSpan>();
                              MainWindowInstance.PrintStatusText("Drift time reset to zero.", Brushes.Black);
                              OnPropertyChanged(nameof(DriftTime));
                         });
                    }
                    return _resetDriftTimeCommand;
               }
          }

          //how long has program been running
          private Stopwatch runtimeStopwatch = new Stopwatch();

          public string RunTime => $"{runtimeStopwatch.Elapsed.TotalDays.ToString("0.0")}";

          public async void PrintStatusText(string val, Brush color, int showTime = 10)
          {
               StatusTextColor = color;
               StatusText = val;
               var delay = Task.Delay(showTime * 1000);
               await delay;
               StatusText = String.Empty;
          }

          public ICommand OpenConsoleCommand
          {
               get
               {
                    if (_openConsoleCommand == null) {
                         _openConsoleCommand = new RelayCommand(x => OpenConsole());
                    }
                    return _openConsoleCommand;
               }
          }

          private ICommand _openConsoleCommand;

          public ICommand CloseTabsCommand
          {
               get
               {
                    if (_closeTabsCommand == null) {
                         _closeTabsCommand = new RelayCommand(x => CloseAllTabs());
                    }
                    return _closeTabsCommand;
               }
          }

          private ICommand _closeTabsCommand;

          public ICommand OpenAddPersonCommand
          {
               get
               {
                    if (_openAddPersonCommand == null) {
                         _openAddPersonCommand = new RelayCommand(x => OpenAddPerson());
                    }
                    return _openAddPersonCommand;
               }
          }

          private ICommand _openAddPersonCommand;

          private void OpenConsole()
          {
               this.ShowView(new ConsoleViewModel());
          }

          private void OpenAddPerson()
          {
               this.ShowView(AddPersonViewModel.Create(new PersonViewModel(new Person()), false));
          }

          public ICommand OpenSearchCommand
          {
               get
               {
                    if (_openSearchCommand == null) {
                         _openSearchCommand = new RelayCommand(x => OpenSearch());
                    }
                    return _openSearchCommand;
               }
          }

          private ICommand _openSearchCommand;

          public ICommand PrintRollCallCommand
          {
               get
               {
                    if (_printRollCallCommand == null) {
                         _printRollCallCommand = new RelayCommand(async x => await PrintRollCall());
                    }
                    return _printRollCallCommand;
               }
          }

          private ICommand _printRollCallCommand;

          public ICommand ShowTocCommand
          {
               get
               {
                    if (_showTocCommand == null) {
                         _showTocCommand = new RelayCommand(x => ShowToc());
                    }
                    return _showTocCommand;
               }
          }

          public ICommand OpenViewModelCommand
          {
               get
               {
                    if (_openViewModelCommand == null) {
                         _openViewModelCommand = new RelayCommand(OpenViewModel);
                    }
                    return _openViewModelCommand;
               }
          }

          public ICommand ShowHelpImageCommand
          {
               get
               {
                    if (_showHelpImageCommand == null) {
                         _showHelpImageCommand = new RelayCommand(ShowHelpImage);
                    }
                    return _showHelpImageCommand;
               }
          }

          public ICommand SaveRollCallsCommand
          {
               get
               {
                    if (_saveRollCallsCommand == null) {
                         _saveRollCallsCommand = new RelayCommand(async x => {
                              try {
                                   await ReportPrinter.ZipRollCalls();
                              }
                              catch (Exception e) {
                                   MessageBox.Show($"There was a problem generating the zip file: {e.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                                   Trace.TraceError("ZipRollCalls - " + e.Message);
                                   Trace.Write(e.StackTrace);
                              }
                              _mainWindowInstance.PrintStatusText("Roll Call Zip Complete!", Brushes.Black);
                         });
                    }
                    return _saveRollCallsCommand;
               }
          }

          private ICommand _saveRollCallsCommand;

          public ICommand ExportAllShiftEntryPresets
          {
               get
               {
                    if (_exportAllShiftEntryPresets == null) {
                         _exportAllShiftEntryPresets = new RelayCommand(async x => {
                              try {
                                   IsBusy = true;
                                   await ShiftEntriesViewModel.ExportAllPresets();
                              }
                              catch (Exception e) {
                                   MessageBox.Show($"There was a problem generating the zip file: {e.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                                   Trace.TraceError("ExportShiftEntries - " + e.Message);
                                   Trace.Write(e.StackTrace);
                              }
                              finally {
                                   IsBusy = false;
                              }
                              _mainWindowInstance.PrintStatusText("Export Completed", Brushes.DarkMagenta);
                         });
                    }
                    return _exportAllShiftEntryPresets;
               }
          }

          private ICommand _exportAllShiftEntryPresets;

          private void OpenViewModel(object obj)
          {
               var param = obj as string;

               TraceEx.PrintLog($"Executing shortcut: {param}");
               switch (param) {
                    case "AddPerson":
                         this.ShowView(AddPersonViewModel.Create(new PersonViewModel(new Person()), false));
                         break;

                    case "Search":
                         this.ShowView(new SearchPersonViewModel());
                         break;

                    case "OrientationsLiveView":
                         this.ShowView(new OrientationsLiveViewModel());
                         break;

                    case "VehicleEntries":
                         var vm = new VehicleEntriesViewModel(PathSettings.Default.VehicleDatabasePath);
                         var showing = this.ShowView(vm, anchorable: true);
                         if (showing) {
                              vm.Initialize();
                         }
                         break;

                    case "AccessLogs":
                         this.ShowView(new AccessEntriesViewModel());
                         break;

                    case "RollCallLiveView":
                         this.ShowView(new RollCallViewModel(showAccess: false), anchorable: true);
                         break;

                    case "Stats":
                         this.ShowView(new StatsViewModel());
                         break;

                    case "SaveStats":
                         this.ShowView(new SaveStatsViewModel());
                         break;

                    case "TaskQueue":
                         this.ShowView(new GlobalSchedulerViewModel());
                         break;

                    case "PhoneViewModel":
                         this.ShowView(new PhoneViewModel());
                         break;

                    case "ShiftEntries":
                         var shiftEntryVm = new ShiftEntriesViewModel(initialize: true);
                         shiftEntryVm.IsSearchExpanded = true;
                         this.ShowView(shiftEntryVm);
                         break;

                    default:
                         throw new ArgumentException("Invalid ViewModel Type");
               }
          }

          private void ShowHelpImage(object obj)
          {
               var fileName = obj as string;
               fileName = PathSettings.Default.HelpPath + "\\HelpImages\\" + fileName + ".tif";
               TraceEx.PrintLog($"Showing help image {fileName}");
               HelpFileViewModel vm = new HelpFileViewModel();
               vm.FileName = fileName;
               vm.DisplayName = obj as string;
               this.ShowView(vm);
          }

          private ICommand _openViewModelCommand;

          public ICommand PhoneImportCommand
          {
               get
               {
                    if (_phoneImportCommand == null) {
                         _phoneImportCommand = new RelayCommand(x => PhoneImport());
                    }
                    return _phoneImportCommand;
               }
          }

          public ICommand ShowAnswerKeyCommand
          {
               get
               {
                    if (_showAnswerKeyCommand == null) {
                         _showAnswerKeyCommand = new RelayCommand(x => {
                              TraceEx.PrintLog("Opening answer key");
                              HelpFileViewModel vm = new HelpFileViewModel();
                              vm.FileName = "AnswerKey.rtf";
                              Workspaces.Add(vm);
                              ActiveWorkspace = vm;
                         });
                    }
                    return _showAnswerKeyCommand;
               }
          }

          public ICommand ShowUpdateLog
          {
               get
               {
                    if (_showUpdateLog == null) {
                         _showUpdateLog = new RelayCommand(x => {
                              TraceEx.PrintLog("Opening update log");
                              HelpFileViewModel vm = new HelpFileViewModel();
                              vm.FileName = "UpdateLog.rtf";
                              vm.DisplayName = "Update Log";
                              Workspaces.Add(vm);
                              ActiveWorkspace = vm;
                         });
                    }
                    return _showUpdateLog;
               }
          }

          private void PhoneImport()
          {
               PhoneDataImporter.ImportPhoneData();
          }

          private ICommand _phoneImportCommand;

          private void ShowToc()
          {
               ContentsViewModel vm = new ContentsViewModel();
               Anchorables.Add(vm);
          }

          private ICommand _showTocCommand;

          private void OpenSearch()
          {
               this.ShowView(new SearchPersonViewModel());
          }

          private async Task PrintRollCall()
          {
               TraceEx.PrintLog("PrintRollCall()");
               await ReportPrinter.PrintReport("Roll Call - Company", Report.Reports);
               PrintStatusText("Roll Call printed", Brushes.Black);
          }

          private void CloseAllTabs()
          {
               var wsArray = _workspaces.ToArray();
               foreach (var workspace in wsArray) {
                    workspace.OnRequestClose();
               }
               PrintStatusText("All tabs closed", Brushes.Black);
          }

          private void ShowEditView()
          {
               EditTestViewModel workspace = new EditTestViewModel();

               this.Workspaces.Add(workspace);
               this.SetActiveWorkspace(workspace);
          }

          public async Task ShowReport(Report r)
          {
               ReportViewModel workspace = new ReportViewModel(r);
               this.Workspaces.Add(workspace);
               this.SetActiveWorkspace(workspace);
               await workspace.Initialize();
          }

          /// <summary>
          /// Returns the collection of available workspaces to display.
          /// A 'workspace' is a ViewModel that can request to be closed.
          /// </summary>
          public ObservableCollection<WorkspaceViewModel> Workspaces
          {
               get
               {
                    if (_workspaces == null) {
                         _workspaces = new ObservableCollection<WorkspaceViewModel>();
                         _workspaces.CollectionChanged += this.OnWorkspacesChanged;
                    }
                    return _workspaces;
               }
          }

          /// <summary>
          /// Show view in main window
          /// </summary>
          /// <param name="viewModel"></param>
          /// <param name="anchorable"></param>
          /// <returns>true if new window created</returns>
          public bool ShowView(WorkspaceViewModel viewModel, bool anchorable = false)
          {
               //check to ensure it is not running
               if (viewModel.OnlyOneCanRun) {
                    var otherRunning = GetOtherRunning(viewModel);
                    if (otherRunning != null) {
                         bool active = this.SetActiveWorkspace(otherRunning);
                         if (active == false) {
                              MessageBox.Show($"Can only run one '{viewModel.DisplayName}' at a time.");
                         }
                         return false;
                    }
               }

               if (anchorable == false) {
                    this.Workspaces.Add(viewModel);
                    this.SetActiveWorkspace(viewModel);
                    return true;
               } else {
                    this.Anchorables.Add(viewModel);
                    return true;
               }
          }

          /// <summary>
          /// Checks if other types of same kind are already running
          /// </summary>
          /// <param name="viewModel"></param>
          private WorkspaceViewModel GetOtherRunning(WorkspaceViewModel viewModel)
          {
               Type type = viewModel.GetType();

               //check anchorables
               foreach (var v in Anchorables) {
                    if (v.GetType() == type) {
                         return v;
                    }
               }

               //check workspaces
               foreach (var v in Workspaces) {
                    if (v.GetType() == type) {
                         return v;
                    }
               }

               return null;
          }

          public ObservableCollection<WorkspaceViewModel> Anchorables
          {
               get
               {
                    if (_anchorables == null) {
                         _anchorables = new ObservableCollection<WorkspaceViewModel>();
                         _anchorables.CollectionChanged += this.OnWorkspacesChanged;
                    }
                    return _anchorables;
               }
          }

          //public ObservableCollection<DependencyObject> Anchorables {
          //     get;
          //     set;
          //} = new ObservableCollection<DependencyObject>();

          private void OnWorkspacesChanged(object sender, NotifyCollectionChangedEventArgs e)
          {
               if (e.NewItems != null && e.NewItems.Count != 0)
                    foreach (WorkspaceViewModel workspace in e.NewItems)
                         workspace.RequestClose += this.OnWorkspaceRequestClose;

               if (e.OldItems != null && e.OldItems.Count != 0)
                    foreach (WorkspaceViewModel workspace in e.OldItems)
                         workspace.RequestClose -= this.OnWorkspaceRequestClose;
          }

          public WorkspaceViewModel ActiveWorkspace
          {
               get
               {
                    return _activeWorkspace;
               }
               set
               {
                    _activeWorkspace = value;
                    OnPropertyChanged(nameof(ActiveWorkspace));
               }
          }

          private WorkspaceViewModel _activeWorkspace;
          private ICommand _resetDriftTimeCommand;
          private ICommand _showAnswerKeyCommand;
          private Brush _statusTextColor = Brushes.Black;
          private RelayCommand _showUpdateLog;
          private ICommand _showHelpImageCommand;

          private bool SetActiveWorkspace(WorkspaceViewModel workspace)
          {
               if (this.Workspaces.Contains(workspace) == false) {
                    return false;
               }
               ActiveWorkspace = workspace;

               ICollectionView collectionView = CollectionViewSource.GetDefaultView(this.Workspaces);
               if (collectionView != null)
                    collectionView.MoveCurrentTo(workspace);

               return true;
          }

          private void OnWorkspaceRequestClose(object sender, EventArgs e)
          {
               WorkspaceViewModel workspace = sender as WorkspaceViewModel;
               workspace.Dispose();

               this.Workspaces.Remove(workspace);
               this.Anchorables.Remove(workspace);
          }
     }
}