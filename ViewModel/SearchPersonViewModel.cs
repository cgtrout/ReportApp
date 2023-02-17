using ProxCard2;
using ReportApp.Data;
using ReportApp.Model;
using ReportApp.Utility;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Media;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace ReportApp.ViewModel
{
     /// <summary>
     /// Description of SearchPersonViewModel.
     /// </summary>
     public class SearchPersonViewModel : WorkspaceViewModel
     {
          #region Fields

          private MainWindowViewModel _mainWindow;
          private ICommand _searchCommand;
          private bool _shouldAutoOpen = true;
          private bool _shouldAutoClose = false;
          private bool _inReadMode;
          private ReaderList proxCard;
          private ICommand _readButtonCommand;

          #endregion Fields

          #region Constructors

          public SearchPersonViewModel()
          {
               base.DisplayName = "Netbox Search";
               CurrentPerson = new PersonViewModel(new Person());
               _mainWindow = MainWindowViewModel.MainWindowInstance;
               ShowDeleted = true;
               CompanyList = DataRepository.CompanyList;
               TraceEx.PrintLog("Opening SearchPerson");

               proxCard = new ProxCard2.ReaderList();
               proxCard.CardPresented += x_CardPresented;
               proxCard.Refresh();
          }

          #endregion Constructors

          #region Properties

          [System.Xml.Serialization.XmlIgnore]
          public List<string> LastNameList
          {
               get
               {
                    if (string.IsNullOrEmpty(CurrentPerson.Company)) return null;
                    using (var db = NetboxDatabase.GetReadOnlyInstance()) {
                         return (from p in db.GetContext().People
                                 where p.Company == this.CurrentPerson.Company
                                 select p.LastName)
                                     .Distinct()
                                     .OrderBy(p => p)
                                     .ToList();
                    }
               }
          }

          [System.Xml.Serialization.XmlIgnore]
          public List<string> FirstNameList
          {
               get
               {
                    if (string.IsNullOrEmpty(CurrentPerson.Company)) return null;
                    using (var db = NetboxDatabase.GetReadOnlyInstance()) { 
                         var query = from p in db.GetContext().People
                                     where p.Company == this.CurrentPerson.Company && p.LastName == this.CurrentPerson.LastName
                                     orderby p.FirstName
                                     select p.FirstName;
                         var list = query.ToList();
                         if (list?.Count == 1) {
                              CurrentPerson.FirstName = list[0];
                         }
                         return list;
                    }
               }
          }

          public List<string> CompanyList { get; private set; }

          public PersonViewModel CurrentPerson { get; set; }

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

          public bool ShowDeleted { get; set; }

          //if set to true will auto open the person form for a person if there is only one search result
          public bool ShouldAutoOpen
          {
               get { return _shouldAutoOpen; }
               set
               {
                    _shouldAutoOpen = value;
                    OnPropertyChanged(nameof(ShouldAutoOpen));
               }
          }

          //if set to true will auto open the person form for a person if there is only one search result
          public bool ShouldAutoClose
          {
               get { return _shouldAutoClose; }
               set
               {
                    _shouldAutoClose = value;
                    OnPropertyChanged(nameof(ShouldAutoClose));
               }
          }

          public bool InReadMode
          {
               get
               {
                    return _inReadMode;
               }
               set
               {
                    _inReadMode = value;
                    OnPropertyChanged(nameof(InReadMode));
                    OnPropertyChanged(nameof(ReadButtonBackground));
               }
          }

          public Brush ReadButtonBackground
          {
               get
               {
                    return InReadMode ? Brushes.Red : Brushes.Transparent;
               }
          }

          public ICommand ReadButtonCommand
          {
               get
               {
                    if (_readButtonCommand == null) {
                         _readButtonCommand = new RelayCommand(ReadButton);
                    }
                    return _readButtonCommand;
               }
          }

          #endregion Properties

          #region Methods

          protected override void OnDispose()
          {
               //if (!EditMode) {
               TraceEx.PrintLog("SearchPerson: OnDispose()");

               proxCard.CardPresented -= x_CardPresented;
               proxCard.Dispose();
               proxCard = null;
               //}
          }

          private void x_CardPresented(string reader, byte[] cardData)
          {
               try {
                    Extractor.ScanCard(cardData, CurrentPerson, InReadMode);
               }
               catch (Exception ex) {
                    Trace.TraceError(ex.GetType().ToString() + " Error in CardPresented " + ex.Message);
               }
          }

          private void ReadButton(object obj)
          {
               if (obj?.GetType() == typeof(bool) && ((bool)obj == true)) {
                    InReadMode = true;
               } else {
                    InReadMode = !InReadMode;
               }
          }

          private void Search()
          {
               Benchmarker.Start("Netbox Search");
               //do actual search
               using (var db = NetboxDatabase.GetReadOnlyInstance()) {
                    IEnumerable<Person> searchQuery = from p in db.GetContext().People
                                                      where
                                                           (p.FirstName.ToLower().StartsWith(CurrentPerson.FirstName.ToLower()))
                                                      && (p.LastName.ToLower().StartsWith(CurrentPerson.LastName.ToLower()))
                                                      && (p.Company.ToLower().StartsWith(CurrentPerson.Company.ToLower()))
                                                      && (CurrentPerson.PinNumber == 0 || p.PinNumber == CurrentPerson.PinNumber)
                                                      && (CurrentPerson.FobNumber == 0 || p.FobNumber == CurrentPerson.FobNumber)
                                                      && (string.IsNullOrEmpty(CurrentPerson.FobCredential) || p.FobCredential == CurrentPerson.FobCredential)
                                                      && (CurrentPerson.OrientationNumber == 0 || p.OrientationNumber == CurrentPerson.OrientationNumber)
                                                      && (p.OrientationTestedBy.ToLower().StartsWith(CurrentPerson.OrientationTestedBy.ToLower()))
                                                      && (p.OrientationLevel.ToLower().StartsWith(CurrentPerson.OrientationLevel.ToLower()))
                                                      && (p.Deleted == ShowDeleted || p.Deleted == false)
                                                      && p.IsNetbox == true
                                                      orderby p.Company, p.LastName, p.FirstName
                                                      select p;

                    TraceEx.PrintLog("Searching Person:");
                    TraceEx.PrintLog(String.Format("{0}, {1} id={2} company={3}", CurrentPerson.LastName, CurrentPerson.FirstName, CurrentPerson.PersonId, CurrentPerson.Company));

                    if (searchQuery.Any()) {
                         if (searchQuery.Count() == 1 && ShouldAutoOpen) {
                              Person first = searchQuery.First();
                              AddPersonViewModel pvm = AddPersonViewModel.CreateAsync(new PersonViewModel(first), true);
                              _mainWindow.ShowView(pvm);
                         } else {
                              //create new multview
                              ViewMultiplePeopleViewModel viewMultWindow = new ViewMultiplePeopleViewModel();

                              var newList = new List<PersonViewModel>();

                              int capSize = 500;
                              int i = 0;
                              foreach (var searchPerson in searchQuery) {
                                   newList.Add(DataRepository.PersonDict[searchPerson.PersonId]);
                                   i++;
                                   if (i == capSize) {
                                        MainWindowViewModel.MainWindowInstance.PrintStatusText($"Only first {capSize} entries loaded.", Brushes.Black);
                                        break;
                                   }
                              }

                              //assign person list
                              viewMultWindow.PersonList = new ObservableCollection<PersonViewModel>(newList);
                              viewMultWindow.UpdatePersonList();
                              _mainWindow.ShowView(viewMultWindow);

                              string searchTitle = CreateSearchTitle();

                              viewMultWindow.DisplayName = searchTitle;
                              viewMultWindow.OnPropertyChanged("DisplayName");
                         }
                         if (ShouldAutoClose) {
                              OnRequestClose();
                         }
                    } else {
                         System.Windows.MessageBox.Show("No results found", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                    Benchmarker.Stop("Netbox Search");
               }//end using
          }

          private string CreateSearchTitle()
          {
               //generate search title
               string comp = string.Empty, name = string.Empty;
               if (!string.IsNullOrEmpty(CurrentPerson.Company)) {
                    comp = $"{CurrentPerson.Company}";
               }
               if (!string.IsNullOrEmpty(CurrentPerson.LastName)) {
                    if (!string.IsNullOrEmpty(comp)) {
                         name += ":";
                    }
                    name += CurrentPerson.LastName;
               }
               if (!string.IsNullOrEmpty(CurrentPerson.FirstName)) {
                    name += $", {CurrentPerson.FirstName}";
               }
               string searchTitle = $"Search: {comp} {name}";
               return searchTitle;
          }

          #endregion Methods
     }
}