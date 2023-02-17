using ReportApp.Utility;
using ReportApp.ViewModel;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace ReportApp.View
{
     /// <summary>
     /// Interaction logic for SearchPerson.xaml
     /// </summary>
     public partial class SearchPersonView : UserControl
     {
          #region Constructors

          public SearchPersonView()
          {
               InitializeComponent();
          }

          #endregion Constructors

          #region Methods

          private void UserControl_Loaded(object sender, RoutedEventArgs e)
          {
               //invoke so that keyboard focus takes place after load (as this causes issue with AvalonDock
               DispatcherHelper.GetDispatcher().BeginInvoke(new Action(() => Keyboard.Focus(ComboBoxCompany)), DispatcherPriority.ContextIdle);
          }

          private void TextBoxLastName_GotFocus(object sender, RoutedEventArgs e)
          {
               (sender as TextBox).SelectAll();
               ReadButton.Command.Execute(true);
          }

          private void CompanyBox_LostFocus(object sender, RoutedEventArgs e)
          {
               var vm = DataContext as SearchPersonViewModel;
               vm.OnPropertyChanged("LastNameList");
               vm.OnPropertyChanged("FirstNameList");
          }

          private void TextBoxLastName_LostFocus(object sender, RoutedEventArgs e)
          {
               var vm = DataContext as SearchPersonViewModel;
               vm.OnPropertyChanged("FirstNameList");
          }

          #endregion Methods
     }
}