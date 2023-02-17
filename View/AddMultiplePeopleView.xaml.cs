using System.Windows;
using System.Windows.Controls;

namespace ReportApp.View
{
     /// <summary>
     /// Interaction logic for AddMultiplePeopleView.xaml
     /// </summary>
     public partial class AddMultiplePeopleView : UserControl
     {
          #region Constructors

          public AddMultiplePeopleView()
          {
               InitializeComponent();
          }

          #endregion Constructors

          #region Methods

          private void DataGrid_GotFocus(object sender, RoutedEventArgs e)
          {
               if (e.OriginalSource.GetType() == typeof(DataGridCell)) {
                    DataGrid grd = (DataGrid)sender;
                    grd.BeginEdit(e);
               }
          }

          #endregion Methods
     }
}