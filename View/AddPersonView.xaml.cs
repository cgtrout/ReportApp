using ReportApp.Utility;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;

namespace ReportApp.View
{
     /// <summary>
     /// Interaction logic for AddPersonView.xaml
     /// </summary>
     public partial class AddPersonView : UserControl
     {
          #region Constructors

          public AddPersonView()
          {
               InitializeComponent();
          }

          #endregion Constructors

          #region Methods

          private static void ClosePopup(object sender)
          {
               var popup = sender as Popup;
               popup.IsOpen = false;
          }

          private static string EnforceInitialStandard(string str)
          {
               str = str.Replace(" ", string.Empty);
               if (str.Length == 4) {
                    str = str.ToLower();
                    str = $"{str[0].ToString().ToUpper()}{str[1]}{str[2].ToString().ToUpper()}{str[3]}";
               } else {
                    str = str.ToUpper();
               }

               return str;
          }

          private void ComboBoxOldCompContact_LostFocus(object sender, RoutedEventArgs e)
          {
               var str = ComboBoxOldCompContact.Text;
               str = EnforceInitialStandard(str);
               ComboBoxOldCompContact.Text = str;
          }

          private void ComboBoxTestedBy_LostFocus(object sender, RoutedEventArgs e)
          {
               ComboBoxTestedBy.Text = ComboBoxTestedBy.Text.Trim(' ');
               ComboBoxTestedBy.Text = EnforceInitialStandard(ComboBoxTestedBy.Text);
          }

          private void CompanyPopup_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
          {
               ClosePopup(sender);
          }

          private void OrientationNumber_LostFocus(object sender, RoutedEventArgs e)
          {
               if (string.IsNullOrEmpty(TextBoxOrientationNumber.Text)) {
                    TextBoxOrientationNumber.Text = "0";
               }
          }

          private void OrientationPopup_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
          {
               ClosePopup(sender);
          }

          private void TextBoxFirst_LostFocus(object sender, RoutedEventArgs e)
          {
               TextInfo ti = new CultureInfo("en-US", false).TextInfo;
               TextBoxFirst.Text = ti.ToTitleCase(TextBoxFirst.Text.ToLower());
          }

          private void UserControl_Loaded(object sender, RoutedEventArgs e)
          {
               //invoke so that keyboard focus takes place after load (as this causes issue with AvalonDock
               DispatcherHelper.GetDispatcher().BeginInvoke(new Action(() => Keyboard.Focus(ComboBoxCompany)), DispatcherPriority.ContextIdle);

               //force upper char casing on company combobox
               (ComboBoxCompany.Template.FindName("PART_EditableTextBox", ComboBoxCompany) as TextBox).CharacterCasing = CharacterCasing.Upper;
          }

          private void PinPopup_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
          {
               ClosePopup(sender);
          }

          private void FobPopup_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
          {
               ClosePopup(sender);
          }

          private void TextBox_GotFocus(object sender, RoutedEventArgs e)
          {
               (sender as TextBox).SelectAll();
               ReadButton.Command.Execute(true);
          }

          private void LevelTextBox_LostFocus(object sender, RoutedEventArgs e)
          {
               LevelTextBox.Text = LevelTextBox.Text.ToUpper();
          }

          #endregion Methods
     }
}