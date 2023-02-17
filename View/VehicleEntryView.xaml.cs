using ReportApp.Utility;
using ReportApp.ViewModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using System.Linq;

namespace ReportApp.View
{
     public class Pass : WorkspaceViewModel
     {
          #region Fields

          private string _passNumber = string.Empty;

          #endregion Fields

          #region Constructors

          public Pass(string v)
          {
               PassNumber = v;
          }

          #endregion Constructors

          #region Properties

          public string PassNumber
          {
               get { return _passNumber; }
               set
               {
                    _passNumber = value;
                    OnPropertyChanged(nameof(PassNumber));
               }
          }

          #endregion Properties

          #region Methods

          public override bool Equals(object obj)
          {
               var other = obj as Pass;
               if (obj == null || other == null) {
                    return false;
               }

               return this.PassNumber == other.PassNumber;
          }

          public override int GetHashCode()
          {
               return base.GetHashCode();
          }

          #endregion Methods
     }

     /// <summary>
     /// Interaction logic for VehicleEntryView.xaml
     /// </summary>
     public partial class VehicleEntryView : UserControl
     {
          #region Fields

          private const int DESIRED_PASSES = 32;
          private TextBox activeTextBox = null;
          private bool buttonDown = false;
          private Cursor cursor;
          private bool inDragDrop = false;
          private EditableTextBox SelectedPass = null;
          private Point startPoint = new Point(0, 0);

          #endregion Fields

          #region Constructors

          public VehicleEntryView()
          {
               PassList = new ObservableCollection<Pass>();

               InitializeComponent();

               DispatcherHelper.GetDispatcher().Invoke(() => {
                    LoadPassState();

                    for (int i = PassList.Count; i < DESIRED_PASSES; ++i) {
                         PassList.Add(new Pass(""));
                    }
               });

               var uri = new Uri("icons/aero_w_pass_2.cur", UriKind.Relative);

               var info = Application.GetResourceStream(uri);
               cursor = new Cursor(info.Stream);

               EventManager.RegisterClassHandler(typeof(Window), Keyboard.PreviewKeyDownEvent, new KeyEventHandler(keyDown), true);
          }

          #endregion Constructors

          #region Properties

          //list of passes
          public static ObservableCollection<Pass> PassList { get; set; }

          #endregion Properties

          #region Methods

          [DllImport("User32.dll")]
          private static extern bool SetCursorPos(int x, int y);

          private void MainDataGrid_Loaded(object sender, RoutedEventArgs e)
          {
               var vm = this.DataContext as VehicleEntriesViewModel;
               vm.ScrollIntoView = ScrollIntoView;
          }

          private void MainDataGrid_Initialized(object sender, EventArgs e)
          {
          }

          private void ScrollIntoView(ScrollVehicleEntryEventArgs objEvent)
          {
               MainDataGrid.ScrollIntoView(objEvent.Entry);
          }

          private void keyDown(object sender, KeyEventArgs e)
          {
               if (e.Key == Key.Up) {
                    if (e.KeyboardDevice.IsKeyDown(Key.RightCtrl) && e.KeyboardDevice.IsKeyDown(Key.RightShift) ||
                        e.KeyboardDevice.IsKeyDown(Key.LeftCtrl) && e.KeyboardDevice.IsKeyDown(Key.LeftShift)) {
                         e.Handled = true;
                         autoAssignPass();
                    }
               }
          }

          private void autoAssignPass()
          {
               //run on dispatcher to ensure timing stays steady
               DispatcherHelper.GetDispatcher().Invoke(new Action(() => {
                    //find last row of datagrid
                    var list = MainDataGrid.ItemsSource as ListCollectionView;

                    if (list.Count == 0) {
                         return;
                    }

                    foreach (var v in list) {
                         var item = v as VehicleEntryViewModel;

                         if (item.Deleted || !string.IsNullOrEmpty(item.TagNum) || item.Person.VehicleReader != 0) {
                              continue;
                         } else {
                              var pass = PassList[0];
                              RemovePass(pass.PassNumber);
                              item.TagNum = string.Copy(pass.PassNumber);
                              var vm = this.DataContext as VehicleEntriesViewModel;
                              TraceEx.PrintLog($"Keyboard Shortcut Auto Assign Pass {item.PersonId} {item.TagNum}");

                              vm.AddPass(pass.PassNumber, item.EntryId);

                              break;
                         }
                    }
               }));
          }

          private void AddEmptyPasses()
          {
               int passesToAdd = DESIRED_PASSES - PassList.Count;
               for (int i = 0; i < passesToAdd; i++) {
                    PassList.Add(new Pass(""));
               }
          }

          private void ButtonClear_Click(object sender, RoutedEventArgs e)
          {
               var res = MessageBox.Show("Are you sure you want to clear the passes?", "Warning", MessageBoxButton.YesNo, MessageBoxImage.Question);
               if (res == MessageBoxResult.No) return;

               PassList.Clear();
               SavePassState();
               AddEmptyPasses();
          }

