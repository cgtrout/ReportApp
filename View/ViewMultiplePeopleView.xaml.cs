using ReportApp.ViewModel;
using System.Windows.Controls;
using System.Windows.Input;

namespace ReportApp.View
{
     /// <summary>
     /// Interaction logic for ViewMultiplePeopleView.xaml
     /// </summary>
     public partial class ViewMultiplePeopleView : UserControl
     {
          #region Constructors

          public ViewMultiplePeopleView()
          {
               InitializeComponent();
          }

          #endregion Constructors

          #region Methods

          private void UserControl_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
          {
               if ((Keyboard.Modifiers & ModifierKeys.Control) > 0) {
                    if (e.Delta > 0)
                         SliderZoom.Value += 0.05;
                    else
                         SliderZoom.Value -= 0.05;
               }
          }

          #endregion Methods

     }
}