using ReportApp.Data;
using ReportApp.Model;
using ReportApp.Utility;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace ReportApp.ViewModel
{
     /// <summary>
     /// Description of VehicleEntriesViewModel.
     /// </summary>
     public class VehicleEntriesViewModel : WorkspaceViewModel
     {
          #region Fields

          private static SerialTaskQueue _taskQueue;

          private static object TimerLock = new object();

          private static Color activeColor = Colors.Yellow;

          private static Color inactiveColor = Colors.Transparent;

          private ICommand _changeDateCommand;

          /// <summary>
          /// Delete Command
          /// </summary>
          private ICommand _closeOutCommand;

          private ICommand _editCommand;

          private bool _IsActive;

          private MainWindowViewModel _mainWindow;

          private string _nameSortDirection = String.Empty;

          /// <summary>
          /// Print Report Command
          /// </summary>
          private ICommand _printReportCommand;

          /// <summary>
          /// RowEditCommand
          /// </summary>
          private ICommand _rowEditCommand;

          private ICommand _cellEditCommand;

          //private DispatcherTimer timer;
          private DispatcherTimer ClockTimer = new DispatcherTimer();

          private DateTime lastDateUpdated = DateTime.MinValue;

          private string vehicleDbPath = null;

          private ICollectionView _view;

          private ShowTypeEnum _showType = ShowTypeEnum.ShowActive;

          private ICommand _deleteCommand;

          private RelayCommand _filterCommand;

          private bool _emptyButtonEnabled;

          private bool _allButtonEnabled;

          private bool _activeButtonEnabled;

          private Brush _allButtonBackground = new SolidColorBrush(inactiveColor);

          private Brush _activeButtonBackground = new SolidColorBrush(activeColor);

          private Brush _emptyButtonBackground = new SolidColorBrush(inactiveColor);

          private bool _canUserSort;

          private ICommand _refreshCommand;
          private bool _isLockEnabled = true;
          private bool _isDateEnabled;
          private DateTime _selectedDate;
          private ICommand _generateReportCommand;

          #endregion Fields

          #region Constructors

          //@"c:\CTApp\DB\Vehicle.sqlite"
          public VehicleEntriesViewModel(string vehicleDbPath)
          {
               InitializeConstructor(vehicleDbPath);
          }

          public VehicleEntriesViewModel()
          {
               InitializeConstructor(PathSettings.Default.VehicleDatabasePath);
               Initialize();

               ApplyFilter();
          }

          #endregion Constructors

          #region Delegates

          public delegate void ScrollIntoViewDelegateSignature(ScrollVehicleEntryEventArgs objEvent);

          #endregion Delegates

          #region Enums

          public enum ShowTypeEnum
          {
               ShowAll,
               ShowActive,
               ShowEmpty
          }

          #endregion Enums

          #region Properties

          public ScrollIntoViewDelegateSignature ScrollIntoView { get; set; }

          //FilterCommand
          public ICommand FilterCommand
          {
               get
               {
                    if (_filterCommand == null) {
                         _filterCommand = new RelayCommand(FilterCommandExecute);
                    }
                    return _filterCommand;
               }
          }

          //RefreshCommand
          public ICommand RefreshCommand
          {
               get
               {
                    if (_refreshCommand == null) {
                         _refreshCommand = new RelayCommand(async x => await Refresh());
                    }
                    return _refreshCommand;
               }
          }

          public ICommand ChangeDateCommand
          {
               get
               {
                    if (_changeDateCommand == null) {
                         _changeDateCommand = new RelayCommand(async x => await ChangeDate());
                    }
                    return _changeDateCommand;
               }
          }

          public ICommand CloseOutCommand
          {
               get
               {
                    if (_closeOutCommand == null) {
                         _closeOutCommand = new RelayCommand(x => CloseOut());
                    }
                    return _closeOutCommand;
               }
          }

          public ICommand DeleteCommand
          {
               get
               {
                    if (_deleteCommand == null) {
                         _deleteCommand = new RelayCommand(x => Delete());
                    }
                    return _deleteCommand;
               }
          }

          public ICommand EditCommand
          {
               get
               {
                    if (_editCommand == null) {
                         _editCommand = new RelayCommand(x => Edit(x));
                    }
                    return _editCommand;
               }
          }

          public ObservableCollection<VehicleEntryViewModel> Entries { get; private set; } = new ObservableCollection<VehicleEntryViewModel>();

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

                    if(value == true) {
                         DispatcherHelper.GetDispatcher().Invoke(LockContent);
                    } else {
                         UnlockContent();
                    }

                    OnPropertyChanged(nameof(IsLockEnabled));
               }
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

          private async Task LockContent()
          {
               TraceEx.PrintLog("User clicked to lock vehicle entries");
               CanUserSort = false;
               await Refresh();
               IsDateEnabled = false;
          }

          private void UnlockContent()
          {
               TraceEx.PrintLog("User clicked to unlock vehicle entries");
               CanUserSort = true;
               IsDateEnabled = true;
          }

          public ICommand GenerateReportCommand
          {
               get
               {
                    if (_generateReportCommand == null) {
                         _generateReportCommand = new RelayCommand(x => {
                              DispatcherHelper.GetDispatcher().Invoke(async () => {
                                   var reportvm = new ReportViewModel(Report.Reports.First(a => a.Name == "Vehicle Entries - By Company"));
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

          public string NameSortDirection
          {
               get
               {
                    return _nameSortDirection;
               }
               set
               {
                    _nameSortDirection = value;
                    OnPropertyChanged(nameof(NameSortDirection));
               }
          }

          public ICommand PrintReportCommand
          {
               get
               {
                    if (_printReportCommand == null) {
                         _printReportCommand = new RelayCommand(async x => await PrintReport());
                    }
                    return _printReportCommand;
               }
          }

          public ICommand RowEditCommand
          {
               get
               {
                    if (_rowEditCommand == null) {
                         _rowEditCommand = new RelayCommand(x => RowEdit());
                    }
                    return _rowEditCommand;
               }
          }

          public ICommand CellEditCommand
          {
               get
               {
                    if (_cellEditCommand == null) {
                         _cellEditCommand = new RelayCommand(x => CellEdit());
                    }
                    return _cellEditCommand;
               }
          }

          public DateTime SelectedDate 
          { 
               get
               {
                    return _selectedDate;
               } 
               set
               {
                    _selectedDate = value;
                    OnPropertyChanged(nameof(SelectedDate));
               } 
          }

          public VehicleEntryViewModel SelectedValue { get; set; }

          public int ShowType
          {
               get
               {
                    return (int)_showType;
               }
               set
               {
                    if (value == (int)_showType) return;
                    _showType = (ShowTypeEnum)value;

                    ScreenShotHelper.SaveScreenShot("VehicleEntries ShowType Changed");
                    
                    OnPropertyChanged(nameof(ShowType));

                    //have to set this way or filter won't always be applied
                    //InitializeEntries(SelectedDate);
                    initializeView();
                    ApplyFilter();
               }
          }

          public ICollectionView View
          {
               get { return _view; }
               set
               {
                    _view = value;
                    OnPropertyChanged(nameof(View));
               }
          }

          public DateTime LastTime { get; private set; } = DateTime.Now;

          public bool EmptyButtonEnabled
          {
               get
               {
                    return _emptyButtonEnabled;
               }
               set
               {
                    _emptyButtonEnabled = value;
                    OnPropertyChanged(nameof(EmptyButtonEnabled));
               }
          }

          public bool AllButtonEnabled
          {
               get
               {
                    return _allButtonEnabled;
               }
               set
               {
                    _allButtonEnabled = value;
                    OnPropertyChanged(nameof(AllButtonEnabled));
               }
          }

          public bool ActiveButtonEnabled
          {
               get
               {
                    return _activeButtonEnabled;
               }
               set
               {
                    _activeButtonEnabled = value;
                    OnPropertyChanged(nameof(ActiveButtonEnabled));
               }
          }

          public Brush AllButtonBackground
          {
               get
               {
                    return _allButtonBackground;
               }
               set
               {
                    _allButtonBackground = value;
                    OnPropertyChanged(nameof(AllButtonBackground));
               }
          }

          public Brush ActiveButtonBackground
          {
               get
               {
                    return _activeButtonBackground;
               }
               set
               {
                    _activeButtonBackground = value;
                    OnPropertyChanged(nameof(ActiveButtonBackground));
               }
          }

          public Brush EmptyButtonBackground
          {
               get
               {
                    return _emptyButtonBackground;
               }
               set
               {
                    _emptyButtonBackground = value;
                    OnPropertyChanged(nameof(EmptyButtonBackground));
               }
          }

          private SerialTaskQueue TaskQueue
          {
               get
               {
                    if (_taskQueue == null) {
                         _taskQueue = new SerialTaskQueue("Vehicle");
                         _taskQueue.StartUpdateTask();
                    }
                    return _taskQueue;
               }
          }

          private bool today => DateTime.Now.Date == SelectedDate.Date;

          #endregion Properties

          #region Methods

          public void ApplyFilter()
          {
               View.Filter = item => {
                    var vehicleItem = item as VehicleEntryViewModel;

                    switch ((ShowTypeEnum)ShowType) {
                         case ShowTypeEnum.ShowAll:
                              return true;

                         case ShowTypeEnum.ShowActive:
                              return vehicleItem.OutTime == DateTime.MinValue || vehicleItem.IsPassEmpty;

                         case ShowTypeEnum.ShowEmpty:
                              return vehicleItem.IsPassEmpty;
                    }
                    return true;
               };
          }

          public void AddEntry(VehicleEntryViewModel entry)
          {
               if (HandleNullEntry("Add", entry)) {
                    //TraceEx.PrintLog($"AddEntry:: HandleNullEntry == true");
                    return;
               }

               //get id and set
               using (var db = VehicleDatabase.GetWriteInstance()) {
                    entry.EntryId = db.GetNextId();
                    db.AddEntry(entry.Entry);
               }

               DispatcherHelper.GetDispatcher().Invoke(new Action(() => {
                    //TraceEx.PrintLog($"VehicleEntriesVM: Adding vehicle entry: {entry} START");

                    //check to see if entry already exists
                    Entries.Add(entry);
                    
                    ScrollIntoView(new ScrollVehicleEntryEventArgs() { Entry = entry });
                    //TraceEx.PrintLog($"VehicleEntriesVM: Adding vehicle entry: {entry} END");
               }));
          }

          public void ChangeEntry(VehicleEntryViewModel entry)
          {
               if (HandleNullEntry("Change", entry)) {
                    return;
               }
               //TraceEx.PrintLog($"VehicleEntriesVM:ChangeEntry START: p={entry.PersonId} l={entry.LicNumber} tag={entry.TagNum}");

               using (var db = VehicleDatabase.GetWriteInstance()) {
                    db.EditEntry(entry.Entry);
               }

               DispatcherHelper.GetDispatcher().Invoke(new Action(() => {
                    //TraceEx.PrintLog($"VehicleEntriesVM:ChangeEntry IN DISPATCHER: p={entry.PersonId} l={entry.LicNumber} tag={entry.TagNum} START");
                    var old = Entries.Where(e => e.EntryId == entry.EntryId);
                    if (old.Any()) {
                         var first = old.First();

                         if (today && first.InTime.Date == DateTime.Now.Date.AddDays(-1).Date && !first.HasNoOutTime) {
                              //remove if this was a carried over entry from the previous day
                              //TraceEx.PrintLog($"ChangeEntry::Removing: {first}");
                              Entries.Remove(first);
                         } else {
                              first.CopyFrom(entry);
                         }
                    } else {
                         //TraceEx.PrintLog($"VehicleEntriesVM:ChangeEntry: NOT FOUND p={entry.PersonId} l={entry.LicNumber}");
                    }
                    //TraceEx.PrintLog($"VehicleEntriesVM:ChangeEntry IN DISPATCHER: p={entry.PersonId} l={entry.LicNumber} tag={entry.TagNum} END");
               }));
          }

          public void DeleteEntry(VehicleEntryViewModel entry)
          {
               if (HandleNullEntry("Delete", entry)) {
                    return;
               } else {
                    DispatcherHelper.GetDispatcher().Invoke(new Action(() => {
                         //TraceEx.PrintLog($"VehicleEntriesVM: Delete vehicle entry dispatcher: {entry.PersonId} {entry.LicNumber} START");
                         var old = Entries.Where(e => e.EntryId == entry.EntryId);
                         if (!old.Any()) {
                              Trace.TraceWarning("VehicleEntriesViewModel: DeleteEntry could not find entryid " + entry.EntryId);
                         }

                         Entries.Remove(old.First());
                         //TraceEx.PrintLog($"VehicleEntriesVM: Delete vehicle entry dispatcher: {entry.PersonId} {entry.LicNumber} END");
                    }));
               }
          }

          /// <summary>
          /// Adds pass to entry with given id
          /// - avoid datarace if pass out time is being edited at same time
          /// </summary>
          /// <param name="passNumber"></param>
          /// <param name="entryId"></param>
          public void AddPass(string passNumber, long entryId)
          {
               //TraceEx.PrintLog($"VehicleEntriesVM: Add Pass: Adding to queue: p={passNumber}");
               TaskQueue.QueueTaskEnd($"Add pass {passNumber}", new Task(() => {
                    //TraceEx.PrintLog($"VehicleEntriesVM: Add Pass: In queue: p={passNumber}");

                    //change db
                    using (var db = VehicleDatabase.GetWriteInstance()) {
                         var dbEntry = db.GetEntry(entryId);
                         if (dbEntry == null) {
                              string message = "VehicleEntriesVM:: Could not load entry to save to DB";
                              Trace.TraceError(message);
                              MainWindowViewModel.MainWindowInstance.PrintStatusText("AddPass: Could not load from db to save changes", Brushes.Red);
                              return;
                         }

                         TraceEx.PrintLog($"VehicleEntriesVM: Add Pass: Changing DB: p={passNumber} person={dbEntry.PersonId}");

                         dbEntry.TagNum = passNumber;
                         db.SubmitChanges();
                    }
               }));
          }

          //call once application is open
          public void Initialize()
          {
               //TaskQueue.WriteToTrace = true;
               //database.SetDebugText();

               //Since Date function does not seem to work with linq query in data.sqlite
               //we must first obtain the data, put it in a array, and then filter by date

               SelectedDate = DateTime.Today;
               InitializeLocalDataEntries();
               initializeView();
               var dispatcher = DispatcherHelper.GetDispatcher();
               dispatcher.Invoke(new Action(() => {
                    DataRepository.PersonChanged += DataRepository_PersonChanged;
                    DataRepository.AccessLogMessageHandler.CollectionChanged += AccessLogMessageHandler_CollectionChanged;
                    VehiclePassMessageRequester.Instance.VehiclePassRequestEvent += VehiclePassRequestEvent;
               }));
          }

          //called by AccessLogMessageHandler_CollectionChanged
          //called by DataRepository_PersonChanged
          public void ProcessAccessEntries(AccessEntry[] accessList)
          {
               Benchmarker.Start("Vehicle:ProcessAccessEntries");
               //TraceEx.PrintLog("Vehicle:ProcessAccessEntries");

               foreach (var entry in accessList) {
                    PersonViewModel person;

                    if (entry.PersonId == null) {
                         continue;
                    }

                    if (DataRepository.PersonDict.ContainsKey(entry.PersonId)) {
                         person = (PersonViewModel)DataRepository.PersonDict[entry.PersonId].Copy();
                    } else {
                         MainWindowViewModel.MainWindowInstance.PrintStatusText($"Error loading person: {entry.PersonId}", Brushes.Red);
                         Trace.TraceWarning("Vehicle: ProcessAccessEntries: could not find personid=" + entry.PersonId);
                         continue;
                    }

                    if (person.VehicleReader == 0 && entry.ReaderKey == ReaderKeyEnum.AdminIn
                     || person.VehicleReader == 0 && entry.ReaderKey == ReaderKeyEnum.TestIn
                     || person.VehicleReader == 1 && entry.ReaderKey == ReaderKeyEnum.ControlIn) {
                         //if 1st vehicle is empty or are no vehicles then skip processing
                         if (!person.VehicleList.Any() || person.VehiclesActivated == false || (person.VehicleList.Count == 1 && String.IsNullOrEmpty(person.VehicleList[0].LicNum))) {
                              continue;
                         }
                         ProcessInEntry(entry, person.InternalPerson);
                    } else if (entry.ReaderKey == ReaderKeyEnum.AdminOut ||
                               entry.ReaderKey == ReaderKeyEnum.CPOut ||
                               entry.ReaderKey == ReaderKeyEnum.ControlOut ||
                               entry.ReaderKey == ReaderKeyEnum.TestOut) {
                         //if 1st vehicle is empty or are no vehicles then skip processing
                         if (!person.VehicleList.Any() || (person.VehicleList.Count == 1 && String.IsNullOrEmpty(person.VehicleList[0].LicNum))) {
                              continue;
                         }
                         ProcessOutEntry(entry);
                    }
               }
               Benchmarker.Stop("Vehicle:ProcessAccessEntries");
          }

          protected override void OnDispose()
          {
               var dispatcher = DispatcherHelper.GetDispatcher();
               dispatcher.Invoke(new Action(() => {
                    DataRepository.PersonChanged -= DataRepository_PersonChanged;
                    DataRepository.AccessLogMessageHandler.CollectionChanged -= AccessLogMessageHandler_CollectionChanged;
               }));
          }

          private void FilterCommandExecute(object obj)
          {
               var param = obj as string;

               AllButtonBackground = new SolidColorBrush(inactiveColor);
               EmptyButtonBackground = new SolidColorBrush(inactiveColor);
               ActiveButtonBackground = new SolidColorBrush(inactiveColor);

               switch (param) {
                    case "All":
                         ShowType = (int)ShowTypeEnum.ShowAll;
                         AllButtonBackground = new SolidColorBrush(activeColor);
                         //TraceEx.PrintLog("VehicleEntries: Filter set to all");
                         break;

                    case "Empty":
                         ShowType = (int)ShowTypeEnum.ShowEmpty;
                         EmptyButtonBackground = new SolidColorBrush(activeColor);
                         //TraceEx.PrintLog("VehicleEntries: Filter set to empty");
                         break;

                    case "Active":
                         ShowType = (int)ShowTypeEnum.ShowActive;
                         ActiveButtonBackground = new SolidColorBrush(activeColor);
                         //TraceEx.PrintLog("VehicleEntries: Filter set to active");
                         break;
               }
          }

          /// <summary>
          /// Only changes out time to prevent data race on edits
          /// </summary>
          /// <param name="entryId"></param>
          /// <param name="outTime"></param>
          private void ChangeOutTime(VehicleEntry elem)
          {
               using (var db = VehicleDatabase.GetWriteInstance()) {
                    var dbEntry = db.GetEntry(elem.EntryId);
                    dbEntry.OutTime = elem.OutTime;
                    dbEntry.OutId = elem.OutId;
                    //TraceEx.PrintLog($"ChangeOutTime: db {dbEntry.OutTime} {dbEntry.OutId}");

                    dbEntry.OutTime = elem.OutTime;
                    dbEntry.OutId = elem.OutId;
                    //TraceEx.PrintLog($"ChangeOutTime: db {dbEntry.OutTime} {dbEntry.OutId}");

                    db.SubmitChanges();
               }

               DispatcherHelper.GetDispatcher().Invoke(new Action(() => {
                    //TraceEx.PrintLog("ChangeOutTime: Dispatcher invoke START");
                    var findObservableElement = Entries.Where(x => x.EntryId == elem.EntryId);
                    if (findObservableElement.Any()) {
                         VehicleEntryViewModel v = findObservableElement.First();
                         v.OutTime = elem.OutTime;
                         v.OutId = elem.OutId;
                         //TraceEx.PrintLog($"ChangeOutTime SetObsElem:: on obsElem={v} inputElem={elem}");

                         //entries from previous day should be cleared
                         if ((v.InTime < DateTime.Today) && (v.OutTime.Date == DateTime.Today)) {
                              Entries.Remove(v);
                         }
                    }

                    //force update Entries observable collection
                    OnPropertyChanged(nameof(Entries));
                    //TraceEx.PrintLog("ChangeOutTime: Dispatcher invoke END");
               }));
          }

          private void VehiclePassRequestEvent(VehiclePassRequestEventArgs e)
          {
               //find next entry id

               ScreenShotHelper.SaveScreenShot("Right click pass added");

               var person = DataRepository.PersonDict[e.PersonId];

               VehicleEntryViewModel entry = new VehicleEntryViewModel(new VehicleEntry()) {
                    PersonId = e.PersonId,
                    Deleted = false,
                    InTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second),
               };

               TraceEx.PrintLog("Vehicle: Adding Pass from right click");
               TraceEx.PrintLog($"entry={entry}");
               if (person.VehicleList.Any()) {
                    entry.LicNumber = person.VehicleList[0].LicNum;
               }

               AddEntry(entry);
          }

          private void initializeView()
          {
               //ReportApp.exe Error: 0 : System.InvalidOperationException
               //'Sorting' is not allowed during an AddNew or EditItem transaction.
               DispatcherHelper.GetDispatcher().Invoke(new Action(() => {
                    //ensure any edits are commited before re-initializing
                    //can be caused by filter being applied during edit - when this happens the edit is not canceled
                    if (View != null) {
                         var v = View as IEditableCollectionView;

                         if (v != null && v.IsEditingItem) {
                              v.CommitEdit();
                         } else if (v != null && v.IsAddingNew) {
                              v.CommitNew();
                         }
                    }
                    View = CollectionViewSource.GetDefaultView(Entries);
                    ICollectionViewLiveShaping viewshaping = (ICollectionViewLiveShaping)_view;

                    viewshaping.LiveFilteringProperties.Add("OutId");
                    viewshaping.LiveFilteringProperties.Add("IsPassEmpty");
                    viewshaping.LiveFilteringProperties.Add("TagNum");
                    viewshaping.IsLiveFiltering = true;
                    resetSort();
               }));

               OnPropertyChanged(nameof(Entries));
          }

          //called by RefreshCommand
          private async Task Refresh()
          {
               ScreenShotHelper.SaveScreenShot("VehicleRefresh");
               SelectedDate = DateTime.Today;
               await InitializeRefresh();

               MainWindowViewModel.MainWindowInstance.PrintStatusText("Vehicle entries refreshed", Brushes.Black);

               //TraceEx.PrintLog("VehicleEntriesVM: Refresh");
               Trace.TraceWarning("User clicked Refresh on vehicle system");
          }

          private void resetSort()
          {
               View.SortDescriptions.Clear();
               View.SortDescriptions.Add(new SortDescription("InTime", ListSortDirection.Ascending));
               View.Refresh();
          }

          private void AccessLogMessageHandler_CollectionChanged(CollectionChangedEventArgs e)
          {
               var entry = (e.ChangedValue as AccessEntry);

               //TraceEx.PrintLog($"VehicleSystem::AccessLogMessageHandler::CollectionChanged p={entry?.PersonId} p={entry?.LogId}");

               var task = new Task(() => {
                    //TraceEx.PrintLog($"VehicleSystem::AccessLogMessageHandler::CollectionChanged Task Start SelectedDate={SelectedDate} entry.DtTm={entry.DtTm}");
                    if (SelectedDate.Date == DateTime.Today && entry.DtTm.Date == DateTime.Today) {
                         //TraceEx.PrintLog($"Recieved access message p={entry.PersonId} id={entry.LogId}");
                         ProcessAccessEntries(new AccessEntry[] { entry });
                    }
                    //TraceEx.PrintLog("AccessLogMessageHandler TASK DONE");
               });

               TaskQueue.QueueTaskEnd("CollectionChanged", task);
          }

          /// <summary>
          /// User has changed the selected date
          /// </summary>
          private async Task ChangeDate()
          {
               ScreenShotHelper.SaveScreenShot("VehicleEntries ChangeDate");

               //this is a workaround to deal with WPF bug that causes event to be fired twice in certain situations
               if (SelectedDate.Date != lastDateUpdated.Date || lastDateUpdated == DateTime.MinValue) {
                    lastDateUpdated = SelectedDate;
                    await InitializeRefresh();
               }
          }

          private async Task InitializeRefresh()
          {
               Entries = new ObservableCollection<VehicleEntryViewModel>();
               OnPropertyChanged(nameof(IsActive));
               await UpdateDataAsync(SelectedDate);
               initializeView();
               ApplyFilter();
          }

          private bool CheckSelectedValueIsNull(string command, bool showMessage = true)
          {
               if (SelectedValue == null) {
                    if (showMessage) {
                         MessageBox.Show("Can't execute command: " + command + ": Nothing is selected.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                    return true;
               }
               return false;
          }

          private async void ClockTimer_Tick(object sender, EventArgs e)
          {
               OnPropertyChanged(nameof(IsActive));
               UpdateHours();

               //check if last time was before midnight and current time is greater than
               var now = DateTime.Now;
               var midnight = DateTime.Today;
               if (LastTime < midnight && now >= midnight) {
                    SelectedDate = now.Date;
                    OnPropertyChanged(nameof(SelectedDate));
                    await ChangeDate();
               }

               LastTime = DateTime.Now;
          }

          private void DataRepository_PersonChanged(DictionaryChangedEventArgs e)
          {
               var person = (PersonViewModel)(e.ChangedValue as PersonViewModel).Copy();

               TaskQueue.QueueTaskEnd("PersonChanged", new Task(() => {
                    if (person.PersonId != null) {
                         //TraceEx.PrintLog($"Vehicle::DataRepository_PersonChanged -- Person changed {person}");
                         UpdateAccessData(SelectedDate, person.PersonId);
                    }
               }));
          }

          private void CloseOut()
          {
               if (CheckSelectedValueIsNull("CloseOut") || SelectedValue.OutTime != DateTime.MinValue) {
                    return;
               }

               ScreenShotHelper.SaveScreenShot("VehicleEntries CloseOut called");
               SelectedValue.OutTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second);
               ChangeEntry(SelectedValue);
               TraceEx.PrintLog("VehicleEntries: User closed out");
               
               MainWindowViewModel.MainWindowInstance.PrintStatusText("Vehicle entry closed out", Brushes.Black);
          }

          private void Delete()
          {
               if (CheckSelectedValueIsNull("Delete")) {
                    return;
               }

               //copy to local variable to ensure that selected value doesn't change
               var selectedVal = SelectedValue;

               var res = MessageBox.Show("Are you sure you want to delete this entry?", "Delete Warning", MessageBoxButton.YesNo, MessageBoxImage.Question);

               if (res == MessageBoxResult.Yes) {
                    selectedVal.Deleted = true;
                    ChangeEntry(selectedVal);
                    DeleteEntry(selectedVal);
                    MainWindowViewModel.MainWindowInstance.PrintStatusText("Vehicle entry deleted", Brushes.Black);
               }
          }

          private void Edit(object personId)
          {
               string _personId = personId == null ? SelectedValue?.PersonId : (string)personId;

               var query = Entries.Where(p => p.Person.PersonId == _personId);

               if (!query.Any()) {
                    Trace.TraceWarning("Vehicle: Edit: Could not find id");
                    System.Windows.MessageBox.Show("There was a problem opening this person.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
               }

               PersonViewModel person = query.First().Person;
               var vm = AddPersonViewModel.CreateAsync(person, true);
               vm.VehiclesExpanded = true;
               _mainWindow.ShowView(vm);
          }

          private bool HandleNullEntry(string command, VehicleEntryViewModel entry)
          {
               if (entry == null) {
                    MessageBox.Show("There was a problem executing this command(Entry is 'Null')");
                    return true;
               }
               return false;
          }

          private void InitializeConstructor(string vehicleDbPath)
          {
               _mainWindow = MainWindowViewModel.MainWindowInstance;

               base.DisplayName = "Vehicle Entries";

               this.vehicleDbPath = vehicleDbPath;

               OnlyOneCanRun = true;

               ClockTimer.Interval = new TimeSpan(0, 0, 5);
               ClockTimer.Tick += ClockTimer_Tick;
               ClockTimer.Start();
          }

          //only called when loading for the first time or changing date
          //this only places existing entries in obscollection
          private void InitializeEntries(DateTime date)
          {
               DisableButtons();
               using (var db = VehicleDatabase.GetWriteInstance()) {
                    IEnumerable<VehicleEntry> query = db.GetTodayQuery(date);
                    IEnumerable<VehicleEntry> leftFromYesterdayQuery = db.GetYesterdayQuery(date);

                    DispatcherHelper.GetDispatcher().Invoke(new Action(() => {
                         Entries.Clear();
                    }));

                    if (query.Any() || leftFromYesterdayQuery.Any()) {
                         if (date.Date == DateTime.Today.Date) {
                              if (leftFromYesterdayQuery.Any()) {
                                   foreach (VehicleEntry entry in leftFromYesterdayQuery) {
                                        DispatcherHelper.GetDispatcher().Invoke(new Action(() => {
                                             Entries.Add(new VehicleEntryViewModel(entry));
                                        }));
                                   }
                              }
                         }
                         foreach (VehicleEntry entry in query) {
                              DispatcherHelper.GetDispatcher().Invoke(new Action(() => {
                                   Entries.Add(new VehicleEntryViewModel(entry));
                              }));
                         }
                    }

                    EnableButtons();

                    //if it is todays date, run through the access data again
                    if (SelectedDate.Date == DateTime.Today) {
                         UpdateAccessData(DateTime.Today);
                    }
               }
          }

          //called at load
          private void InitializeLocalDataEntries()
          {
               InitializeEntries(SelectedDate);
          }

          private async Task PrintReport()
          {
               string reportName = "Vehicle Entries - By Time";

               if (NameSortDirection == "Ascending") {
                    reportName = "Vehicle Entries - By Name";
               }
               await ReportPrinter.PrintReport(reportName, Report.Reports, SelectedDate);
          }

          private void ProcessInEntry(AccessEntry entry, Person person)
          {
               using (var db = VehicleDatabase.GetReadOnlyInstance()) {
                    //TraceEx.PrintLog($"VehicleSystem::ProcessInEntry AccessEntry={entry}");
                    var vehicleDBList = db.GetTable();

                    bool exists = vehicleDBList.Any(x => x.InId == entry.LogId);
                    if (exists) {
                         //TraceEx.PrintLog($"VehicleSystem::ProcessInEntry RETURN ON EXISTS"); 
                         return;
                    }

                    bool alreadyHasEntry = vehicleDBList.Any(x => entry.PersonId == x.PersonId && x.Deleted == false && x.OutId == 0 && x.InTime > DateTime.Now.Date && x.InTime < DateTime.Now.Date.AddDays(1));
                    if (alreadyHasEntry) {
                         //TraceEx.PrintLog($"VehicleSystem::ProcessInEntry RETURN ON ALREADYHASENTRY");
                         return;
                    }

                    bool isDuplicate = vehicleDBList.Any(v => v.PersonId == entry.PersonId && (entry.DtTm >= v.InTime && entry.DtTm <= v.OutTime));
                    if (isDuplicate) {
                         //TraceEx.PrintLog($"VehicleSystem::ProcessInEntry RETURN ON ISDUPLICATE");
                         return;
                    }
               }
               if (entry.DtTm.Date == DateTime.Now.Date) {
                    var newEntry = new VehicleEntryViewModel(new VehicleEntry());
                    newEntry.EntryId = 0;
                    newEntry.InTime = entry.DtTm;
                    newEntry.InId = entry.LogId;
                    newEntry.PersonId = person.PersonId;

                    newEntry.TagNum = person.VehicleList[0].TagNum;
                    newEntry.LicNumber = person.VehicleList[0].LicNum;
                    newEntry.Make = person.VehicleList[0].Make;
                    newEntry.Color = person.VehicleList[0].Color;
                    newEntry.Model = person.VehicleList[0].Model;
                    AddEntry(newEntry);
               } //if found then we don't want to add here
          }

          private void ProcessOutEntry(AccessEntry accessEntry)
          {
               bool alreadyUsed = false;
               bool any = false;
               IQueryable<VehicleEntry> search = null;
               using (var db = VehicleDatabase.GetWriteInstance()) {
                    var vehicleDBList = db.GetTable();

                    //look for vehicle with no 'outtime'
                    search = from x in vehicleDBList
                             where x.OutId == 0 && x.PersonId == accessEntry.PersonId && accessEntry.DtTm > x.InTime && x.Deleted == false && x.InTime > SelectedDate.Date.AddHours(-12) && x.InTime < SelectedDate.Date.AddHours(34)
                             select x;

                    alreadyUsed = vehicleDBList.Any(x => x.OutId == accessEntry.LogId);
                    any = search != null ? search.Any() : false;

                    if (any && !alreadyUsed) {
                         //Edit
                         //TraceEx.PrintLog($"ProcessOutEntry any && !alreadyUsed:: entry={accessEntry}");
                         VehicleEntry foundElem = null;
                         VehicleEntry[] array = search.ToArray();
                         foundElem = array.First();
                         foundElem.OutId = accessEntry.LogId;
                         foundElem.OutTime = accessEntry.DtTm;

                         ChangeOutTime(foundElem);
                    }
               }
          }

          private void RowEdit()
          {
               if (CheckSelectedValueIsNull("RowEdit", showMessage: false)) {
                    return;
               }

               DispatcherHelper.GetDispatcher().Invoke(new Action(() => {
                    try {
                         View.Refresh();
                    }
                    catch (InvalidOperationException) {
                         //unsure of why this works sometimes but other times throws exception
                         //it works fine this way, but it would be nice to know why it needs to be done
                         //this way
                    }
               }));
          }

          private void CellEdit()
          {
               if (CheckSelectedValueIsNull("CellEdit")) {
                    return;
               }
               ChangeEntry(SelectedValue);
          }

          //called by DataRepository_PersonChanged
          private async void UpdateAccessData(DateTime date, string personid)
          {
               //do not update if more than 2 days back
               if ((DateTime.Now.Date - date.Date).Days > 2) {
                    return;
               }

               AccessEntry[] array = Array.Empty<AccessEntry>();
               bool isAnyResults = false;

               //get access log for this person, place it on taskqueue to reduce locking time
               var task = new Task(() => {
                    using (var netboxdb = NetboxDatabase.GetReadOnlyInstance()) {
                         var query = from x in netboxdb.GetContext().AccessEntries
                                     where x.DtTm > date.Date.AddHours(-22) && x.DtTm < date.AddHours(24) && x.PersonId == personid
                                     select x;
                         array = query.ToArray();
                         isAnyResults = array.Any();
                    }
               });

               GlobalScheduler.DBQueue.QueueTaskEnd("Vehicle: Get person data", task);

               //TraceEx.PrintLog("UpdateAccessData:: Waiting for DB taskqueue");
               await task;
               //TraceEx.PrintLog("UpdateAccessData:: Waiting for DB taskqueue DONE");

               var updateTask = new Task(() => {
                    if (isAnyResults) {
                         ProcessAccessEntries(array);
                    }
               });

               TaskQueue.QueueTaskEnd("VehicleUpdate", updateTask);

               //TraceEx.PrintLog("UpdateAccessData:: Waiting for Vehicle taskqueue");
               await updateTask;
               //TraceEx.PrintLog("UpdateAccessData:: Waiting for Vehicle taskqueue DONE");
          }

          private async void UpdateAccessData(DateTime date)
          {
               AccessEntry[] array;
               bool isAnyResults = false;
               using (var netboxdb = NetboxDatabase.GetReadOnlyInstance()) {
                    var query = from x in netboxdb.GetContext().AccessEntries
                                where x.DtTm > date.Date.AddHours(-22) && x.DtTm < date.AddHours(24)
                                select x;
                    array = query.ToArray();
                    isAnyResults = array.Any();
               }
               var updateTask = new Task(() => {
                    DisableButtons();

                    if (isAnyResults) {
                         ProcessAccessEntries(array);
                    }

                    EnableButtons();
               });

               TaskQueue.QueueTaskEnd("VehicleUpdate", updateTask);

               await updateTask;
          }

          private void EnableButtons()
          {
               DispatcherHelper.GetDispatcher().Invoke(new Action(() => {
                    ActiveButtonEnabled = true;
                    AllButtonEnabled = true;
                    EmptyButtonEnabled = true;
               }));
          }

          private void DisableButtons()
          {
               DispatcherHelper.GetDispatcher().Invoke(new Action(() => {
                    ActiveButtonEnabled = false;
                    AllButtonEnabled = false;
                    EmptyButtonEnabled = false;
               }));
          }

          //called after form has been initialized
          private async Task UpdateDataAsync(DateTime date)
          {
               TaskQueue.RemoveAll();
               if (TaskQueue.RunningTask?.Name == date.ToString()) {
                    return;
               }

               var task = new Task(() => {
                    InitializeEntries(date);
               });
               TaskQueue.QueueTaskEnd(date.ToString(), task);
               
               //TraceEx.PrintLog("VehicleEntriesVM::UpdateDataAsync: await task START");
               await task;
               //TraceEx.PrintLog("VehicleEntriesVM::UpdateDataAsync: await task END");
          }

          private void UpdateHours()
          {
               foreach (var e in Entries) {
                    e.RefreshHoursIn();
               }
          }

          #endregion Methods
     }

     public class ScrollVehicleEntryEventArgs
     {
          #region Properties

          public VehicleEntryViewModel Entry { get; set; }

          #endregion Properties
     }
}

public delegate void VehiclePassRequestEventHandler(VehiclePassRequestEventArgs e);

public class VehiclePassRequestEventArgs : EventArgs
{
     #region Fields

     public string PersonId;

     #endregion Fields
}

public class VehiclePassMessageRequester
{
     #region Fields

     public static VehiclePassMessageRequester _instance;

     #endregion Fields

     #region Events

     public event VehiclePassRequestEventHandler VehiclePassRequestEvent;

     #endregion Events

     #region Properties

     public static VehiclePassMessageRequester Instance
     {
          get
          {
               if (_instance == null) {
                    _instance = new VehiclePassMessageRequester();
               }
               return _instance;
          }
     }

     #endregion Properties

     #region Methods

     public void VehiclePassRequest(VehiclePassRequestEventArgs e)
     {
          if (VehiclePassRequestEvent != null) {
               VehiclePassRequestEvent(e);
          }
     }

     #endregion Methods
}