using ReportApp.Data;
using ReportApp.Model;
using ReportApp.Utility;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Serialization;

namespace ReportApp.ViewModel
{
     public class ShiftEntrySearch : WorkspaceViewModel
     {
          #region Fields

          private string _company = string.Empty;
          private string _firstName = string.Empty;
          private string _lastName = string.Empty;

          #endregion Fields

          #region Properties

          public string Company
          {
               get { return _company; }
               set
               {
                    _company = value;
                    if (value == null) _company = string.Empty;
                    OnPropertyChanged(nameof(Company));

                    if (string.IsNullOrEmpty(Company) == false) {
                         OnPropertyChanged(nameof(LastNameList));
                         OnPropertyChanged(nameof(FirstNameList));
                    }
               }
          }

          public string FirstName
          {
               get { return _firstName; }
               set
               {
                    _firstName = value;
                    if (value == null) _firstName = string.Empty;
                    OnPropertyChanged(nameof(FirstName));
               }
          }

          public string LastName
          {
               get { return _lastName; }
               set
               {
                    _lastName = value;
                    if (value == null) _lastName = string.Empty;

                    OnPropertyChanged(nameof(LastName));
                    OnPropertyChanged(nameof(FirstNameList));
               }
          }

          [System.Xml.Serialization.XmlIgnore]
          public List<string> CompanyList
          {
               get { return DataRepository.CompanyList; }
          }

          [System.Xml.Serialization.XmlIgnore]
          public List<Person> CompanyPersonList
          {
               get
               {
                    TraceEx.PrintLog("ShiftEntriesSearch::CompanyPersonList GET");
                    using (var db = NetboxDatabase.GetReadOnlyInstance()) {
                         var query = from p in db.GetContext().People
                                     where p.Company == this.Company
                                     orderby p.Company
                                     select p;
                         return query.Distinct().ToList();
                    }
               }
          }

          [System.Xml.Serialization.XmlIgnore]
          public List<string> LastNameList
          {
               get
               {
                    if (string.IsNullOrEmpty(Company)) return
                               new List<string> { LastName };
                    TraceEx.PrintLog("ShiftEntriesSearch::LastNameList GET");
                    using (var db = NetboxDatabase.GetReadOnlyInstance()) {
                         var query = (from p in db.GetContext().People
                                      where p.Company == this.Company
                                      select p.LastName)
                                     .Distinct()
                                     .OrderBy(p => p);
                         return query.ToList();
                    }
               }
          }

          [System.Xml.Serialization.XmlIgnore]
          public List<string> FirstNameList
          {
               get
               {
                    if (string.IsNullOrEmpty(Company)) return
                              new List<string> { FirstName };
                    TraceEx.PrintLog("ShiftEntriesSearch::FirstNameList GET");
                    using (var db = NetboxDatabase.GetReadOnlyInstance()) {

                         var query = from p in db.GetContext().People
                                     where p.Company == this.Company && p.LastName == this.LastName
                                     orderby p.FirstName
                                     select p.FirstName;
                         var list = query.ToList();
                         if (list?.Count == 1) {
                              FirstName = list[0];
                              OnPropertyChanged(nameof(FirstName));
                         }
                         return list;
                    }
               }
          }

          #endregion Properties

          #region Methods

          public string GenerateSearchLine()
          {
               return $"(Company like '{Company.Replace("'", "''")}%'and lastname like '{LastName.Replace("'", "''")}%'and firstname like '{FirstName.Replace("'", "''")}%')";
          }

