using ReportApp.Data;
using ReportApp.Model;
using ReportApp.Utility;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Media;
using System.Speech.Synthesis;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace ReportApp.ViewModel
{
     /// <summary>
     /// Description of AccessEntryViewModel.
     /// </summary>
     public class AccessEntriesViewModel : WorkspaceViewModel
     {
          private MainWindowViewModel _mainWindow;
          private DateTime lastUpdateTime;
          private SpeechSynthesizer synth = new SpeechSynthesizer();

          public ObservableCollection<AccessEntryViewModel> AccessEntries { get; set; }

          public ICollectionView View
          {
               get { return _view; }
               set
               {
                    _view = value;
                    OnPropertyChanged(nameof(View));
               }
          }

          public ICommand GenerateReportCommand
          {
               get
               {
                    if (_generateReportCommand == null) {
                         _generateReportCommand = new RelayCommand(x => {
                              DispatcherHelper.GetDispatcher().Invoke(async () => {
                                   var reportvm = new ReportViewModel(Report.Reports.First(a => a.Name == "Access Log"));
                                   await reportvm.Initialize();
                                   ShowReport(reportvm);
                              });
                         });
                    }
                    return _generateReportCommand;
               }
          }

          private void ShowReport(ReportViewModel reportvm)
          {
               MainWindowViewModel.MainWindowInstance.ShowView(reportvm);
          }

          private ICollectionView _view;

          private DispatcherTimer ClockTimer = new DispatcherTimer();

          public AccessEntriesViewModel()
          {
               base.DisplayName = "Access Logs";

               AccessEntries = new ObservableCollection<AccessEntryViewModel>();
               initializeView();

               SelectedDate = DateTime.Today;
               _mainWindow = MainWindowViewModel.MainWindowInstance;

               TraceEx.PrintLog("Opening Access Log");

               var dispatcher = DispatcherHelper.GetDispatcher();
               dispatcher.Invoke(new Action(() => {
                    DataRepository.AccessLogMessageHandler.CollectionChanged += AccessLogMessageHandler_CollectionChanged;
               }));

               ClockTimer.Interval = new TimeSpan(0, 0, 5);
               ClockTimer.Tick += ClockTimer_Tick;
               ClockTimer.Start();

               synth.Volume = 100;

               ClearFilter();
          }

          private void initializeView()
          {
               View = CollectionViewSource.GetDefaultView(AccessEntries);
               View.SortDescriptions.Add(new SortDescription("DtTm", ListSortDirection.Descending));
          }

          public DateTime LastTime { get; private set; } = DateTime.Now;

          private void ClockTimer_Tick(object sender, EventArgs e)
          {
               OnPropertyChanged(nameof(IsActive));

               //check if last time was before midnight and current time is greater than
               var now = DateTime.Now;
               var midnight = DateTime.Today;
               if (LastTime < midnight && now >= midnight) {
                    TraceEx.PrintLog("Changing Date!!");
                    SelectedDate = now.Date;
                    OnPropertyChanged(nameof(SelectedDate));
               }

               LastTime = DateTime.Now;
          }

          private void AccessLogMessageHandler_CollectionChanged(CollectionChangedEventArgs e)
          {
               //DispatcherHelper.GetDispatcher().BeginInvoke( new Action(()=> {
               if (SelectedDate.Date == DateTime.Today) {
                    var elem = e.ChangedValue as AccessEntry;
                    PersonViewModel p = null;
                    if (elem.PersonId != null && DataRepository.PersonDict.ContainsKey(elem.PersonId)) {
                         p = DataRepository.PersonDict[elem.PersonId];
                    }

#if FILTERTEST
                    if (p != null && (p.LastName.ToLower().Contains("test") == true
                         || p.FirstName.ToLower().Contains("test") == true)) {
                         return;
                    }
#endif

                    //if date is not for selected date return without adding it
                    if (elem.DtTm.Date != SelectedDate.Date) {
                         return;
                    }

                    //find access entries and remove
                    if (AccessEntries.Any()) {
                         var query = AccessEntries.Where(x => x.LogId == elem.LogId);

                         if (query.Any()) {
                              AccessEntries.Remove(query.First());
                         }
                    }

                    AccessEntries.Add(new AccessEntryViewModel(p, elem));

                    if (elem.Reason == 0) return;

                    if (elem.Reason == ReasonCode.CardExpired) {
                         if (synth.State != SynthesizerState.Speaking) {
                              SystemSounds.Exclamation.Play();
                              synth.SpeakAsync("Expired! Expired!");
                         }
                    } else if (elem.Reason == ReasonCode.CardNotInS2NCDatabase) {
                         if (synth.State != SynthesizerState.Speaking) {
                              SystemSounds.Exclamation.Play();
                              synth.SpeakAsync("Credential! Credential!");
                         }
                    } else if (elem.Reason == ReasonCode.AntiPassbackViolation) {
                         if (synth.State != SynthesizerState.Speaking) {
                              SystemSounds.Exclamation.Play();
                              synth.SpeakAsync("Passback! Passback!");
                         }
                    } else if (elem.Reason == ReasonCode.WrongLocation) {
                         if (synth.State != SynthesizerState.Speaking) {
                              SystemSounds.Exclamation.Play();
                              synth.SpeakAsync("Fob not active!");
                         }
                    }
               }
               //}), DispatcherPriority.Render);
          }

          public bool IsDateEnabled
          {
               get
               {
                    return _isDateEnabled;
               }
               set
               {
                    _isDateEnabled = value;

                    OnPropertyChanged(nameof(IsDateEnabled));
               }
          }

          public bool CanUserSort
          {
               get
               {
                    return _canUserSort;
               }
               set
               {
                    _canUserSort = value;
                    OnPropertyChanged(nameof(CanUserSort));
               }
          }

          public bool IsLockEnabled
          {
               get
               {
                    return _isLockEnabled;
               }
               set
               {
                    _isLockEnabled = value;

                    if (value == true) {
                         LockContent();
                    } else {
                         UnlockContent();
                    }

                    OnPropertyChanged(nameof(IsLockEnabled));
               }
          }

          private void UnlockContent()
          {
               IsDateEnabled = true;
               CanUserSort = true;
          }

          private void LockContent()
          {
               SelectedDate = DateTime.Today;
               IsDateEnabled = false;
               CanUserSort = false;
          }

          public AccessEntryViewModel SelectedItem
          {
               get { return _selectedItem; }
               set
               {
                    _selectedItem = value;
                    OnPropertyChanged(nameof(SelectedItem));
               }
          }

          private AccessEntryViewModel _selectedItem;

          private ICommand _editCommand;

          public ICommand EditCommand
          {
               get
               {
                    if (_editCommand == null) {
                         _editCommand = new RelayCommand((x) => { Edit(x); });
                    }
                    return _editCommand;
               }
          }

          private void Edit(object personId)
          {
               string _personId = (string)personId;
               if (_personId == null) {
                    _personId = _selectedItem?.CurrentPerson?.PersonId;
               }
               if (_personId != null) {
                    var query = AccessEntries.Where(p => p.CurrentPerson?.PersonId == _personId);

                    if (!query.Any()) {
                         Trace.TraceWarning("AccessLog: Edit: Could not find id");
                         System.Windows.MessageBox.Show("There was a problem opening this person.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                         return;
                    }

                    PersonViewModel person = query.First().CurrentPerson;
                    _mainWindow.ShowView(AddPersonViewModel.CreateAsync(person, true));
               }
          }

          public ICommand AddVehicleCommand
          {
               get
               {
                    if (_addVehicleCommand == null) {
                         _addVehicleCommand = new RelayCommand((x) => {
                              DispatcherHelper.GetDispatcher().BeginInvoke(new Action(() => {
                                   VehiclePassRequestEventArgs e = new VehiclePassRequestEventArgs();
                                   if (SelectedItem != null) {
                                        e.PersonId = SelectedItem.CurrentPerson.PersonId;
                                        VehiclePassMessageRequester.Instance.VehiclePassRequest(e);
                                   }
                              }), System.Windows.Threading.DispatcherPriority.Render);
                         });
                    }
                    return _addVehicleCommand;
               }
          }

          public ICommand AddAwayListCommand
          {
               get
               {
                    if (_addAwayListCommand == null) {
                         _addAwayListCommand = new RelayCommand((x) => {
                              DispatcherHelper.GetDispatcher().BeginInvoke(new Action(() => {
                                   AwayListRequestEventArgs e = new AwayListRequestEventArgs();
                                   if (SelectedItem != null) {
                                        e.PersonId = SelectedItem.CurrentPerson.PersonId;
                                        AwayListMessageRequester.Instance.AwayListRequest(e);
                                   }
                              }), System.Windows.Threading.DispatcherPriority.Render);
                         });
                    }
                    return _addAwayListCommand;
               }
          }

          public bool IsActive
          {
               get
               {
                    return SelectedDate.Date == DateTime.Today;
               }
               set
               {
                    _IsActive = value;
                    OnPropertyChanged(nameof(IsActive));
               }
          }

          private bool _IsActive;

          public DateTime SelectedDate
          {
               get { return _selectedDate; }
               set
               {
                    _selectedDate = value;
                    ChangeDate();
                    OnPropertyChanged(nameof(IsActive));
                    OnPropertyChanged(nameof(SelectedDate));
               }
          }

          private DateTime _selectedDate;

          private ICommand _filterCommand;

          public ICommand FilterCommand
          {
               get
               {
                    if (_filterCommand == null) {
                         _filterCommand = new RelayCommand(x => {
                              TraceEx.PrintLog("FilterCommand executed");
                              ApplyFilter();
                         });
                    }
                    return _filterCommand;
               }
          }

          private ICommand _clearFilterCommand;

          public ICommand ClearFilterCommand
          {
               get
               {
                    if (_clearFilterCommand == null) {
                         _clearFilterCommand = new RelayCommand(x => {
                              ClearFilter();
                         });
                    }
                    return _clearFilterCommand;
               }
          }

          private void ClearFilter()
          {
               FilterText = String.Empty;
               //send roll call select message
               DispatcherHelper.GetDispatcher().BeginInvoke(new Action(() => {
                    var e = new RollCallSelectMessageRequestEventArgs();
                    e.query = string.Empty;
                    e.MessageType = RollCallSelectMessageRequestEventArgs.MessageTypeEnum.Clear;
                    RollcallSelectMessageRequester.Instance.RollcallSelectMessageRequest(e);
               }), System.Windows.Threading.DispatcherPriority.Render);
               ApplyFilter();
          }

          //FilterByNameCommand
          public ICommand FilterByNameCommand
          {
               get
               {
                    if (_filterByNameCommand == null) {
                         _filterByNameCommand = new RelayCommand(x => {
                              if (SelectedItem == null) return;
                              FilterText = $"{SelectedItem.CurrentPerson.LastName}, {SelectedItem.CurrentPerson.FirstName}";
                              ApplyFilter();
                         });
                    }
                    return _filterByNameCommand;
               }
          }

          public string FilterText
          {
               get { return _filterText; }
               set
               {
                    _filterText = value;
                    OnPropertyChanged(nameof(FilterText));
               }
          }

          public Brush FilterColor
          {
               get
               {
                    if (string.IsNullOrEmpty(FilterText)) {
                         return new SolidColorBrush(Colors.White);
                    } else {
                         return new SolidColorBrush(Colors.Yellow);
                    }
               }
          }

          private string _filterText;

          public void ApplyFilter()
          {
               TraceEx.PrintLog($"Filter Applied: {_filterText}");

               //send roll call select message
               if (!string.IsNullOrEmpty(_filterText)) {
                    DispatcherHelper.GetDispatcher().BeginInvoke(new Action(() => {
                         var e = new RollCallSelectMessageRequestEventArgs();
                         e.query = _filterText;
                         e.MessageType = RollCallSelectMessageRequestEventArgs.MessageTypeEnum.Change;
                         RollcallSelectMessageRequester.Instance.RollcallSelectMessageRequest(e);
                    }), System.Windows.Threading.DispatcherPriority.Render);
               } else if (string.IsNullOrEmpty(_filterText)) {
                    DispatcherHelper.GetDispatcher().BeginInvoke(new Action(() => {
                         var e = new RollCallSelectMessageRequestEventArgs();
                         e.query = _filterText;
                         e.MessageType = RollCallSelectMessageRequestEventArgs.MessageTypeEnum.Clear;
                         RollcallSelectMessageRequester.Instance.RollcallSelectMessageRequest(e);
                    }), System.Windows.Threading.DispatcherPriority.Render);
               }

               OnPropertyChanged(nameof(FilterColor));

               View.Filter = item => {
                    var accessItem = item as AccessEntryViewModel;

                    if (string.IsNullOrEmpty(_filterText)) {
                         return true;
                    }
                    string lastNameFilter = _filterText;
                    string firstNameFilter = _filterText;
                    string companyFilter = _filterText;

                    if (_filterText.Contains(",")) {
                         var split = _filterText.Split(',');
                         lastNameFilter = split[0].Trim();
                         firstNameFilter = split[1].Trim();

                         if (split.Length > 2) {
                              companyFilter = split[2].Trim();
                         } else {
                              companyFilter = string.Empty;
                         }
                         if (accessItem?.CurrentPerson?.LastName.IndexOf(lastNameFilter, StringComparison.CurrentCultureIgnoreCase) >= 0
                              && accessItem?.CurrentPerson?.FirstName.IndexOf(firstNameFilter, StringComparison.CurrentCultureIgnoreCase) >= 0
                              && accessItem?.CurrentPerson?.Company.IndexOf(companyFilter, StringComparison.CurrentCultureIgnoreCase) >= 0) {
                              return true;
                         } else {
                              return false;
                         }
                    }

                    if (accessItem?.CurrentPerson?.LastName.IndexOf(lastNameFilter, StringComparison.CurrentCultureIgnoreCase) >= 0
                    || accessItem?.CurrentPerson?.FirstName.IndexOf(firstNameFilter, StringComparison.CurrentCultureIgnoreCase) >= 0
                    || accessItem?.CurrentPerson?.Company.IndexOf(companyFilter, StringComparison.CurrentCultureIgnoreCase) >= 0) {
                         return true;
                    } else {
                         return false;
                    }
               };
          }

          private ICommand _shiftEntryCommand;
          private ICommand _addVehicleCommand;
          private ICommand _filterByNameCommand;
          private Brush _filterColor = new SolidColorBrush(Colors.Transparent);
          private ICommand _addAwayListCommand;
          private bool _isDateEnabled = false;
          private bool _isLockEnabled = true;
          private bool _canUserSort;
          private ICommand _generateReportCommand;

          public ICommand ShiftEntryCommand
          {
               get
               {
                    if (_shiftEntryCommand == null) {
                         _shiftEntryCommand = new RelayCommand(x => { OpenShiftEntry(); });
                    }
                    return _shiftEntryCommand;
               }
          }

          private void OpenShiftEntry()
          {
               if (SelectedItem == null || SelectedItem.CurrentPerson == null) return;
               var mainWindow = MainWindowViewModel.MainWindowInstance;
               ShiftEntriesViewModel vm = new ShiftEntriesViewModel(ShiftEntriesViewModel.TimeFrameSelection.Month);
               var selectedItem = SelectedItem;
               vm.IsUserSearch = false;
               vm.ShiftEntrySearchList.Add(new ShiftEntrySearch {
                    Company = selectedItem.CurrentPerson?.Company,
                    LastName = selectedItem.CurrentPerson?.LastName,
                    FirstName = selectedItem.CurrentPerson?.FirstName
               });
               vm.ReportName = $"{selectedItem.CurrentPerson?.LastName}, {selectedItem.CurrentPerson?.FirstName}";
               vm.IsUserSearch = true;
               vm.InitializeQueryAndView("AccessEntriesVM");
               mainWindow.ShowView(vm);
          }

          private void ChangeQuery()
          {
               using (var db = NetboxDatabase.GetReadOnlyInstance()) {
                    var query = from x in db.GetContext().AccessEntries
                                where x.DtTm > SelectedDate.Date && x.DtTm < SelectedDate.Date.AddDays(1)
                                orderby x.DtTm descending
                                select x;

                    UpdateFromQuery(query.ToList());
               }
               ApplyFilter();
               lastUpdateTime = DateTime.Now;
               //View.Refresh();
          }

          private void UpdateFromQuery(List<AccessEntry> queryList)
          {
               //Monitor.Enter(netboxDatabase.DatabaseLock);
               DispatcherHelper.GetDispatcher().Invoke(new Action(() => IsBusy = true), DispatcherPriority.Render);
               try {
                    var newList = new List<AccessEntryViewModel>(queryList.Count);
                    foreach (var item in queryList) {
                         PersonViewModel person = null;
                         if (string.IsNullOrEmpty(item.PersonId) == false) {
                              if (DataRepository.PersonDict.ContainsKey(item.PersonId)) {
                                   person = DataRepository.PersonDict[item.PersonId];
                              }
                         }
                         var vm = new AccessEntryViewModel(person, item);
                         //bool exists = AccessEntries.Any(x => item.LogId == x.LogId);

                         //if (!exists) {
#if FILTERTEST
                         if (person != null && (person.LastName.ToLower().Contains("test") == true
                              || person.FirstName.ToLower().Contains("test") == true)) {
                              continue;
                         }
#endif
                         newList.Add(vm);
                         //}
                    }

                    //Monitor.Exit(netboxDatabase.DatabaseLock);

                    AccessEntries = new ObservableCollection<AccessEntryViewModel>(newList);
                    initializeView();
               }
               finally {
                    IsBusy = false;
               }
          }

          private void ChangeDate()
          {
               TraceEx.PrintLog($"AccessEntries:: Date Changed {SelectedDate}");
               AccessEntries.Clear();
               ChangeQuery();
          }

          protected override void OnDispose()
          {
               TraceEx.PrintLog("Disposing AccessEntriesVM");

               var dispatcher = DispatcherHelper.GetDispatcher();
               dispatcher.Invoke(new Action(() => {
                    DataRepository.AccessLogMessageHandler.CollectionChanged -= AccessLogMessageHandler_CollectionChanged;
                    ClockTimer.Tick -= ClockTimer_Tick;
               }), DispatcherPriority.Render);
          }
     }
}