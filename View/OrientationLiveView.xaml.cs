using ReportApp.Utility;
using ReportApp.ViewModel;
using System;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;

namespace ReportApp.View
{
     /// <summary>
     /// Interaction logic for OrientationLiveView.xaml
     /// </summary>
     public partial class OrientationLiveView : UserControl
     {
          #region Constructors

          public OrientationLiveView()
          {
               InitializeComponent();
          }

          #endregion Constructors

          #region Methods

          private void CollectionViewSource_Filter(object sender, FilterEventArgs e)
          {
               var item = e.Item as PersonViewModel;
               if (item.OrientationNumber == 0) {
                    e.Accepted = false;
               }
          }

          private void DockPanel_KeyUp(object sender, KeyEventArgs e)
          {
               if (e.Key != Key.Return) return;

               ExecuteSearch();
          }

          private void ExecuteSearch()
          {
               TextLastName.GetBindingExpression(TextBox.TextProperty).UpdateSource();
               TextFirstName.GetBindingExpression(TextBox.TextProperty).UpdateSource();
               TextCompany.GetBindingExpression(ComboBox.TextProperty).UpdateSource();
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

          private void UserControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
          {
               DispatcherHelper.GetDispatcher().BeginInvoke(new Action(() => Keyboard.Focus(TextCompany)), DispatcherPriority.ContextIdle);
          }

          private void SubmitButton_Click(object sender, System.Windows.RoutedEventArgs e)
          {
               ExecuteSearch();
          }

          #endregion Methods
     }
}