          private void DataGrid_Drop(object sender, DragEventArgs e)
          {
               e.Handled = true;
               var dataGrid = e.Source as DataGrid;
               var parent = VisualTreeHelper.GetParent(e.Source as UIElement);
               var childCount = VisualTreeHelper.GetChildrenCount(e.Source as UIElement);

               var position = e.GetPosition(parent as IInputElement);

               //offset position by zoom
               position.X /= SliderZoom.Value;
               position.Y /= SliderZoom.Value;

               var hitTest = dataGrid.InputHitTest(position);

               if (hitTest.GetType() == typeof(ScrollViewer)) {
                    hitTest = (hitTest as ScrollViewer).InputHitTest(e.GetPosition(parent as IInputElement));
               }

               var textBlock = hitTest as TextBlock;

               if (textBlock == null) {
                    return;
               }

               var vehicleEntryViewModel = textBlock.DataContext as VehicleEntryViewModel;
               var bindingExpression = textBlock.GetBindingExpression(TextBlock.TextProperty);

               //only let them drag to tagnum
               if (bindingExpression.ResolvedSourcePropertyName == "TagNum") {
                    var row = GetDesiredParent(typeof(DataGridRow), textBlock) as DataGridRow;
                    var vehicle = row.DataContext as VehicleEntryViewModel;
                    dataGrid.SelectionUnit = DataGridSelectionUnit.Cell;
                    DataGridCellInfo cellInfo = new DataGridCellInfo(GetDesiredParent(typeof(DataGridCell), textBlock) as DataGridCell);
                    dataGrid.CurrentCell = cellInfo;

                    dataGrid.BeginEdit();

                    vehicle.TagNum = e.Data.GetData(DataFormats.StringFormat) as string;
                    bindingExpression.UpdateSource();

                    //select row
                    dataGrid.SelectionUnit = DataGridSelectionUnit.FullRow;
                    dataGrid.SelectedItem = row.DataContext;

                    dataGrid.CommitEdit();
                    RemovePass(vehicle.TagNum);
               }
          }

          private void DeletePass_Click(object sender, RoutedEventArgs e)
          {
               if (SelectedPass != null) {
                    RemovePass(SelectedPass.Text);
               }
          }

          private void EditableTextBox_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
          {
               SelectedPass = sender as EditableTextBox;
          }

          //recursively go up tree to find desired object
          private DependencyObject GetDesiredParent(Type type, DependencyObject depObject)
          {
               var parent = VisualTreeHelper.GetParent(depObject);
               if (parent != null) {
                    if (type == parent.GetType()) {
                         return parent;
                    } else {
                         return GetDesiredParent(type, parent);
                    }
               } else {
                    return null;
               }
          }

          private void GiveFeedbackEventHandler(object sender, GiveFeedbackEventArgs e)
          {
               if (e.Effects == DragDropEffects.Move) {
                    e.UseDefaultCursors = false;

                    Mouse.SetCursor(cursor);
               } else {
                    e.UseDefaultCursors = true;
               }
               e.Handled = true;
          }

          private void InsertPass(string number)
          {
               var index = PassList.IndexOf(new Pass(number));
               PassList.Insert(index, new Pass(""));

               RemoveExtraPasses();
               SavePassState();
          }

          private void InsertPass_Click(object sender, RoutedEventArgs e)
          {
               if (SelectedPass != null) {
                    InsertPass(SelectedPass.Text);
               }
          }

          private void LoadPassState()
          {
               foreach (var str in VehiclePasses.Default.Passes) {
                    PassList.Add(new Pass(str));
               }
          }

          private void popup_MouseMove(object sender, MouseEventArgs e)
          {
          }

          private void RemoveExtraPasses()
          {
               if (PassList.Count > DESIRED_PASSES) {
                    var index = DESIRED_PASSES;
                    PassList.RemoveAt(index);
               }
          }

          private void RemovePass(string number)
          {
               int index = 0;
               foreach (var v in PassList) {
                    if (v.PassNumber == number) {
                         break;
                    }
                    index++;
               }
               if (index != PassList.Count) {
                    PassList.RemoveAt(index);
               }

               AddEmptyPasses();

               SavePassState();
          }

          private void SavePassState()
          {
               VehiclePasses.Default.Passes.Clear();
               foreach (var v in PassList) {
                    VehiclePasses.Default.Passes.Add(v.PassNumber);
               }
               VehiclePasses.Default.Save();
          }

          private void TextBox_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
          {
          }

          //bool mouseDown = false;
          private void TextBox_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
          {
          }

          private void TextBox_MouseMove(object sender, MouseEventArgs e)
          {
          }

          private void TextBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
          {
               startPoint = e.GetPosition(null);
               activeTextBox = sender as TextBox;
               buttonDown = true;
          }

          private void TextBox_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
          {
               buttonDown = false;
          }

