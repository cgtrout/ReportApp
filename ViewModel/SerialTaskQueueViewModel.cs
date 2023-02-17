using System;
using System.Collections.ObjectModel;
using System.Windows.Threading;
using ReportApp.Utility;

namespace ReportApp.ViewModel
{
     /// <summary>
     /// Description of SerialTaskQueueViewModel.
     /// </summary>
     public class SerialTaskQueueViewModel : WorkspaceViewModel
     {
          #region Fields

          private readonly SerialTaskQueue serialTaskQueue;

          private DispatcherTimer updateTimer;

          #endregion Fields

          #region Constructors

          public SerialTaskQueueViewModel()
          {
               Initialize();
          }

          public SerialTaskQueueViewModel(SerialTaskQueue taskqueue, string name)
          {
               serialTaskQueue = taskqueue;

               Name = name;
               Initialize();
          }

          #endregion Constructors

          #region Properties

          public string Name { get; set; }

          public ObservableCollection<TaskWithNameViewModel> TaskList { get; private set; }

          #endregion Properties

          #region Methods

          public void UpdateTaskList(TaskWithName[] array)
          {
               //only do this temporarily
               TaskList.Clear();

               foreach (var task in array) {
                    //if(!TaskList.Contains(task)) {
                    TaskList.Add(new TaskWithNameViewModel(task));
                    //}
               }

               OnPropertyChanged(nameof(TaskList));
          }

          protected override void OnDispose()
          {
               updateTimer.Stop();
          }

          private void Initialize()
          {
               TaskList = new ObservableCollection<TaskWithNameViewModel>();
               updateTimer = new DispatcherTimer();
               updateTimer.Tick += new EventHandler(updateTimer_Tick);
               updateTimer.Interval = new TimeSpan(0, 0, 0, 0, 20);
               updateTimer.Start();
          }

          private void updateTimer_Tick(object sender, EventArgs e)
          {
               UpdateTaskList(serialTaskQueue.GetArray());
          }

          #endregion Methods
     }
}