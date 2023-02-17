using API_Interface;
using ReportApp.Data;
using ReportApp.Utility;
using System;
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
     /// <summary>
     /// Description of ViewMultiplePeopleViewModel.
     /// </summary>
     public class ViewMultiplePeopleViewModel : WorkspaceViewModel
     {
          #region Fields

          private ICommand _deleteCommand;
          private ICommand _editCommand;
          private MainWindowViewModel _mainWindow;
          private PersonViewModel _selectedValue;
          private ICommand _shiftEntryCommand;
          private ICommand _addAwayListCommand;

          #endregion Fields

          #region Constructors

          public ViewMultiplePeopleViewModel()
          {
               DisplayName = "View Mult";
               TraceEx.PrintLog("Opening view multiple people");

               PersonList = new ObservableCollection<PersonViewModel>(
                       (from p in DataRepository.PersonDict.Values
                        where p.IsNetbox /*&& p.Deleted == false*/
                        orderby p.Company, p.LastName, p.FirstName
                        select p)
                  );

               //PersonList.CollectionChanged += PersonList_CollectionChanged;
               UpdatePersonList();

               //need ref to mainwindow so we can spawn edit pages
               _mainWindow = MainWindowViewModel.MainWindowInstance;
          }

          #endregion Constructors

          #region Properties

          public ICommand DeleteCommand
          {
               get
               {
                    if (_deleteCommand == null) {
                         _deleteCommand = new RelayCommand(Delete);
                    }
                    return _deleteCommand;
               }
          }

          public ICommand EditCommand
          {
               get
               {
                    if (_editCommand == null) {
                         _editCommand = new RelayCommand(Edit);
                    }
                    return _editCommand;
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
                                   if (SelectedValue != null) {
                                        e.PersonId = SelectedValue.PersonId;
                                        AwayListMessageRequester.Instance.AwayListRequest(e);
                                   }
                              }), System.Windows.Threading.DispatcherPriority.Render);
                         });
                    }
                    return _addAwayListCommand;
               }
          }

          public ICollectionView PersonCollectionView { get; set; }
          public ObservableCollection<PersonViewModel> PersonList { get; set; }
          public int SelectedIndex { get; set; }

          public PersonViewModel SelectedValue
          {
               get
               {
                    return _selectedValue;
               }
               set
               {
                    _selectedValue = value;
                    OnPropertyChanged(nameof(SelectedValue));
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

          #endregion Properties

          #region Methods

          public void UpdatePersonList()
          {
               TraceEx.PrintLog("UpdatePersonList()");

               PersonCollectionView = CollectionViewSource.GetDefaultView(PersonList);
          }

          private void Delete(object _personId)
          {
               string personId = string.Empty;
               if (_personId == null) {
                    _personId = SelectedValue?.PersonId;
               }
               personId = (string)_personId;
               var query = PersonList.Where(p => p.PersonId == personId);
               if (!query.Any()) {
                    Trace.TraceWarning("ViewMult: Delete: Could not find id");
                    System.Windows.MessageBox.Show("There was a problem opening this person for deletion.", "Error");
                    return;
               } else {
                    var elem = query.First();

                    var result = MessageBox.Show("Do you want to delete this person?", "Delete", MessageBoxButton.YesNo);
                    if (result == MessageBoxResult.No) {
                         return;
                    }
                    elem.Deleted = true;
                    //ensure repository entry is set to delete
                    DataRepository.PersonDict[personId].Deleted = true;
                    DataRepository.PersonDict[personId].PinNumber = 0;
                    DataRepository.PersonDict[personId].FobNumber = 0;
                    DataRepository.PersonDict[personId].FobCredential = string.Empty;
                    DataRepository.RefreshStaticLists();

                    API_Interaction.RemovePerson(personId);

                    PersonList.Remove(elem);
                    PersonCollectionView.Refresh();

                    //remove from db
                    using (var db = NetboxDatabase.GetWriteInstance()) {
                         var list = new List<string>();
                         list.Add(personId);
                         db.MarkDeletedPerson(list);
                    }

                    MainWindowViewModel.MainWindowInstance.PrintStatusText("Person deleted", Brushes.Black);
               }
          }

          private void Edit(object personId)
          {
               PersonViewModel person = null;
               if (SelectedValue == null) return;

               if (personId == null) {
                    person = DataRepository.PersonDict[SelectedValue.PersonId];
               } else {
                    string _personId = (string)personId;
                    var query = PersonList.Where(p => p.PersonId == _personId);

                    if (!query.Any()) {
                         Trace.TraceWarning("ViewMult: Edit: Could not find id");
                         System.Windows.MessageBox.Show("There was a problem opening this person.", "Error");
                         return;
                    }

                    person = query.First();
               }
               _mainWindow.ShowView(AddPersonViewModel.CreateAsync(person, true));
          }

          private void OpenShiftEntry()
          {
               if (SelectedValue == null) return;
               var mainWindow = MainWindowViewModel.MainWindowInstance;
               ShiftEntriesViewModel vm = new ShiftEntriesViewModel(ShiftEntriesViewModel.TimeFrameSelection.Month);
               var person = SelectedValue as PersonViewModel;
               vm.IsUserSearch = false;
               vm.ShiftEntrySearchList.Add(new ShiftEntrySearch {
                    Company = person?.Company,
                    LastName = person?.LastName,
                    FirstName = person?.FirstName
               });
               vm.ReportName = $"{person?.LastName}, { person?.FirstName}";
               vm.IsUserSearch = true;
               vm.InitializeQueryAndView("ViewMultiplePeopleVM");
               mainWindow.ShowView(vm);
          }

          private void PersonList_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
          {
               //PersonList.
          }

          #endregion Methods
     }
}