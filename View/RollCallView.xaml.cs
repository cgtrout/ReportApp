using ReportApp.Utility;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ReportApp.View
{
     /// <summary>
     /// Interaction logic for RollCallView.xaml
     /// </summary>
     public partial class RollCallView : UserControl
     {
          #region Constructors

          public RollCallView()
          {
               InitializeComponent();

               EventManager.RegisterClassHandler(typeof(Window), Keyboard.KeyUpEvent, new KeyEventHandler(keyUp), true);
          }

          #endregion Constructors

          #region Methods

          private static DependencyObject GetExpander(DependencyObject container)
          {
               if (container is Expander) return container;

               for (var i = 0; i < VisualTreeHelper.GetChildrenCount(container); i++) {
                    var child = VisualTreeHelper.GetChild(container, i);

                    var result = GetExpander(child);
                    if (result != null) {
                         return result;
                    }
               }
               return null;
          }

          private void keyUp(object sender, KeyEventArgs e)
          {
               var vm = this.DataContext as ViewModel.RollCallViewModel;
               if (e.SystemKey == Key.F10) {
                    TraceEx.PrintLog("Keyboard Shortcut F10");
                    e.Handled = true;
                    vm.CurrentSortSelection++;
                    if (vm.CurrentSortSelection == (int)ViewModel.RollCallViewModel.SortSelection.TimeIn + 1) {
                         vm.CurrentSortSelection = 0;
                    }
               } else if (e.Key == Key.F11) {
                    TraceEx.PrintLog("Keyboard Shortcut F11");
                    Collapse();
               } else if (e.Key == Key.F12) {
                    TraceEx.PrintLog("Keyboard Shortcut F12");
                    Expand();
               }
          }

          private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
          {
          }

          private void DockPanel_KeyUp(object sender, KeyEventArgs e)
          {
               if (e.Key != Key.Return) return;

               TextLastName.GetBindingExpression(TextBox.TextProperty).UpdateSource();
               TextFirstName.GetBindingExpression(TextBox.TextProperty).UpdateSource();
               TextCompany.GetBindingExpression(TextBox.TextProperty).UpdateSource();
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

          private void ExpanderSearch_Expanded(object sender, System.Windows.RoutedEventArgs e)
          {
               ComboBoxSearch.Visibility = System.Windows.Visibility.Hidden;
               ButtonPrint.Visibility = System.Windows.Visibility.Hidden;
          }

          private void ExpanderSearch_Collapsed(object sender, System.Windows.RoutedEventArgs e)
          {
               ComboBoxSearch.Visibility = System.Windows.Visibility.Visible;
               ButtonPrint.Visibility = System.Windows.Visibility.Visible;
          }

          private void ButtonCollapse_Click(object sender, System.Windows.RoutedEventArgs e)
          {
               Collapse();
          }

          private void Collapse()
          {
               if (MainListBox.Items.Groups == null) return;

               TraceEx.PrintLog("Roll call header : Collapse");

               foreach (var item in MainListBox.Items.Groups) {
                    var listboxitem = MainListBox.ItemContainerGenerator.ContainerFromItem(item);
                    var itemExpander = (Expander)GetExpander(listboxitem);
                    if (itemExpander != null) {
                         itemExpander.IsExpanded = false;
                    }
               }
          }

          private void ButtonExpand_Click(object sender, System.Windows.RoutedEventArgs e)
          {
               Expand();
          }

          private void Expand()
          {
               if (MainListBox.Items.Groups == null) return;

               TraceEx.PrintLog("Roll call header : Expand");

               foreach (var item in MainListBox.Items.Groups) {
                    var listboxitem = MainListBox.ItemContainerGenerator.ContainerFromItem(item);
                    var itemExpander = (Expander)GetExpander(listboxitem);
                    if (itemExpander != null) {
                         itemExpander.IsExpanded = true;
                    }
               }
          }

          #endregion Methods
     }
}