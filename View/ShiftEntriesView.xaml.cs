using System.Windows.Controls;
using System.Windows.Input;

namespace ReportApp.View
{
     /// <summary>
     /// Interaction logic for UserControl1.xaml
     /// </summary>
     public partial class ShiftEntriesView : UserControl
     {
          #region Constructors

          public ShiftEntriesView()
          {
               InitializeComponent();
          }

          #endregion Constructors

          #region Methods

          private void DockPanel_KeyUp(object sender, KeyEventArgs e)
          {
               if (e.Key != Key.Return) return;
               ExecuteSearch();
          }

          private void ExecuteSearch()
          {
               var vm = this.DataContext as ViewModel.ShiftEntriesViewModel;
               vm.IsUserSearch = false;
               //TextLastName.GetBindingExpression(TextBox.TextProperty).UpdateSource();
               //TextFirstName.GetBindingExpression(TextBox.TextProperty).UpdateSource();
               //TextCompany.GetBindingExpression(ComboBox.TextProperty).UpdateSource();
               vm.InitializeQueryAndView("xaml.cs ExecuteSearch()");
               vm.IsUserSearch = true;
          }

          private void UserControl_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
          {
               if ((Keyboard.Modifiers & ModifierKeys.Control) > 0) {
                    if (e.Delta > 0)
                         SliderZoom.Value += 0.05;
                    else
                         SliderZoom.Value -= 0.05;
               }
          }

          private void SubmitButton_Click(object sender, System.Windows.RoutedEventArgs e)
          {
               ExecuteSearch();
          }

          #endregion Methods
     }
}