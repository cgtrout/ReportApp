using ReportApp.ViewModel;

namespace ReportApp.Utility
{
     /// <summary>
     /// Description of TaskWithNameViewModel.
     /// </summary>
     public class TaskWithNameViewModel : WorkspaceViewModel
     {
          #region Fields

          private TaskWithName task;

          #endregion Fields

          #region Constructors

          public TaskWithNameViewModel(TaskWithName _task)
          {
               TheTask = _task;
          }

          #endregion Constructors

          #region Properties

          public bool IsRunning
          {
               get
               {
                    if (task.task.Status == System.Threading.Tasks.TaskStatus.Running) {
                         return true;
                    }
                    return false;
               }
          }

          public string Name
          {
               get { return task.Name; }
               set
               {
                    task.Name = value;
                    OnPropertyChanged(nameof(Name));
               }
          }

          public TaskWithName TheTask
          {
               get { return task; }
               set
               {
                    task = value;
                    OnPropertyChanged(nameof(TheTask));
               }
          }

          #endregion Properties
     }
}