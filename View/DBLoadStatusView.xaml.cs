using ReportApp.Model;
using System;
using System.Windows;
using System.Windows.Threading;

namespace ReportApp.View
{
     /// <summary>
     /// Interaction logic for DBLoaderView.xaml
     /// </summary>
     public partial class DBLoadStatusView : Window
     {
          #region Fields

          private int flashState = 0;
          private DispatcherTimer timer;

          #endregion Fields

          #region Constructors

          public DBLoadStatusView()
          {
               InitializeComponent();
               this.SizeToContent = SizeToContent.Height;
               WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;
               //DBLoadStatus.Changed += () => Dispatcher.Invoke((Action)delegate() {
               //	DBLoadStatus_Changed();
               //});
               timer = new DispatcherTimer();
               timer.Tick += new EventHandler(dispatcherTimer_Tick);
               timer.Interval = new TimeSpan(0, 0, 0, 0, 250);
               timer.Start();
          }

          #endregion Constructors

          #region Methods

          private void Button_Click(object sender, RoutedEventArgs e)
          {
               //toggle visibility
               if (ScrollView1.Visibility == Visibility.Collapsed) {
                    ScrollView1.Visibility = Visibility.Visible;
                    this.SizeToContent = SizeToContent.Manual;
                    this.Height = 300;
                    this.MaxHeight = 300;
               } else {
                    this.SizeToContent = SizeToContent.Height;
                    ScrollView1.Visibility = Visibility.Collapsed;
               }
          }

          private void DBLoadStatus_Changed()
          {
               //Debug.WriteLine("DBLoadStatus Changed");
               if (ScrollView1.Visibility == Visibility.Visible) {
                    StatusTextBlock.Text = DBLoadStatus.GetStatusText();
                    ScrollView1.ScrollToBottom();
               }
          }

          private void dispatcherTimer_Tick(object sender, EventArgs e)
          {
               DBLoadStatus_Changed();
               string flashStr = "Loading";

               switch (flashState) {
                    case 0:
                         flashStr += ".";
                         break;

                    case 1:
                         flashStr += "..";
                         break;

                    case 2:
                         flashStr += "...";
                         flashState = 0;
                         break;
               }
               flashState++;
               LoadingText.Text = flashStr;
          }

          private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
          {
               timer.Stop();
          }

          #endregion Methods
     }
}