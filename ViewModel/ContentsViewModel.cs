using ReportApp.Model;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace ReportApp.ViewModel
{
     internal class ContentsViewModel : WorkspaceViewModel
     {
          #region Fields

          private ReadOnlyCollection<CommandViewModel> _editCommands;
          private ReadOnlyCollection<CommandViewModel> _monthEndReportCommands;
          private ReadOnlyCollection<CommandViewModel> _reportCommands;
          private ReadOnlyCollection<CommandViewModel> _viewCommands;

          #endregion Fields

          #region Constructors

          public ContentsViewModel()
          {
               base.DisplayName = "Table of Contents";
          }

          #endregion Constructors

          #region Properties

          /// <summary>
          /// Returns a read-only list of commands
          /// that the UI can display and execute.
          /// </summary>
          public ReadOnlyCollection<CommandViewModel> EditCommands
          {
               get
               {
                    if (_editCommands == null) {
                         List<CommandViewModel> cmds = this.CreateEditCommands();
                         _editCommands = new ReadOnlyCollection<CommandViewModel>(cmds);
                    }
                    return _editCommands;
               }
          }

          public ReadOnlyCollection<CommandViewModel> MonthEndReportCommands
          {
               get
               {
                    if (_monthEndReportCommands == null) {
                         List<CommandViewModel> cmds = this.CreateMonthEndReportCommands();
                         _monthEndReportCommands = new ReadOnlyCollection<CommandViewModel>(cmds);
                    }
                    return _monthEndReportCommands;
               }
          }

          /// <summary>
          /// Returns a read-only list of commands
          /// that the UI can display and execute.
          /// </summary>
          public ReadOnlyCollection<CommandViewModel> ReportCommands
          {
               get
               {
                    if (_reportCommands == null) {
                         List<CommandViewModel> cmds = this.CreateReportCommands();
                         _reportCommands = new ReadOnlyCollection<CommandViewModel>(cmds);
                    }
                    return _reportCommands;
               }
          }

          public ReadOnlyCollection<CommandViewModel> ViewCommands
          {
               get
               {
                    if (_viewCommands == null) {
                         List<CommandViewModel> cmds = this.CreateViewCommands();
                         _viewCommands = new ReadOnlyCollection<CommandViewModel>(cmds);
                    }
                    return _viewCommands;
               }
          }

          #endregion Properties

          #region Methods

          private List<CommandViewModel> CreateEditCommands()
          {
               List<CommandViewModel> list = new List<CommandViewModel>();

               var mainWindow = MainWindowViewModel.MainWindowInstance;

               //list.Add(new CommandViewModel("Add Multiple People", new RelayCommand(P => this.ShowView(new AddMultiplePeopleViewModel()))));

               list.Add(new CommandViewModel("Add Person (F1)", new RelayCommand(P => mainWindow.ShowView(AddPersonViewModel.Create(new PersonViewModel(new Person()), false)))));
               list.Add(new CommandViewModel("Netbox Search (F2)", new RelayCommand(P => mainWindow.ShowView(new SearchPersonViewModel()))));
               list.Add(new CommandViewModel("View Full Person List", new RelayCommand(P => mainWindow.ShowView(new ViewMultiplePeopleViewModel()))));
               //list.Add(new CommandViewModel("View Phone List", new RelayCommand(P => mainWindow.ShowView(new PhoneViewModel()))));

               return list;
          }

          private List<CommandViewModel> CreateMonthEndReportCommands()
          {
               List<CommandViewModel> list = new List<CommandViewModel>();

               var query = from r in Report.Reports
                           where r.Hide == false && r.IsMonthReport == true
                           orderby r.Name
                           select r;

               foreach (var r in query) {
                    if (r.Hide == true || r.IsMonthReport == false) {
                         continue;
                    }
                    list.Add(new CommandViewModel(r.Name, new RelayCommand(async P => await MainWindowViewModel.MainWindowInstance.ShowReport(r))));
               }

               return list;
          }

          private List<CommandViewModel> CreateReportCommands()
          {
               List<CommandViewModel> list = new List<CommandViewModel>();

               var query = from r in Report.Reports
                           where r.Hide == false && r.IsMonthReport == false
                           orderby r.Name
                           select r;

               foreach (var r in query) {
                    if (r.Hide == true || r.IsMonthReport == true) {
                         continue;
                    }
                    list.Add(new CommandViewModel(r.Name, new RelayCommand(async P => await MainWindowViewModel.MainWindowInstance.ShowReport(r))));
               }

               return list;
          }

          private List<CommandViewModel> CreateViewCommands()
          {
               List<CommandViewModel> list = new List<CommandViewModel>();

               var mainWindow = MainWindowViewModel.MainWindowInstance;

               //list.Add(new CommandViewModel("Add Multiple People", new RelayCommand(P => this.ShowView(new AddMultiplePeopleViewModel()))));
               list.Add(new CommandViewModel("Access Logs (F7)", new RelayCommand(P => mainWindow.ShowView(new AccessEntriesViewModel()))));
               //list.Add(new CommandViewModel("RollCall", new RelayCommand(P => mainWindow.ShowView(new RollCallViewModel()))));
               list.Add(new CommandViewModel("Orientation Search (F3)", new RelayCommand(P => mainWindow.ShowView(new OrientationsLiveViewModel()))));
               list.Add(new CommandViewModel("Shift Entries (F8)", new RelayCommand(P => mainWindow.ShowView(new ShiftEntriesViewModel(initialize: true)))));

               //
               //list.Add(new CommandViewModel("Vehicle Entries", new RelayCommand(P => {
               //     var vm = new VehicleEntriesViewModel(PathSettings.Default.VehicleDatabasePath);
               //     bool showing = mainWindow.ShowView(vm);

               //     if(showing) {
               //          vm.Initialize();
               //     }

               //})));

               return list;
          }

          #endregion Methods
     }
}