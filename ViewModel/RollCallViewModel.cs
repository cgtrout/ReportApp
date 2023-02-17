using ReportApp.Data;
using ReportApp.Model;
using ReportApp.Utility;
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace ReportApp.ViewModel
{
     //represents the entire roll call list
     public class RollCallViewModel : WorkspaceViewModel
     {
          #region Fields

          private const float sliderHigherDefault = 23;
          private ICommand _clearCommand;
          private string _companySearch;
          private int _currentSortSelection = 0;
          private ICommand _editCommand;
          private string _firstNameSearch;
          private bool _isCompanyVisible = true;
          private bool _isReaderVisible = false;
          private string _lastNameSearch;
          private ICommand _printRollCallCommand;
          private SingleRollCallViewModel _selectedItem;
          private ICommand _shiftEntryCommand;
          private bool _showAccess = true;
          private float _sliderHigherValue = sliderHigherDefault;
          private float _sliderLowerValue = 0;
          private DictionaryObserver<string, SingleRollCallViewModel> dictionaryObserver;
          private DispatcherTimer timer;

          private ICollectionView _view;
          private ICommand _generateCustomRollCallCommand;
          private float _lastNameWidth = 71;
          private float _firstNameWidth = 44;
          private float _fullNameWidth = 0;

          private FilterTypeEnum filterType = FilterTypeEnum.AndFilter;

          private bool ignoreProcessSort = false;

          private bool wantFilter = false;

          private BitmapSource _hourBitmap;

          private ICommand _addVehicleCommand;

          private ICommand _addAwayListCommand;

          #endregion Fields

          #region Constructors

          public RollCallViewModel(bool showAccess = true)
          {
               Initialize(showAccess);
          }

          public RollCallViewModel()
          {
               Initialize(false);
          }

          #endregion Constructors

          #region Enums

          public enum SortSelection
          {
               Company,
               Reader,
               TimeIn,
               LateWorkers
          };

          private enum FilterTypeEnum
          { AndFilter, OrFilter };

          #endregion Enums

          #region Properties

          //FullNameWidth
          public float FullNameWidth
          {
               get { return _fullNameWidth; }
               set
               {
                    _fullNameWidth = value;
                    OnPropertyChanged(nameof(FullNameWidth));
               }
          }

          //LastNameWidth
          public float LastNameWidth
          {
               get { return _lastNameWidth; }
               set
               {
                    _lastNameWidth = value;
                    OnPropertyChanged(nameof(LastNameWidth));
               }
          }

          //LastNameWidth
          public float FirstNameWidth
          {
               get { return _firstNameWidth; }
               set
               {
                    _firstNameWidth = value;
                    OnPropertyChanged(nameof(FirstNameWidth));
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

          public ICommand AddVehicleCommand
          {
               get
               {
                    if (_addVehicleCommand == null) {
                         _addVehicleCommand = new RelayCommand((x) => {
                              DispatcherHelper.GetDispatcher().BeginInvoke(new Action(() => {
                                   VehiclePassRequestEventArgs e = new VehiclePassRequestEventArgs();
                                   if (SelectedItem != null) {
                                        e.PersonId = SelectedItem.PersonId;
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
                                        e.PersonId = SelectedItem.PersonId;
                                        AwayListMessageRequester.Instance.AwayListRequest(e);
                                   }
                              }), System.Windows.Threading.DispatcherPriority.Render);
                         });
                    }
                    return _addAwayListCommand;
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

          public string CompanySearch
          {
               get { return _companySearch; }
               set
               {
                    _companySearch = value;
                    wantFilter = true;
                    ProcessSortSelection();
                    OnPropertyChanged(nameof(CompanySearch));
               }
          }

          public int CurrentSortSelection
          {
               get
               {
                    return _currentSortSelection;
               }
               set
               {
                    var sortSelection = (SortSelection)value;
                    _currentSortSelection = value;

                    if (sortSelection == SortSelection.LateWorkers) {
                         wantFilter = true;
                    } else {
                         wantFilter = false;
                    }

                    ProcessSortSelection();
                    OnPropertyChanged(nameof(CurrentSortSelection));
               }
          }

          public ICommand EditCommand
          {
               get
               {
                    if (_editCommand == null) {
                         _editCommand = new RelayCommand((x) => Edit());
                    }
                    return _editCommand;
               }
          }

          public string FirstNameSearch
          {
               get { return _firstNameSearch; }
               set
               {
                    _firstNameSearch = value;
                    wantFilter = true;
                    ProcessSortSelection();
                    OnPropertyChanged(nameof(FirstNameSearch));
               }
          }

          public bool IsCompanyVisible
          {
               get
               {
                    return _isCompanyVisible;
               }
               set
               {
                    _isCompanyVisible = value;
                    OnPropertyChanged(nameof(IsCompanyVisible));
               }
          }

          public bool IsReaderVisible
          {
               get
               {
                    return _isReaderVisible;
               }
               set
               {
                    _isReaderVisible = value;
                    OnPropertyChanged(nameof(IsReaderVisible));
               }
          }

          //search parameters
          public string LastNameSearch
          {
               get { return _lastNameSearch; }
               set
               {
                    _lastNameSearch = value;
                    wantFilter = true;
                    ProcessSortSelection();
                    OnPropertyChanged(nameof(LastNameSearch));
               }
          }

          public ICommand PrintRollCallCommand
          {
               get
               {
                    if (_printRollCallCommand == null) {
                         _printRollCallCommand = new RelayCommand(async (x) => await PrintRollCall());
                    }
                    return _printRollCallCommand;
               }
          }

          public ICommand GenerateCustomRollCallCommand
          {
               get
               {
                    if (_generateCustomRollCallCommand == null) {
                         _generateCustomRollCallCommand = new RelayCommand(async (x) => await GenerateCustomRollCall());
                    }
                    return _generateCustomRollCallCommand;
               }
          }

          //GenerateCustomRollCall

          public SingleRollCallViewModel SelectedItem
          {
               get
               {
                    return _selectedItem;
               }
               set
               {
                    _selectedItem = value;
                    OnPropertyChanged(nameof(SelectedItem));
               }
          }

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

          public bool ShowAccess
          {
               get
               {
                    return _showAccess;
               }
               set
               {
                    _showAccess = value;
                    OnPropertyChanged(nameof(ShowAccess));
               }
          }

          public float SliderHigherValue
          {
               get
               {
                    return _sliderHigherValue;
               }
               set
               {
                    _sliderHigherValue = value;
                    OnPropertyChanged(nameof(SliderHigherValue));
                    OnPropertyChanged(nameof(SliderToolTip));
               }
          }

          public float SliderLowerValue
          {
               get
               {
                    return _sliderLowerValue;
               }
               set
               {
                    _sliderLowerValue = value;
                    OnPropertyChanged(nameof(SliderLowerValue));
                    OnPropertyChanged(nameof(SliderToolTip));
               }
          }

          public string SliderToolTip
          {
               get
               {
                    return $"Use this slider to filter by hours in.\nLow is {SliderLowerValue} \nHigh is {SliderHigherValue}";
               }
          }

          public Color TimeColor => new Color() { B = 255, R = 0, G = 0 };

          public BitmapSource HourBitmap
          {
               get
               {
                    return _hourBitmap;
               }
               set
               {
                    _hourBitmap = value;
                    OnPropertyChanged(nameof(HourBitmap));
               }
          }

          #endregion Properties

          #region Methods

          //TimeColor
          protected override void OnDispose()
          {
               TraceEx.PrintLog("RollCallViewModel: OnDispose()");

               var dispatcher = DispatcherHelper.GetDispatcher();
               dispatcher.BeginInvoke(new Action(() => {
                    DataRepository.RollcallChanged -= dictionaryObserver.DataRepository_ValueChanged;
                    DataRepository.PersonChanged -= DataRepository_PersonChanged;
               }));
               timer.Tick -= Timer_Tick;
          }

          private void Clear()
          {
               LastNameSearch = string.Empty;
               FirstNameSearch = string.Empty;
               CompanySearch = string.Empty;
               SliderHigherValue = sliderHigherDefault;
               SliderLowerValue = 0;
               filterType = FilterTypeEnum.AndFilter;
               wantFilter = false;

               ProcessSortSelection();
          }

          private void Edit()
          {
               if (SelectedItem == null) {
                    MessageBox.Show("Can't open - no person selected", "Warning");
                    return;
               }
               PersonViewModel person = (SelectedItem as SingleRollCallViewModel).LinkedPerson;
               MainWindowViewModel.MainWindowInstance.ShowView(AddPersonViewModel.CreateAsync(person, true));
          }

          private bool Filter(object x)
          {
               var vm = (SingleRollCallViewModel)x;

               bool lastNameSearch = string.IsNullOrEmpty(LastNameSearch) || vm.LinkedPerson.LastName.ToLower().Contains(LastNameSearch.ToLower());
               bool firstNameSearch = string.IsNullOrEmpty(FirstNameSearch) || vm.LinkedPerson.FirstName.ToLower().Contains(FirstNameSearch.ToLower());
               bool companySearch = string.IsNullOrEmpty(CompanySearch) || vm.LinkedPerson.Company.ToLower().Contains(CompanySearch.ToLower());
               bool hourSearch = vm.TimeInFloat > SliderLowerValue && vm.TimeInFloat < SliderHigherValue;

               if (filterType == FilterTypeEnum.AndFilter) {
                    return lastNameSearch
                          && firstNameSearch
                          && companySearch
                          && hourSearch;
               } else if (filterType == FilterTypeEnum.OrFilter) {
                    return lastNameSearch
                          || firstNameSearch
                          || companySearch;
               }
               return false;
          }

          private void Initialize(bool showAccess)
          {
               base.DisplayName = "Roll Call Live View";

               dictionaryObserver = new DictionaryObserver<string, SingleRollCallViewModel>(DataRepository.RollCallDict);

               DispatcherHelper.GetDispatcher().Invoke(new Action(() => {
                    DataRepository.RollcallChanged += dictionaryObserver.DataRepository_ValueChanged;
                    DataRepository.RollcallChanged += DataRepository_RollcallChanged;
                    DataRepository.PersonChanged += DataRepository_PersonChanged;
                    RollcallSelectMessageRequester.Instance.RollcallSelectMessageRequestEvent += RollcallSelectMessageRequestEvent;
               }));

               View = CollectionViewSource.GetDefaultView(dictionaryObserver.Collection);

               CurrentSortSelection = 0;

               timer = new DispatcherTimer();
               setupTimer();

               TraceEx.PrintLog("Opening Rollcall Live");
               ShowAccess = showAccess;
               this.OnlyOneCanRun = true;

               ////TEMP add fake data
               //for (double i = 0; i < 14.0; i += 0.1) {
               //     var rollcall = new RollCall() {
               //          DtTm = DateTime.Now.AddHours(-i),
               //          PersonId = "_2",
               //          Reader = "ADMIN IN"
               //     };
               //     dictionaryObserver.Collection.Add(new SingleRollCallViewModel(rollcall));
               //}

               CreateHourBitmap();
          }

          private void DataRepository_RollcallChanged(DictionaryChangedEventArgs e)
          {
               //update headcount every time rollcall is changed
               UpdateDisplayName();
          }

          private void RollcallSelectMessageRequestEvent(RollCallSelectMessageRequestEventArgs e)
          {
               try {
                    ignoreProcessSort = true;
                    if (e.MessageType == RollCallSelectMessageRequestEventArgs.MessageTypeEnum.Change) {
                         filterType = FilterTypeEnum.OrFilter;
                         string lastNameFilter = e.query;
                         string firstNameFilter = e.query;
                         string companyFilter = e.query;

                         if (e.query.Contains(",")) {
                              filterType = FilterTypeEnum.AndFilter;
                              var split = e.query.Split(',');
                              lastNameFilter = split[0].Trim();
                              firstNameFilter = split[1].Trim();

                              if (split.Length > 2) {
                                   companyFilter = split[2].Trim();
                              } else {
                                   companyFilter = string.Empty;
                              }
                         }

                         LastNameSearch = lastNameFilter;
                         FirstNameSearch = firstNameFilter;
                         CompanySearch = companyFilter;
                    } else if (e.MessageType == RollCallSelectMessageRequestEventArgs.MessageTypeEnum.Clear) {
                         filterType = FilterTypeEnum.AndFilter;
                         LastNameSearch = string.Empty;
                         FirstNameSearch = string.Empty;
                         CompanySearch = string.Empty;
                         wantFilter = false;
                    }
               }
               finally {
                    ignoreProcessSort = false;
                    _currentSortSelection = (int)SortSelection.Company;
                    OnPropertyChanged(nameof(CurrentSortSelection));
                    ProcessSortSelection();
               }
          }

          private void DataRepository_PersonChanged(DictionaryChangedEventArgs e)
          {
               if (e.Edit == true) {
                    foreach (var v in dictionaryObserver.Collection) {
                         PersonViewModel p = e.ChangedValue as PersonViewModel;
                         if (v.PersonId == p.PersonId) {
                              v.OnPropertyChanged("LinkedPerson");
                         }
                    }
               }
          }

          private void OpenShiftEntry()
          {
               if (SelectedItem == null) return;
               var mainWindow = MainWindowViewModel.MainWindowInstance;
               ShiftEntriesViewModel vm = new ShiftEntriesViewModel(ShiftEntriesViewModel.TimeFrameSelection.Month);
               var rollcall = SelectedItem as SingleRollCallViewModel;
               vm.IsUserSearch = false;
               vm.ShiftEntrySearchList.Add(new ShiftEntrySearch {
                    Company = rollcall.LinkedPerson?.Company,
                    LastName = rollcall.LinkedPerson?.LastName,
                    FirstName = rollcall.LinkedPerson?.FirstName
               });
               vm.ReportName = $"{ rollcall.LinkedPerson?.LastName}, { rollcall.LinkedPerson?.FirstName}";
               vm.IsUserSearch = true;
               vm.InitializeQueryAndView("RollcallVM");
               mainWindow.ShowView(vm);
          }

          private async Task PrintRollCall()
          {
               await ReportPrinter.PrintReport("Roll Call - Company", Report.Reports);
               TraceEx.PrintLog("PrintRollCall()");
          }

          private async Task GenerateCustomRollCall()
          {
               //get rollcall from list
               var report = ReportPrinter.FindReportInList("Roll Call - Company", Report.Reports).Copy();

               //customize
               report.Query = $@"select company as 'Company', lastname as 'Last', firstname as 'First', dttm as 'In Time', printf(' % .1f',((strftime('%s', 'now')-strftime('%s', dttm))/3600.0)-7) as 'Time In', reader as 'Reader', employeecategory as cat
                              from rollcall
                              inner join person
                              on rollcall.personid = person.personid
                              where deleted = 0
                                and (Company like '{CompanySearch}%'
                                and lastname like '{LastNameSearch}%'
                                and firstname like '{FirstNameSearch}%')
                                and (((strftime('%s', 'now')-strftime('%s', dttm))/3600.0)-7 > {SliderLowerValue} and ((strftime('%s', 'now')-strftime('%s', dttm))/3600.0)-7 < {SliderHigherValue})
                              order by company, lastname, firstname";
               var reportvm = ReportPrinter.CreateReportVM(DateTime.Now, report);
               report.Name = "Custom Roll Call";
               MainWindowViewModel.MainWindowInstance.ShowView(reportvm);
               TraceEx.PrintLog("Rollcall: custom generated");
               await reportvm.Initialize();
          }

          private void ProcessSortSelection()
          {
               if (ignoreProcessSort) {
                    return;
               }

               IsReaderVisible = false;
               IsCompanyVisible = false;

               View.GroupDescriptions.Clear();
               View.SortDescriptions.Clear();

               ICollectionViewLiveShaping viewshaping = (ICollectionViewLiveShaping)View;
               viewshaping.IsLiveFiltering = true;
               viewshaping.LiveFilteringProperties.Clear();
               viewshaping.LiveFilteringProperties.Add("LinkedPerson.Company");
               viewshaping.LiveFilteringProperties.Add("LinkedPerson.FirstName");
               viewshaping.LiveFilteringProperties.Add("LinkedPerson.LastName");
               viewshaping.IsLiveSorting = true;

               if ((SortSelection)CurrentSortSelection == SortSelection.Company) {
                    IsReaderVisible = true;
                    View.GroupDescriptions.Add(new PropertyGroupDescription("Company"));
                    View.SortDescriptions.Add(new SortDescription("LinkedPerson.Company", ListSortDirection.Ascending));
                    View.SortDescriptions.Add(new SortDescription("LinkedPerson.LastName", ListSortDirection.Ascending));
                    View.SortDescriptions.Add(new SortDescription("LinkedPerson.FirstName", ListSortDirection.Ascending));
                    LastNameWidth = 85;
                    FirstNameWidth = 44;
                    FullNameWidth = 0;
                    SliderLowerValue = 0;
                    TraceEx.PrintLog("Rollcall Live : Company Sort");
               } else if ((SortSelection)CurrentSortSelection == SortSelection.TimeIn) {
                    IsReaderVisible = false;
                    IsCompanyVisible = true;
                    View.SortDescriptions.Add(new SortDescription("TimeInFloat", ListSortDirection.Descending));
                    LastNameWidth = 0;
                    FirstNameWidth = 0;
                    FullNameWidth = 95;
                    SliderLowerValue = 0;
                    TraceEx.PrintLog("Rollcall Live : Time In Sort");
               } else if ((SortSelection)CurrentSortSelection == SortSelection.LateWorkers) {
                    IsReaderVisible = true;
                    View.GroupDescriptions.Add(new PropertyGroupDescription("Company"));
                    View.SortDescriptions.Add(new SortDescription("LinkedPerson.Company", ListSortDirection.Ascending));
                    View.SortDescriptions.Add(new SortDescription("LinkedPerson.LastName", ListSortDirection.Ascending));
                    View.SortDescriptions.Add(new SortDescription("LinkedPerson.FirstName", ListSortDirection.Ascending));
                    LastNameWidth = 85;
                    FirstNameWidth = 44;
                    FullNameWidth = 0;
                    SliderLowerValue = 12;
                    TraceEx.PrintLog("Rollcall Live : Late Workers");
               } else {
                    IsCompanyVisible = true;
                    View.GroupDescriptions.Add(new PropertyGroupDescription("Reader"));
                    View.SortDescriptions.Add(new SortDescription("Reader", ListSortDirection.Ascending));
                    View.SortDescriptions.Add(new SortDescription("LinkedPerson.Company", ListSortDirection.Ascending));
                    View.SortDescriptions.Add(new SortDescription("LinkedPerson.LastName", ListSortDirection.Ascending));
                    View.SortDescriptions.Add(new SortDescription("LinkedPerson.FirstName", ListSortDirection.Ascending));
                    LastNameWidth = 0;
                    FirstNameWidth = 0;
                    FullNameWidth = 95;
                    SliderLowerValue = 0;
                    TraceEx.PrintLog("Rollcall Live : Reader Sort");
               }

               if (wantFilter) {
                    View.Filter = x => Filter(x);
               } else {
                    View.Filter = null;
               }
               UpdateDisplayName();
               View.Refresh();
          }

          private void UpdateDisplayName()
          {
               if (wantFilter) {
                    this.DisplayName = "Rollcall *Filtered*";
               } else {
                    this.DisplayName = $"Rollcall Count: {DataRepository.RollCallDict.Count}";
               }

               OnPropertyChanged("DisplayName");
          }

          private void setupTimer()
          {
               timer.Interval = new TimeSpan(0, 1, 0);
               timer.Tick += Timer_Tick;
               timer.Start();
          }

          private void Timer_Tick(object sender, EventArgs e)
          {
               var dispatcher = DispatcherHelper.GetDispatcher();
               dispatcher.BeginInvoke(new Action(() => {
                    foreach (var v in dictionaryObserver.Collection) {
                         v.UpdateTime();
                    }
               }));
          }

          private void CreateHourBitmap()
          {
               SingleRollCallViewModel vm = new SingleRollCallViewModel(new RollCall());
               vm.DtTm = DateTime.Now;

               PixelFormat pf = PixelFormats.Bgra32;
               int width = 265;
               int height = 25;
               int rawStride = (width * pf.BitsPerPixel + 7) / 8;

               byte[] pixelData = new byte[rawStride * height];

               for (int i = 0; i < rawStride * height; i++) {
                    pixelData[i] = 255;
               }

               //fill data
               for (int i = 0; i < 265; i++) {
                    var color = new Color();

                    //get hour equiv based on pixel pos
                    float hour = (13.0f / width) * (float)i;

                    vm.DtTm = DateTime.Now.AddHours(-hour);
                    byte r, b, g, a;
                    vm.CalculateColor(out r, out b, out g, out a);
                    color.R = r;
                    color.G = g;
                    color.B = b;
                    color.A = a;
                    for (byte y = 1; y < 20; y++) {
                         setpixel(ref pixelData, i, y, rawStride, color);
                    }
               }

               try {
                    HourBitmap = BitmapSource.Create(width, height, 96, 96, pf, null, pixelData, rawStride);
               }
               catch (Exception e) {
                    var msg = $"hourbitmap creation problem: {e.GetType()}: {e.Message}";
                    TraceEx.PrintLog(msg);
               }
          }

          private void setpixel(ref byte[] bits, int x, int y, int stride, Color c)
          {
               bits[x * 4 + y * stride] = c.B;
               bits[x * 4 + y * stride + 1] = c.G;
               bits[x * 4 + y * stride + 2] = c.R;
               bits[x * 4 + y * stride + 3] = c.A;
          }

          #endregion Methods
     }
}

public delegate void RollcallSelectMessageEventHandler(RollCallSelectMessageRequestEventArgs e);

public class RollCallSelectMessageRequestEventArgs
{
     #region Fields

     public MessageTypeEnum MessageType;

     public string query;

     #endregion Fields

     #region Enums

     public enum MessageTypeEnum { Change, Clear };

     #endregion Enums
}

public class RollcallSelectMessageRequester
{
     #region Fields

     public static RollcallSelectMessageRequester _instance;

     #endregion Fields

     #region Events

     public event RollcallSelectMessageEventHandler RollcallSelectMessageRequestEvent;

     #endregion Events

     #region Properties

     public static RollcallSelectMessageRequester Instance
     {
          get
          {
               if (_instance == null) {
                    _instance = new RollcallSelectMessageRequester();
               }
               return _instance;
          }
     }

     #endregion Properties

     #region Methods

     public void RollcallSelectMessageRequest(RollCallSelectMessageRequestEventArgs e)
     {
          if (RollcallSelectMessageRequestEvent != null) {
               RollcallSelectMessageRequestEvent(e);
          }
     }

     #endregion Methods
}