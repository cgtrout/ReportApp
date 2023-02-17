using ReportApp.Data;
using ReportApp.Utility;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace ReportApp.ViewModel
{
     internal class OrientationsLiveViewModel : WorkspaceViewModel
     {
          #region Fields

          private ICommand _clearCommand;
          private string _companySearch = string.Empty;
          private ICommand _editCommand;
          private string _firstNameSearch = string.Empty;
          private bool _hideExpired = false;
          private bool _searchExpanded = false;
          private string _lastNameSearch = string.Empty;
          private string _numberSearch = string.Empty;
          private PersonViewModel _selectedItem;
          private ICommand _shiftEntryCommand;

          private ObservableCollection<PersonViewModel> collection;
          private Dictionary<string, PersonViewModel> dict;

          private ICollectionView view;
          private RelayCommand _searchCommand;

          #endregion Fields

          #region Constructors

          public OrientationsLiveViewModel()
          {
               base.DisplayName = "Orientation Search";

               var dispatcher = DispatcherHelper.GetDispatcher();
               dispatcher.Invoke(new Action(() => {
                    DataRepository.PersonChanged += DataRepository_PersonChanged; ;
               }));

               ChangeQuery();
               RefreshView();

               TraceEx.PrintLog("Opening Orientations Live");
               this.OnlyOneCanRun = true;
          }

          #endregion Constructors

          #region Properties

          public ICommand SearchCommand
          {
               get
               {
                    if (_searchCommand == null) {
                         _searchCommand = new RelayCommand(x => Search());
                    }
                    return _searchCommand;
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

          public List<string> CompanyList => DataRepository.CompanyList;

          public string CompanySearch
          {
               get { return _companySearch; }
               set
               {
                    _companySearch = value;
                    //View.Refresh();
                    OnPropertyChanged(nameof(CompanySearch));
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
                    OnPropertyChanged(nameof(FirstNameSearch));
               }
          }

          public bool HideExpired
          {
               get { return _hideExpired; }
               set
               {
                    _hideExpired = value;
                    View.Refresh();
                    OnPropertyChanged(nameof(HideExpired));
               }
          }

          public string LastNameSearch
          {
               get { return _lastNameSearch; }
               set
               {
                    _lastNameSearch = value;
                    OnPropertyChanged(nameof(LastNameSearch));
               }
          }

          public string NumberSearch
          {
               get { return _numberSearch; }
               set
               {
                    _numberSearch = value;
                    OnPropertyChanged(nameof(NumberSearch));
               }
          }

          public ConcurrentDictionary<string, PersonViewModel> PersonDict
          {
               get { return DataRepository.PersonDict; }
          }

          public bool SearchExpanded
          {
               get { return _searchExpanded; }
               set
               {
                    _searchExpanded = value;
                    OnPropertyChanged(nameof(SearchExpanded));
               }
          }

          public PersonViewModel SelectedItem
          {
               get { return _selectedItem; }
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

          public ICollectionView View
          {
               get
               {
                    return view;
               }
          }

          #endregion Properties

          #region Methods

          protected override void OnDispose()
          {
               var dispatcher = DispatcherHelper.GetDispatcher();
               dispatcher.Invoke(new Action(() => {
                    DataRepository.PersonChanged += DataRepository_PersonChanged;
               }));
          }

          private void RefreshView()
          {
               view = CollectionViewSource.GetDefaultView(collection);
               view.Filter = x => Filter(x);

               view.SortDescriptions.Add(new SortDescription("OrientationNumber", ListSortDirection.Descending));

               ICollectionViewLiveShaping viewshaping = (ICollectionViewLiveShaping)view;
               viewshaping.LiveFilteringProperties.Add("OrientationNumber");
               viewshaping.IsLiveFiltering = true;
          }

          private void ChangeQuery()
          {
               Benchmarker.Start("Orien Search");

               bool orientationQuery = string.IsNullOrEmpty(LastNameSearch) && string.IsNullOrEmpty(FirstNameSearch) && string.IsNullOrEmpty(CompanySearch);

               NetboxDatabase db = NetboxDatabase.GetReadOnlyInstance();
               var query = orientationQuery ? GetOrientationQuery(db) : GetNameQuery(db);
               if (query.Any()) {
                    var newList = new List<PersonViewModel>();
                    dict = new Dictionary<string, PersonViewModel>(StringComparer.InvariantCulture);
                    //only load cap size
                    int cap = 500;
                    int i = 0;
                    foreach (var e in query) {
                         if (DataRepository.PersonDict.ContainsKey(e.PersonId)) {
                              var vm = DataRepository.PersonDict[e.PersonId];
                              newList.Add(vm);
                              dict.Add(e.PersonId, vm);
                         } else {
                              Trace.TraceError("Could not find in repos dict!");
                              TraceEx.PrintLog("Error: OrientationLiveVM::ChangeQuery()");
                         }
                         i++;
                         if (i == cap) {
                              MainWindowViewModel.MainWindowInstance.PrintStatusText($"Only first {cap} entries loaded.", Brushes.Black);
                              break;
                         }
                    }
                    collection = new ObservableCollection<PersonViewModel>(newList);
               }
               Benchmarker.Stop("Orien Search");
          }

          private IOrderedQueryable<Model.Person> GetOrientationQuery(NetboxDatabase db)
          {
               return from p in db.GetContext().People
                      where p.OrientationNumber != 0
                      orderby p.OrientationNumber descending
                      select p;
          }

          private IOrderedQueryable<Model.Person> GetNameQuery(NetboxDatabase db)
          {
               return from p in db.GetContext().People
                      where
                         (string.IsNullOrEmpty(LastNameSearch) || p.LastName.ToLower().Contains(LastNameSearch.ToLower()))
                      && (string.IsNullOrEmpty(FirstNameSearch) || p.FirstName.ToLower().Contains(FirstNameSearch.ToLower()))
                      && (string.IsNullOrEmpty(NumberSearch) || p.OrientationNumber.ToString().StartsWith(NumberSearch))
                      && (string.IsNullOrEmpty(CompanySearch) || p.Company.ToLower().Contains(CompanySearch.ToLower()))
                      && p.OrientationNumber != 0
                      orderby p.Company, p.LastName, p.FirstName
                      select p;
          }

          private void Search()
          {
               ChangeQuery();
               RefreshView();

               TraceEx.PrintLog($"Orienation Search: {LastNameSearch}, {FirstNameSearch} for {CompanySearch}");

               OnPropertyChanged(nameof(View));
          }

          private void DataRepository_PersonChanged(DictionaryChangedEventArgs e)
          {
               var person = e.ChangedValue as PersonViewModel;
               var original = e.OriginalValue as PersonViewModel;
               if (e.Edit == false && e.Remove == false) {
                    //add
                    if (dict.ContainsKey(person.PersonId) == false) {
                         AddPerson(person);
                    }
               } else if (e.Remove == true) {
                    //remove
               } else {
                    //edit
                    if (Filter(person)) {
                         if (dict.ContainsKey(person.PersonId) == false) {
                              AddPerson(person);
                         }
                    } else {
                         RemovePerson(person, original);
                    }
               }
          }

          private void AddPerson(PersonViewModel person)
          {
               collection.Add(person);
               dict[person.PersonId] = person;
          }

          private void RemovePerson(PersonViewModel person, PersonViewModel original)
          {
               var result = collection.Remove(original);
               dict.Remove(person.PersonId);
          }

          private void Clear()
          {
               LastNameSearch = string.Empty;
               FirstNameSearch = string.Empty;
               NumberSearch = string.Empty;
               CompanySearch = string.Empty;
               Search();
               TraceEx.PrintLog("Orientation Search: Clear");
          }

          private void Edit()
          {
               if (SelectedItem == null) {
                    return;
               }
               if (SelectedItem.IsNetbox) {
                    AddPersonViewModel vm = AddPersonViewModel.CreateAsync(SelectedItem, true);
                    MainWindowViewModel.MainWindowInstance.ShowView(vm);
               } else {
                    MessageBox.Show("Can not edit imported orientation data.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
               }
          }

          private bool Filter(object x)
          {
               var kvp = (PersonViewModel)x;

               if (kvp.OrientationNumber == 0) return false;

               bool lastNameSearch = string.IsNullOrEmpty(LastNameSearch) || kvp.LastName.ToLower().Contains(LastNameSearch.ToLower());
               bool firstNameSearch = string.IsNullOrEmpty(FirstNameSearch) || kvp.FirstName.ToLower().Contains(FirstNameSearch.ToLower());
               bool numberSearch = string.IsNullOrEmpty(NumberSearch) || kvp.OrientationNumber.ToString().StartsWith(NumberSearch);
               bool testedBySearch = string.IsNullOrEmpty(CompanySearch) || kvp.Company.ToLower().Contains(CompanySearch.ToLower());
               bool isExpiredSearch = HideExpired == false ? true : !kvp.IsExpired;

               return (lastNameSearch)
                     && (firstNameSearch)
                     && numberSearch
                     && testedBySearch
                     && isExpiredSearch;
          }

          private void OpenShiftEntry()
          {
               var mainWindow = MainWindowViewModel.MainWindowInstance;
               ShiftEntriesViewModel vm = new ShiftEntriesViewModel(ShiftEntriesViewModel.TimeFrameSelection.Month);
               if (SelectedItem == null) return;
               var person = SelectedItem as PersonViewModel;
               vm.IsUserSearch = false;
               vm.ShiftEntrySearchList.Add(new ShiftEntrySearch {
                    Company = person?.Company,
                    LastName = person?.LastName,
                    FirstName = person?.FirstName
               });
               vm.ReportName = $"{person?.LastName}, { person?.FirstName}";
               vm.IsUserSearch = true;
               vm.InitializeQueryAndView("OrientationsLiveVM");
               mainWindow.ShowView(vm);
          }

          #endregion Methods
     }
}