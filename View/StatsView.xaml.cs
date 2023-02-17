using ReportApp.Utility;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace ReportApp.View
{
     /// <summary>
     /// Interaction logic for StatsView.xaml
     /// </summary>
     public partial class StatsView : UserControl
     {
          #region Constructors

          public StatsView()
          {
               InitializeComponent();
               printGrid.LayoutTransform = new ScaleTransform(2, 2);
          }

          #endregion Constructors

          #region Methods

          public void Print()
          {
               PrintDialog dialog = new PrintDialog();
               if (dialog.ShowDialog() != true) {
                    return;
               }
               wrapPanelName.Visibility = Visibility.Visible;
               ButtonPrint.Visibility = Visibility.Collapsed;

               //print
               Size pageSize = new Size(dialog.PrintableAreaWidth, dialog.PrintableAreaHeight);

               //var origTransform = printGrid.LayoutTransform;
               //printGrid.LayoutTransform = new ScaleTransform(2, 2);
               printGrid.Measure(pageSize);
               printGrid.Arrange(new Rect(0, 15, pageSize.Width, pageSize.Height));
               dialog.PrintVisual(printGrid, $"Daily Stats");

               //printGrid.LayoutTransform = origTransform;
               ButtonPrint.Visibility = Visibility.Visible;
               wrapPanelName.Visibility = Visibility.Collapsed;
          }

          private void ButtonPrint_Click(object sender, RoutedEventArgs e)
          {
               Print();
          }

          private void UserControl_Loaded(object sender, RoutedEventArgs e)
          {
               DispatcherHelper.GetDispatcher().BeginInvoke(new Action(() => Keyboard.Focus(TextBoxPhoneCalls)), DispatcherPriority.ContextIdle);
          }

          #endregion Methods
     }
}