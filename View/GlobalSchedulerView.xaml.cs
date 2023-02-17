using System.Windows.Controls;

namespace ReportApp.View
{
     /// <summary>
     /// Interaction logic for GlobalSchedulerView.xaml
     /// </summary>
     public partial class GlobalSchedulerView : UserControl
     {
          #region Constructors

          public GlobalSchedulerView()
          {
               InitializeComponent();
          }

          #endregion Constructors

          #region Methods

          private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
          {
               HistoryScrollViewer.ScrollToBottom();
          }

          #endregion Methods
     }
}