          #endregion Methods
     }

     public class ShiftEntriesViewModel : WorkspaceViewModel, CopyableObject
     {
          #region Fields

          private static object lockObject = new object();
          private ICommand _clearCommand;
          private ICommand _editCommand;

          private ICommand _generateReportCommand;

          /// <summary>
          /// RowEditCommand
          /// </summary>
          private ICommand _rowEditCommand;

          private DateTime _selectedDate;
          private int _selectedSort = 0;
          private bool _showCompany;
          private TimeFrameSelection _timeFrameSelectionIndex = TimeFrameSelection.Day;
          private ICollectionView _view;
          private QueryObserver<ShiftEntryViewModel> Observer;
          private bool _isSearchExpanded = false;

          private bool _isUserSearch = false;
          private List<ShiftEntrySearch> _shiftEntrySearchList = new List<ShiftEntrySearch>();
          private string _reportName;
          private ICommand _savePresetCommand;
          private ICommand _openPresetCommand;
          private DateTime _endDate;
          private Visibility _showAdvancedDateSettings = Visibility.Collapsed;

          private bool _isEnabled;

          #endregion Fields

          #region Constructors

          public ShiftEntriesViewModel()
          {
          }

          public ShiftEntriesViewModel(bool initialize)
          {
               Initialize();
               IsUserSearch = false;
               SelectedDate = DateTime.Today;
               EndDate = SelectedDate;

               if (initialize) {
                    IsUserSearch = true;
               }
               InitializeQueryAndView("Constructor");
          }

          public ShiftEntriesViewModel(TimeFrameSelection selectionType)
          {
               _isUserSearch = false;
               Initialize();
               TimeFrameSelectionIndex = (int)selectionType;

               if (selectionType == TimeFrameSelection.Month) {
                    var today = DateTime.Today;
                    SelectedDate = new DateTime(today.Year, today.Month, 1);
               }
               SelectedSort = (int)SortType.TimeSort;
               InitializeView();
               _isUserSearch = false;
          }

          #endregion Constructors

          #region Enums

          public enum SortType
          {
               GroupByCompany,
               TimeSort
          };

          public enum TimeFrameSelection { Year, Month, Week, Day, Custom };

          #endregion Enums

          //

          #region Properties

          //set to false so multiple property changes don't each execute a separate requery
          [System.Xml.Serialization.XmlIgnore]
          public bool IsUserSearch
          {
               get
               {
                    return _isUserSearch;
               }
               set
               {
                    _isUserSearch = value;
               }
          }

          [System.Xml.Serialization.XmlIgnore]
          public bool IsEnabled
          {
               get
               {
                    return _isEnabled;
               }
               set
               {
                    _isEnabled = value;
                    OnPropertyChanged(nameof(IsEnabled));
               }
          }

          public ICommand ClearCommand
          {
               get
               {
                    if (_clearCommand == null) {
                         _clearCommand = new RelayCommand(x => Clear());
                    }
                    return _clearCommand;
               }
          }

          [System.Xml.Serialization.XmlIgnore]
          public List<string> CompanyList => DataRepository.CompanyList;

          public ICommand EditCommand
          {
               get
               {
                    if (_editCommand == null) {
                         _editCommand = new RelayCommand(x => Edit());
                    }
                    return _editCommand;
               }
          }

          public ICommand GenerateReportCommand
          {
               get
               {
                    if (_generateReportCommand == null) {
                         _generateReportCommand = new RelayCommand(async x => {
                              var reportvm = await GenerateReport();
                              ShowReport(reportvm);

                              GenerateAccessReport();
                         });
                    }
                    return _generateReportCommand;
               }
          }

          //IsSearchExpanded
          [System.Xml.Serialization.XmlIgnore]
          public bool IsSearchExpanded
          {
               get { return _isSearchExpanded; }
               set
               {
                    _isSearchExpanded = value;
                    OnPropertyChanged(nameof(IsSearchExpanded));
               }
          }

          public string ReportName
          {
               get { return _reportName; }
               set
               {
                    _reportName = value;
                    OnPropertyChanged(nameof(ReportName));
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

          public List<ShiftEntrySearch> ShiftEntrySearchList
          {
               get { return _shiftEntrySearchList; }
               set
               {
                    _shiftEntrySearchList = value;
                    OnPropertyChanged(nameof(ShiftEntrySearchList));
               }
          }

          public ICommand SavePresetCommand
          {
               get
               {
                    if (_savePresetCommand == null) {
                         _savePresetCommand = new RelayCommand(x => {
                              try {
                                   TraceEx.PrintLog("ShiftEntriesVM: Save Preset");

                                   if ((TimeFrameSelection)TimeFrameSelectionIndex == TimeFrameSelection.Custom) {
                                        System.Windows.MessageBox.Show("Can not save a preset with a custom set time range.\n\nChange the timeframe to year, month, or week.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                                        return;
                                   }

                                   SaveFileDialog saveFileDialog = new SaveFileDialog();
                                   saveFileDialog.Filter = "Preset File|*.xml";
                                   saveFileDialog.Title = "Save a Preset File";
                                   saveFileDialog.RestoreDirectory = true;
                                   Directory.CreateDirectory(PathSettings.Default.ShiftEntryPresetLocation);
                                   saveFileDialog.InitialDirectory = PathSettings.Default.ShiftEntryPresetLocation;
                                   saveFileDialog.FileName = this.ReportName;
                                   if (saveFileDialog.ShowDialog() == DialogResult.OK) {
                                        if (saveFileDialog.FileName != "") {
                                             XmlSerializer writer = new XmlSerializer(typeof(ShiftEntriesViewModel));
                                             FileStream file = File.Create(saveFileDialog.FileName);
                                             writer.Serialize(file, this);
                                             file.Close();

                                             MainWindowViewModel.MainWindowInstance.PrintStatusText("Preset Saved!", Brushes.DarkBlue);
                                        }
                                   }
                              }
                              catch (Exception e) {
                                   System.Windows.Forms.MessageBox.Show($"Error saving file - {e.Message}");
                              }
                         });
                    }
                    return _savePresetCommand;
               }
          }

          public ICommand OpenPresetCommand
          {
               get
               {
                    if (_openPresetCommand == null) {
                         _openPresetCommand = new RelayCommand(x => {
                              try {
                                   TraceEx.PrintLog("ShiftEntriesVM: Open Preset");

                                   OpenFileDialog openFileDialog = new OpenFileDialog();
                                   openFileDialog.Filter = "Preset File|*.xml";
                                   openFileDialog.Title = "Open a Preset File";
                                   openFileDialog.RestoreDirectory = true;
                                   openFileDialog.InitialDirectory = PathSettings.Default.ShiftEntryPresetLocation;
                                   if (openFileDialog.ShowDialog() == DialogResult.OK) {
                                        if (openFileDialog.FileName != "") {
                                             ShiftEntriesViewModel vm = OpenPreset(openFileDialog.FileName);
                                             this.CopyFromOther(vm);

                                             InitializeQueryAndView("OpenPreset");

                                             MainWindowViewModel.MainWindowInstance.PrintStatusText("Preset Loaded!", Brushes.DarkBlue);
                                        }
                                   }
                              }
                              catch (Exception e) {
                                   System.Windows.Forms.MessageBox.Show($"Error opening file - {e.Message}");
                              }
                         });
                    }
                    return _openPresetCommand;
               }
          }

          [System.Xml.Serialization.XmlIgnore]
          public DateTime SelectedDate
          {
               get { return _selectedDate; }
               set
               {
                    if (value == _selectedDate) return;
                    _selectedDate = value;

                    CalculateEndDate();

                    //set collectionview to null so view does not
                    //update during batch add
                    //var viewCopy = _view;
                    if (_isUserSearch) {
                         InitializeQueryAndView("SelectedDate");
                    }

                    //View = viewCopy;
                    OnPropertyChanged(nameof(SelectedDate));
               }
          }

          [System.Xml.Serialization.XmlIgnore]
          public DateTime EndDate
          {
               get { return _endDate; }
               set
               {
                    if (value == _endDate) return;
                    _endDate = value;

                    //set collectionview to null so view does not
                    //update during batch add
                    //var viewCopy = _view;
                    if (_isUserSearch) {
                         InitializeQueryAndView("EndDate");
                    }

                    //View = viewCopy;
                    OnPropertyChanged(nameof(EndDate));
               }
          }

          public int SelectedSort
          {
               get { return _selectedSort; }
               set
               {
                    if (value == _selectedSort) return;
                    _selectedSort = value;
                    if (_isUserSearch) {
                         InitializeQueryAndView("SelectedSort");
                    }
                    OnPropertyChanged(nameof(SelectedSort));
               }
          }

          [System.Xml.Serialization.XmlIgnore]
          public ShiftEntryViewModel SelectedValue { get; set; }

          public bool ShowCompany
          {
               get { return _showCompany; }
               set
               {
                    _showCompany = value;
                    OnPropertyChanged(nameof(ShowCompany));
               }
          }

          public Visibility ShowAdvancedDateSettings
          {
               get { return _showAdvancedDateSettings; }
               set
               {
                    _showAdvancedDateSettings = value;
                    OnPropertyChanged(nameof(ShowAdvancedDateSettings));
               }
          }

          public int TimeFrameSelectionIndex
          {
               get { return (int)_timeFrameSelectionIndex; }
               set
               {
                    _timeFrameSelectionIndex = (TimeFrameSelection)value;
                    OnPropertyChanged(nameof(TimeFrameSelectionIndex));

                    //auto change date
                    switch (_timeFrameSelectionIndex) {
                         case TimeFrameSelection.Year:
                              SelectedDate = new DateTime(DateTime.Now.Year, 1, 1);
                              ShowAdvancedDateSettings = Visibility.Collapsed;
                              break;

                         case TimeFrameSelection.Month:
                              SelectedDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                              ShowAdvancedDateSettings = Visibility.Collapsed;
                              break;

                         case TimeFrameSelection.Week:
                              var dt = DateTime.Today;
                              var compareDate = DateTime.Today;

                              //find previous sunday (unless it is after tuesday at which point select the previous sunday to that)
                              for (; ; ) {
                                   if (dt.DayOfWeek == DayOfWeek.Sunday && (compareDate - dt).Days > 2) {
                                        break;
                                   }

                                   dt = dt.AddDays(-1);
                              }
                              SelectedDate = dt;
                              ShowAdvancedDateSettings = Visibility.Collapsed;
                              break;

                         case TimeFrameSelection.Custom:
                              ShowAdvancedDateSettings = Visibility.Visible;
                              break;
                    }
                    OnPropertyChanged(nameof(SelectedDate));
                    CalculateEndDate();
               }
          }

          [System.Xml.Serialization.XmlIgnore]
          public ICollectionView View
          {
               get { return _view; }
               set
               {
                    _view = value;
                    OnPropertyChanged(nameof(View));
               }
          }

          #endregion Properties

          #region Methods

          /// <summary>
          /// Exports all presets from main preset directory
          /// </summary>
          public static async Task ExportAllPresets()
          {
               //Get directory location from user
               FolderBrowserDialog dialog = new FolderBrowserDialog();
               dialog.SelectedPath = $"{PathSettings.Default.DefaultSaveLocation}";
               dialog.ShowNewFolderButton = false;
               dialog.Description = "Select the drive to save the exported files to.\nThey will be placed in directory named \"_Files Exported YYYY-MM-DD\"";

               //find first monday
               DateTime first = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
               DateTime curr = first;
               for (; ; ) {
                    if (curr.DayOfWeek == DayOfWeek.Monday) {
                         break;
                    }
                    curr = curr.AddDays(1);
               }
               DateTime firstTuesday = curr.AddDays(1);

               try {
                    if (dialog.ShowDialog() == DialogResult.OK) {
                         MainWindowViewModel.MainWindowInstance.IsBusy = true;

                         string directoryLocation = dialog.SelectedPath;

                         //Create Directory
                         var dirInfo = Directory.CreateDirectory(directoryLocation);

                         string subdirectoryLocation = $"{directoryLocation}\\_Files Exported {DateTime.Today.ToString("yyyy-MM-dd")}";

                         //Create Subdirectory
                         Directory.CreateDirectory(subdirectoryLocation);

                         //Find every preset file
                         var files = Directory.GetFiles(PathSettings.Default.ShiftEntryPresetLocation, "*.xml");

                         //For each preset file
                         foreach (var file in files) {
                              //Open preset - get ShiftEntriesVM
                              var shiftEntriesVM = OpenPreset(file);

                              //only export month report if it is the first monday or tuesday of the month
                              if (shiftEntriesVM.TimeFrameSelectionIndex == (int)ShiftEntriesViewModel.TimeFrameSelection.Month
                                   && DateTime.Today > firstTuesday) {
                                   continue;
                              }

                              //do not export any reports set to 'day' or 'year'
                              if (shiftEntriesVM.TimeFrameSelectionIndex == (int)ShiftEntriesViewModel.TimeFrameSelection.Day
                                   || shiftEntriesVM.TimeFrameSelectionIndex == (int)ShiftEntriesViewModel.TimeFrameSelection.Year) {
                                   continue;
                              }

                              //Generate report

                              ReportViewModel reportvm = await shiftEntriesVM.GenerateReport();

                              //Save report to directory
                              await reportvm.SaveZipFile(filepath: $"{subdirectoryLocation}\\", filename: $"{reportvm.Report.Name}.ZIP");
                         }
                    }
               }
               finally {
                    MainWindowViewModel.MainWindowInstance.IsBusy = false;
               }
          }

          public void Initialize()
          {
               base.DisplayName = "Shift Entries";
               Observer = new QueryObserver<ShiftEntryViewModel>();
               Observer.MessageHandler = DataRepository.ShiftEntryMessageHandler;
          }

          public async void InitializeQueryAndView(string caller)
          {
               TraceEx.PrintLog($"InitializeQueryAndView: {caller} START");

               View = null;

               await Task.Run(() => {
                    lock (lockObject) {
                         IsBusy = true;
                         IsEnabled = false;

                         UpdateQuery(caller);
                         InitializeView();
                         ProcessSortSelection();

                         IsBusy = false;
                         IsEnabled = true;
                    }

                    TraceEx.PrintLog($"InitializeQueryAndView: {caller} END");
               });
          }

          public object Copy()
          {
               throw new NotImplementedException();
          }

          public void CopyFromOther(object obj)
          {
               TraceEx.PrintLog($"ShiftEntriesVM:: CopyFromOther START");
               this.IsUserSearch = false;
               ShiftEntriesViewModel other = obj as ShiftEntriesViewModel;
               this.ReportName = ConvertUtility.NullStringCopy(other.ReportName);
               this.SelectedSort = other.SelectedSort;

               this.ShiftEntrySearchList = other.ShiftEntrySearchList;
               OnPropertyChanged(nameof(ShiftEntrySearchList));

               this.TimeFrameSelectionIndex = other.TimeFrameSelectionIndex;
               this.IsUserSearch = true;
               TraceEx.PrintLog($"ShiftEntriesVM:: CopyFromOther END");
          }

          protected override void OnDispose()
          {
               Observer.Dispose();
          }

          private static ShiftEntriesViewModel OpenPreset(string filename)
          {
               XmlSerializer reader = new XmlSerializer(typeof(ShiftEntriesViewModel));
               ShiftEntriesViewModel vm;
               FileStream file;

               using (file = File.OpenRead(filename)) {
                    vm = reader.Deserialize(file) as ShiftEntriesViewModel;
               }

               if (vm.TimeFrameSelectionIndex == (int)TimeFrameSelection.Month) {
                    vm.SelectedDate = new DateTime(DateTime.Today.Year, DateTime.Today.AddMonths(-1).Month, 1);
               }

               vm.IsUserSearch = false;

               return vm;
          }

          private void ShowReport(ReportViewModel reportvm)
          {
               MainWindowViewModel.MainWindowInstance.ShowView(reportvm);
          }

          private void Clear()
          {
               ShiftEntrySearchList = new List<ShiftEntrySearch>();
               this.ReportName = String.Empty;

               InitializeQueryAndView("Clear");
          }

          private void Edit()
          {
               if (SelectedValue == null) {
                    return;
               }

               AddPersonViewModel vm = AddPersonViewModel.CreateAsync(SelectedValue.LinkedPerson, true);
               MainWindowViewModel.MainWindowInstance.ShowView(vm);
          }

          private async Task<ReportViewModel> GenerateReport()
          {
               this.IsBusy = true;

               //get rollcall from list
               var report = ReportPrinter.FindReportInList("Shift Entries", Report.Reports).Copy();

               var startDate = SelectedDate;
               var endDate = EndDate;

               //var companyText = string.IsNullOrEmpty(CompanySearch) ? "" : CompanySearch.Replace("'", "''");

               report.Query = $@"select person.lastname as 'Last', person.firstname as 'First', person.company as 'Company', shiftentry.intime as 'In', shiftentry.outtime as 'Out', Round(shiftentry.hoursworked, 4) as 'Hours'
                              from person, shiftentry
                              where person.personid = shiftentry.personid and (Date(intime) >= Date('{startDate.ToString("yyyy-MM-dd")}') and Date(intime) <= Date('{endDate.ToString("yyyy-MM-dd")}'))";

               //and ( (Company like '{companyText}%'and lastname like '{LastNameSearch}%'and firstname like '{FirstNameSearch}%') or
               //      (Company like '{companyText2}%'and lastname like '{LastNameSearch2}%'and firstname like '{FirstNameSearch2}%') )

               if (ShiftEntrySearchList.Any()) {
                    report.Query += " and (\n";

                    int i = 0;
                    foreach (var searchParam in ShiftEntrySearchList) {
                         report.Query += searchParam.GenerateSearchLine();
                         i++;

                         if (i < ShiftEntrySearchList.Count) {
                              report.Query += " or \n";
                         }
                    }
                    report.Query += ")\n";
               }

               report.Query += $@" and lastname not like 'Test'
                                  order by company, lastname, firstname";
               string dateString = string.Empty;
               string dateFormat = "MMM-dd-yyyy";

               if ((TimeFrameSelection)TimeFrameSelectionIndex == TimeFrameSelection.Day) {
                    dateString = $"({ startDate.ToString(dateFormat)})";
               } else {
                    dateString = $"({ startDate.ToString(dateFormat)} to { endDate.AddDays(0).ToString(dateFormat)})";
               }

               string reportName = string.Empty;

               if (string.IsNullOrEmpty(ReportName)) {
                    reportName = "Custom Hour Report";
               } else {
                    reportName = ReportName;
               }

               var timeFrameString = _timeFrameSelectionIndex == TimeFrameSelection.Custom ? " " : " - " + _timeFrameSelectionIndex.ToString();

               report.Name = $"{reportName}{timeFrameString}{dateString}";
               report.ShowTime = false;
               report.ShowDatePicker = false;

               var reportvm = ReportPrinter.CreateReportVM(startDate, report);

               reportvm.DisplayName = reportName;

               reportvm.ShowDateTime = false;
               await reportvm.Initialize();

               //aux report generation / init
               reportvm.AuxReport = GenerateAccessReport();
               await reportvm.AuxReport.Initialize();

               this.IsBusy = false;

               return reportvm;
          }

          private ReportViewModel GenerateAccessReport()
          {
               //get rollcall from list
               var report = ReportPrinter.FindReportInList("Access Log", Report.Reports).Copy();

               var startDate = SelectedDate;
               var endDate = EndDate;

               report.Query = $@"select person.lastname, person.firstname, person.company, datetime(accessentry.dttm) as dttm, reader, type, reason, readerkey, portalkey
                              from person, accessentry
                              where person.personid = accessentry.personid and (Date(dttm) >= Date('{startDate.ToString("yyyy-MM-dd")}') and Date(dttm) <= Date('{endDate.ToString("yyyy-MM-dd")}'))";

               if (ShiftEntrySearchList.Any()) {
                    report.Query += " and (\n";

                    int i = 0;
                    foreach (var searchParam in ShiftEntrySearchList) {
                         report.Query += searchParam.GenerateSearchLine();
                         i++;

                         if (i < ShiftEntrySearchList.Count) {
                              report.Query += " or \n";
                         }
                    }
                    report.Query += ")\n";
               }

               report.Query += $@" and lastname not like 'Test'
                                  order by company, lastname, firstname, dttm";
               string dateString = string.Empty;
               string dateFormat = "MMM-dd-yyyy";

               if ((TimeFrameSelection)TimeFrameSelectionIndex == TimeFrameSelection.Day) {
                    dateString = $"({ startDate.ToString(dateFormat)})";
               } else {
                    dateString = $"({ startDate.ToString(dateFormat)} to { endDate.AddDays(0).ToString(dateFormat)})";
               }

               string reportName = string.Empty;
               reportName = "Access Log Report";

               var timeFrameString = _timeFrameSelectionIndex == TimeFrameSelection.Custom ? " " : " - " + _timeFrameSelectionIndex.ToString();

               report.Name = $"{reportName}{timeFrameString}{dateString}";
               report.ShowTime = false;
               report.ShowDatePicker = false;

               return new ReportViewModel(report);
          }

          private void InitializeView()
          {
               DispatcherHelper.GetDispatcher().BeginInvoke(new Action(() => {
                    TraceEx.PrintLog("ShiftEntriesVM::InitializeView");
                    View = CollectionViewSource.GetDefaultView(Observer.Collection);

                    //change predicate to only react to relevent messages
                    Observer.Predicate = (x) => {
                         return x.InTime > SelectedDate.Date && x.InTime < SelectedDate.AddDays(1);
                    };
               }));
          }

          private void ProcessSortSelection()
          {
               DispatcherHelper.GetDispatcher().BeginInvoke(new Action(() => {
                    TraceEx.PrintLog("ShiftEntriesVM::ProcessSortSelection");
                    SortType sortType = (SortType)SelectedSort;
                    View.SortDescriptions.Clear();
                    View.GroupDescriptions.Clear();

                    switch (sortType) {
                         case SortType.GroupByCompany:
                              View.GroupDescriptions.Add(new PropertyGroupDescription("LinkedPerson.Company"));
                              View.SortDescriptions.Add(new SortDescription("LinkedPerson.Company", ListSortDirection.Ascending));
                              View.SortDescriptions.Add(new SortDescription("LinkedPerson.FullName", ListSortDirection.Ascending));
                              ShowCompany = false;
                              break;

                         case SortType.TimeSort:
                              ShowCompany = true;
                              View.SortDescriptions.Add(new SortDescription("InTime", ListSortDirection.Ascending));
                              break;
                    }
               }));
               //View.Refresh();
          }

          private void RowEdit()
          {
               if (SelectedValue == null) {
                    TraceEx.PrintLog("ShiftEntriesVM: selectedValue is null");
                    return;
               }
               //ChangeEntry(SelectedValue);
               SelectedValue.CalculateHours();
               using (var db = NetboxDatabase.GetWriteInstance()) {
                    db.EditEntry(SelectedValue.Entry);
               }
          }

          private void UpdateQuery(string caller)
          {
               TraceEx.PrintLog($"ShiftEntriesVM::UpdateQuery START {caller}");
               //obtain from database
               var dict = new Dictionary<long, ShiftEntryViewModel>();
               using (var db = NetboxDatabase.GetReadOnlyInstance()) {
                    if (ShiftEntrySearchList.Any() == false) {
                         var query = from se in db.GetContext().ShiftEntries
                                     join p in db.GetContext().People on se.PersonId equals p.PersonId
                                     where se.InTime > SelectedDate.Date && se.InTime < EndDate.Date.AddDays(1)
                                     && !p.LastName.Contains("Test")
                                     select se;

                         foreach (var v in query) {
                              dict[v.ShiftEntryId] = new ShiftEntryViewModel(v);
                         }
                    } else {
                         //add for each search set individually
                         TraceEx.PrintLog("ShiftEntriesVM::UpdateQuery add search set");
                         foreach (var searchParam in ShiftEntrySearchList) {
                              TraceEx.PrintLog($"ShiftEntriesVM::UpdateQuery searchParam={searchParam}");
                              var query = from se in db.GetContext().ShiftEntries
                                          join p in db.GetContext().People on se.PersonId equals p.PersonId
                                          where se.InTime > SelectedDate.Date && se.InTime < EndDate.Date.AddDays(1)
                                          && p.LastName.Contains(searchParam.LastName) && p.FirstName.Contains(searchParam.FirstName) && p.Company.Contains(searchParam.Company)
                                          && !p.LastName.Contains("Test")
                                          select se;

                              foreach (var v in query) {
                                   dict[v.ShiftEntryId] = new ShiftEntryViewModel(v);
                              }
                         }
                    }

                    var list = dict.Values.ToList();

                    DispatcherHelper.GetDispatcher().BeginInvoke(new Action(() => {
                         TraceEx.PrintLog($"ShiftEntriesVM::UpdateQuery SetObserver {caller}");

                         //set observer/view
                         Observer.AddQuery(list);
                    }));
                    TraceEx.PrintLog($"ShiftEntriesVM::UpdateQuery END {caller}");
               }//using db
          }

          private void CalculateEndDate()
          {
               bool subDate = true;
               switch (_timeFrameSelectionIndex) {
                    case TimeFrameSelection.Custom:
                         subDate = false;
                         break;

                    case TimeFrameSelection.Day:
                         _endDate = SelectedDate.AddDays(1);
                         break;

                    case TimeFrameSelection.Week:
                         _endDate = SelectedDate.AddDays(7);
                         break;

                    case TimeFrameSelection.Month:
                         _endDate = SelectedDate.AddMonths(1);
                         break;

                    case TimeFrameSelection.Year:
                         _endDate = SelectedDate.AddYears(1);
                         break;
               }
               if (subDate) {
                    _endDate = EndDate.AddDays(-1);
               }
               OnPropertyChanged(nameof(EndDate));
          }

          #endregion Methods
     }
}