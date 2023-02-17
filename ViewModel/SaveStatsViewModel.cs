using fyiReporting.RdlViewer;
using ReportApp.Model;
using ReportApp.Utility;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace ReportApp.ViewModel
{
     public class SaveStatsViewModel : WorkspaceViewModel
     {
          #region Fields

          private ICommand _saveZip;

          private DateTime _selectedDate = DateTime.Now.Subtract(new TimeSpan(27, 0, 0, 0));

          #endregion Fields

          #region Constructors

          public SaveStatsViewModel()
          {
               base.DisplayName = "Month end file export";
               SelectedDate = new DateTime(_selectedDate.Year, _selectedDate.Month, 1);
          }

          #endregion Constructors

          #region Properties

          public ICommand SaveZipCommand
          {
               get
               {
                    if (_saveZip == null) {
                         _saveZip = new RelayCommand(async x => await saveZip());
                    }
                    return _saveZip;
               }
          }

          public DateTime SelectedDate
          {
               get { return _selectedDate; }
               set
               {
                    _selectedDate = value;
                    OnPropertyChanged(nameof(SelectedDate));
               }
          }

          #endregion Properties

          #region Methods

          private static async Task SaveFiles(string directory, Report report, DateTime selectedDate)
          {
               ReportViewModel vm = new ReportViewModel(report);
               vm.SelectedDate = selectedDate;
               RdlViewer rdlviewer = new RdlViewer();
               await ReportPrinter.Initialize(vm, rdlviewer);
               await vm.Initialize();
               vm.GenerateCsvFile(directory + "\\CSV\\" + report.Name + ".csv");
               vm.GenerateExcelFile(directory + "\\Excel\\" + report.Name + ".xlsx");
               rdlviewer.SaveAs($"{directory + "\\PDF\\" + report.Name }.pdf", fyiReporting.RDL.OutputPresentationType.PDF);
          }

          private static void SetupDirectory(string directory)
          {
               //create directory if it doesnt exist
               DirectoryInfo di = Directory.CreateDirectory(directory);

               //delete any files/directory in directory
               foreach (var file in di.GetFiles()) {
                    file.Delete();
               }
               foreach (var dir in di.GetDirectories()) {
                    dir.Delete(recursive: true);
               }

               di.CreateSubdirectory("PDF");
               di.CreateSubdirectory("CSV");
               di.CreateSubdirectory("Excel");
          }

          private void FilePrompt(out Microsoft.Win32.SaveFileDialog dlg, out bool? result)
          {
               //show file prompt
               dlg = new Microsoft.Win32.SaveFileDialog();
               dlg.FileName = SelectedDate.ToString("yyyy MM");
               dlg.DefaultExt = ".ZIP";
               dlg.Filter = "Zip Documents (.ZIP)|*.ZIP";
               result = dlg.ShowDialog();
          }

          private async Task saveZip()
          {
               var directory = @"c:\_export\";
               MainWindowViewModel.MainWindowInstance.IsBusy = true;
               SetupDirectory(directory);

               Microsoft.Win32.SaveFileDialog dlg;
               bool? result;
               FilePrompt(out dlg, out result);
               try {
                    if (result == true) {
                         string filename = dlg.FileName;

                         //save files
                         var query = from r in Report.Reports
                                     where r.IsMonthReport == true
                                     select r;
                         foreach (var report in query) {
                              await SaveFiles(directory, report, SelectedDate);
                         }

                         //export orientation data as well
                         var orientationReport = Report.Reports.Where(x => x.Name == "Orientation - By Company").First();
                         if (orientationReport != null) {
                              await SaveFiles(directory, orientationReport, SelectedDate);
                         }

                         //zip files
                         ZipFile.CreateFromDirectory(directory, dlg.FileName);

                         MainWindowViewModel.MainWindowInstance.PrintStatusText("Report file exported", Brushes.Black);
                    }
               }
               catch (IOException e) {
                    MessageBox.Show($"There was a problem saving the file - try changing the name. {e.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
               }
               catch (Exception e) {
                    MessageBox.Show($"There was a problem saving the file - try changing the name. {e.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    Trace.TraceWarning($"SaveStatsViewModel: Unhandled exception {e.Message}");
               }
               finally {
                    MainWindowViewModel.MainWindowInstance.IsBusy = false;
               }
          }

          #endregion Methods
     }
}