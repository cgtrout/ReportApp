using ReportApp.Data;
using ReportApp.Utility;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace ReportApp.ViewModel
{
     public delegate void AwayListRequestEventHandler(AwayListRequestEventArgs e);

     public class AwayListViewModel : WorkspaceViewModel
     {
          #region Fields

          private ICollectionView _view;
          private OtherDatabase db;
          private QueryObserver<AwayListItemViewModel> Observer;
          private RelayCommand _cellEditCommand;
          private RelayCommand _rowEditCommand;
          private ICommand _deleteCommand;
          private DispatcherTimer timer;

          #endregion Fields

          #region Constructors

          public AwayListViewModel()
          {
               base.DisplayName = "Away List";

               db = new OtherDatabase(PathSettings.Default.OtherDatabasePath);

               Observer = new QueryObserver<AwayListItemViewModel>();

               UpdateQuery();
               InitializeView();

               RemoveOldEntries();

               timer = new DispatcherTimer {
                    Interval = new TimeSpan(4, 0, 0),
               };
               timer.Tick += Timer_Tick;
               timer.Start();

               var dispatcher = DispatcherHelper.GetDispatcher();
               dispatcher.BeginInvoke(new Action(() => {
                    AwayListMessageRequester.Instance.AwayListRequestEvent += Instance_AwayListRequestEvent; ;
               }));
          }

          #endregion Constructors

          #region Properties

          public AwayListItemViewModel SelectedValue { get; set; }

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

          public void RemoveOldEntries()
          {
               var query = from x in db.GetContext().AwayListEntry
                           where DateTime.Today >= x.ReturnDate
                           select x;
               foreach (var v in query) {
                    TraceEx.PrintLog($"Removing away list entry {v.AwayListId} {v.PersonId}");
                    //delete from db
                    db.Delete(v.AwayListId);

                    //find in observer and delete
                    DispatcherHelper.GetDispatcher().Invoke(() => {
                         var observerQuery = Observer.Collection.Where(x => x.AwayListId == v.AwayListId);
                         if (observerQuery.Any()) {
                              var first = observerQuery.First();
                              Observer.Collection.Remove(first);
                              MainWindowViewModel.MainWindowInstance.PrintStatusText($"{first.Person.FullName} removed from away list", Brushes.Blue);
                         }
                    });
               }
          }

          private void Timer_Tick(object sender, EventArgs e)
          {
               //only run at early morning
               if (DateTime.Now.Hour >= 0 && DateTime.Now.Hour < 6) {
                    RemoveOldEntries();
               }
          }

          private void RowEdit()
          {
          }

          private void CellEdit()
          {
               ChangeEntry(SelectedValue);
          }

          private void ChangeEntry(AwayListItemViewModel selectedValue)
          {
               db.EditEntry(SelectedValue.AwayListInteral);
          }

          private void Delete()
          {
               var name = SelectedValue?.Person.FullName;
               db.Delete(SelectedValue.AwayListId);
               Observer.Collection.Remove(SelectedValue);
               MainWindowViewModel.MainWindowInstance.PrintStatusText($"Deleted {name} from away list", Brushes.Black);
          }

          //called when user right clicks and selects "Add to away list"
          private void Instance_AwayListRequestEvent(AwayListRequestEventArgs e)
          {
               AddNewItem(e.PersonId);
          }

          private void AddNewItem(string personId)
          {
               if (personId == null) {
                    return;
               }
               var item = new AwayListItemViewModel(new Model.AwayList());
               item.PersonId = personId;
               item.ReturnDate = DateTime.Today.AddDays(1);
               item.AwayListInteral.AwayListId = db.GetNextId();
               Observer.Collection.Add(item);
               db.AddEntry(item.AwayListInteral);
               var name = item?.Person?.FullName;
               MainWindowViewModel.MainWindowInstance.PrintStatusText($"Added {name} to away list", Brushes.Black);
          }

          private void InitializeView()
          {
               View = CollectionViewSource.GetDefaultView(Observer.Collection);
          }

          private void UpdateQuery()
          {
               var query = from x in db.GetContext().AwayListEntry
                           orderby x.ReturnDate
                           select x;
               var list = new List<AwayListItemViewModel>();
               foreach (var v in query) {
                    list.Add(new AwayListItemViewModel(v));
               }
               Observer.AddQuery(list);
               View = CollectionViewSource.GetDefaultView(Observer.Collection);
          }

          #endregion Methods
     }

     public class AwayListRequestEventArgs : EventArgs
     {
          #region Fields

          public string PersonId;

          #endregion Fields
     }

     public class AwayListMessageRequester
     {
          #region Fields

          public static AwayListMessageRequester _instance;

          #endregion Fields

          #region Events

          public event AwayListRequestEventHandler AwayListRequestEvent;

          #endregion Events

          #region Properties

          public static AwayListMessageRequester Instance
          {
               get
               {
                    if (_instance == null) {
                         _instance = new AwayListMessageRequester();
                    }
                    return _instance;
               }
          }

          #endregion Properties

          #region Methods

          public void AwayListRequest(AwayListRequestEventArgs e)
          {
               if (AwayListRequestEvent != null) {
                    AwayListRequestEvent(e);
               }
          }

          #endregion Methods
     }
}