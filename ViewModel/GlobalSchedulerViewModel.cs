using System.Collections.ObjectModel;

namespace ReportApp.ViewModel
{
     /// <summary>
     /// Description of GlobalSchedulerViewModel.
     /// </summary>
     public class GlobalSchedulerViewModel : WorkspaceViewModel
     {
          #region Constructors

          public GlobalSchedulerViewModel()
          {
               base.DisplayName = "Global Scheduler";
               TaskQueues = new ObservableCollection<SerialTaskQueueViewModel>();
               TaskQueues.Add(new SerialTaskQueueViewModel(GlobalScheduler.AccessUpdateQueue, "AccessUpdate"));
               TaskQueues.Add(new SerialTaskQueueViewModel(GlobalScheduler.DBQueue, "DBUpdate"));
               TaskQueues.Add(new SerialTaskQueueViewModel(GlobalScheduler.PersonUpdateQueue, "PersonUpdate"));
               TaskQueues.Add(new SerialTaskQueueViewModel(GlobalScheduler.APIUpdateQueue, "APIUpdate"));
               DBLoadStatus = new DBLoadStatusViewModel();
          }

          #endregion Constructors

          #region Properties

          public DBLoadStatusViewModel DBLoadStatus { get; set; }
          public ObservableCollection<SerialTaskQueueViewModel> TaskQueues { get; set; }

          #endregion Properties
     }
}