          private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
          {
               SavePassState();
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

          private void ViewControl_GotFocus(object sender, RoutedEventArgs e)
          {
               Keyboard.Focus(DateBox);
          }

          //this is not reliably called when switching to another tool window
          private void ViewControl_LostFocus(object sender, RoutedEventArgs e)
          {
               //DateBox.Focus();
               Keyboard.Focus(DateBox);
          }

          private void ViewControl_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
          {
          }

          private void ViewControl_MouseMove(object sender, MouseEventArgs e)
          {
               //base.OnMouseMove(e);
               var mousePos = e.GetPosition(null);
               var diff = startPoint - mousePos;

               if ((Mouse.LeftButton == MouseButtonState.Pressed && buttonDown == true) &&
                  (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance
                  || Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)) {
                    DataObject data = new DataObject();
                    if (activeTextBox != null) {
                         data.SetData(DataFormats.StringFormat, activeTextBox.Text);

                         if (inDragDrop) {
                              return;
                         }

                         TraceEx.PrintLog("inDragDrop set to true.");
                         inDragDrop = true;

                         DispatcherHelper.GetDispatcher().BeginInvoke(new Action(() => {
                              TraceEx.PrintLog("Starting DragDrop operation");
                              var pos = e.GetPosition(sender as IInputElement);
                              Trace.WriteLine($"sender pos:{pos}");
                              //SystemSounds.Beep.Play();

                              MainDataGrid.CommitEdit();

                              buttonDown = false;
                              try {
                                   DragDrop.DoDragDrop(this, data, DragDropEffects.Move);
                              }
                              catch (COMException comException) {
                                   TraceEx.PrintLog($"Drag Drop Problem: {comException.Message}");
                              }
                              catch (NullReferenceException nullException) {
                                   TraceEx.PrintLog($"Drag Drop Problem: {nullException.Message}");
                              }
                              finally {
                                   //DragDrop.RemoveGiveFeedbackHandler(this, GiveFeedbackEventHandler);
                                   this.Cursor = null;
                                   inDragDrop = false;
                                   TraceEx.PrintLog("inDragDrop set to false.");
                              }
                         }), DispatcherPriority.Background);
                    }
               }
          }

          private void MainDataGrid_PreviewKeyDown(object sender, KeyEventArgs e)
          {
               var uiElement = e.OriginalSource as UIElement;
               if (e.Key == Key.Enter && uiElement != null) {
                    e.Handled = true;
                    uiElement.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));

                    var isCommitted = MainDataGrid.CommitEdit(DataGridEditingUnit.Cell, true);
                    var rowIsCommitted = MainDataGrid.CommitEdit(DataGridEditingUnit.Row, true);
               }
          }

          private void MainDataGrid_Sorting(object sender, DataGridSortingEventArgs e)
          {
               TraceEx.PrintLog($"MainDataGrid_Sorting:: sorting '{e.Column.Header}' field. ");
               e.Handled = false;
          }

          private void MainDataGrid_LoadingRow(object sender, DataGridRowEventArgs e)
          {
          }

          #endregion Methods

          string MissingPassesFileName 
          { 
               get => PathSettings.Default.SettingsLocation + "MissingPasses.txt"; 
          }

          private void AutoFill_Click(object sender, RoutedEventArgs e)
          {
               try {
                    TraceEx.PrintLog("User clicked to auto fill");

                    if(SelectedPass == null || string.IsNullOrEmpty(SelectedPass.Text)) {
                         MessageBox.Show("Empty pass selected - type in and select the first pass you want the numbering to start from and then rightclick that pass to auto fill from that spot", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                         TraceEx.PrintLog("Empty pass selected: user presented with error");
                         return;
                    }

                    //load missing passes from file
                    var missingPassList = new SortedSet<int>();
                    using (var reader = File.OpenText(MissingPassesFileName)) {
                         while (!reader.EndOfStream) {
                              var line = reader.ReadLine();

                              if (int.TryParse(line, out int num)) {
                                   missingPassList.Add(num);
                              }
                         }
                    }

                    //take first number as start
                    int number, index = 0;

                    //if a pass is selected, find its index
                    if (SelectedPass != null) {
                         index = PassList.IndexOf(new Pass(SelectedPass.Text));
                    }

                    //get number at starting pass
                    number = int.Parse(PassList[index].PassNumber);

                    //loop through all passes, starting at index
                    for (int i = index; i < PassList.Count; i++) {
                         //skip any numbers that are in missing list file
                         while(missingPassList.Contains(number)) {
                              number++;
                         }

                         PassList[i].PassNumber = number.ToString();

                         //pad with zero
                         if(PassList[i].PassNumber.Length == 1) {
                              PassList[i].PassNumber = "0" + PassList[i].PassNumber;
                         }
                         number++;
                    }
               }
               catch (Exception ex) {
                    var msg = $"Auto fill failed {ex.GetType()}: {ex.Message}";
                    MessageBox.Show(msg);
                    Trace.TraceError(msg);
               }
          }

          private void OpenMissingPassFile_Click(object sender, RoutedEventArgs e)
          {
               try {
                    Process.Start("notepad.exe", MissingPassesFileName);
               } catch (Exception ex ) {
                    var msg = $"Open missing pass file failed {ex.GetType()}: {ex.Message}";
                    MessageBox.Show(msg);
                    Trace.TraceError(msg);
               }
          }
     }
}