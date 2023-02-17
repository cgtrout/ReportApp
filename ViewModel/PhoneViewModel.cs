using ReportApp.Data;
using ReportApp.Utility;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;
using System.Windows.Input;

namespace ReportApp.ViewModel
{
     public class PhoneViewModel : WorkspaceViewModel
     {
          #region Fields

          public PhoneItemViewModel _selectedValue;

          /// <summary>
          /// RowEditCommand
          /// </summary>
          private ICommand _rowEditCommand;

          private bool _showHiddenElements = false;
          private ICollectionView _view;
          private PhoneDatabase db;
          private QueryObserver<PhoneItemViewModel> Observer;

          #endregion Fields

          #region Constructors

          public PhoneViewModel()
          {
               base.DisplayName = "Phone Info";
               Observer = new QueryObserver<PhoneItemViewModel>();

               db = new PhoneDatabase(PathSettings.Default.PhoneDatabasePath);

               UpdateQuery();
               InitializeView();
          }

          #endregion Constructors

          #region Properties

          public List<PersonViewModel> PersonList
          {
               get
               {
                    return DataRepository.SortedList;
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

          public PhoneItemViewModel SelectedValue
          {
               get { return _selectedValue; }
               set
               {
                    _selectedValue = value;
                    OnPropertyChanged(nameof(SelectedValue));
               }
          }

          public bool ShowHiddenElements
          {
               get { return _showHiddenElements; }
               set
               {
                    _showHiddenElements = value;
                    OnPropertyChanged(nameof(ShowHiddenElements));
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

          private void InitializeView()
          {
               View = CollectionViewSource.GetDefaultView(Observer.Collection);
          }

          private void RowEdit()
          {
               if (SelectedValue == null) {
                    TraceEx.PrintLog("ShiftEntriesVM: selectedValue is null");
                    return;
               }
               db.EditEntry(SelectedValue.UnderlyingItem);
          }

          private void UpdateQuery()
          {
               var query = from x in db.GetContext().PhoneInfo
                           orderby x.FullName
                           select x;
               var list = new List<PhoneItemViewModel>();
               foreach (var v in query) {
                    list.Add(new PhoneItemViewModel(v));
               }
               Observer.AddQuery(list);
               View = CollectionViewSource.GetDefaultView(Observer.Collection);
          }

          #endregion Methods
     }
}