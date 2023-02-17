using System.Windows.Controls;

namespace ReportApp.View
{
     /// <summary>
     /// Interaction logic for ConsoleView.xaml
     /// </summary>
     public partial class ConsoleView : UserControl
     {
          #region Constructors

          public ConsoleView()
          {
               InitializeComponent();
          }

          #endregion Constructors

          #region Methods

          private void History_TextChanged(object sender, TextChangedEventArgs e)
          {
               ScrollViewerHistory.ScrollToBottom();
          }

          #endregion Methods
     }
}