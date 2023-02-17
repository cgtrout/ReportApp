using ReportApp.Model;
using System;
using System.Windows.Threading;

namespace ReportApp.ViewModel
{
     /// <summary>
     /// Description of DBLoadStatusViewModel.
     /// </summary>
     public class DBLoadStatusViewModel : ViewModelBase
     {
          #region Fields

          private DispatcherTimer timer;

          #endregion Fields

          #region Constructors

          public DBLoadStatusViewModel()
          {
               timer = new DispatcherTimer();
               timer.Interval = new TimeSpan(0, 0, 0, 0, 16);
               timer.Tick += timer_Tick;
               timer.Start();
          }

          #endregion Constructors

          #region Properties

          public string StatusText
          {
               get { return DBLoadStatus.GetStatusText(); }
          }

          #endregion Properties

          #region Methods

          protected override void OnDispose()
          {
               timer.Stop();
          }

          private void timer_Tick(object sender, EventArgs e)
          {
               OnPropertyChanged(nameof(StatusText));
          }

          #endregion Methods
     }
}