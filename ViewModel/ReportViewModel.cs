using fyiReporting.RdlViewer;
using OfficeOpenXml;
using OfficeOpenXml.Table;
using ReportApp.Model;
using ReportApp.Utility;
using System;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms.Integration;
using System.Windows.Input;
using System.Windows.Media;

namespace ReportApp.ViewModel
{
     /// <summary>
     /// ViewModel for 'one' report
     /// </summary>
     public class ReportViewModel : WorkspaceViewModel
     {
          #region Fields

          private static System.Windows.Forms.PrintDialog printDlg;
          private ICommand _saveCsvCommand;
          private bool _dateBoxEnabled = true;
          private bool _printButtonEnabled = true;
          private ICommand _buttonPrintCommand;
          private DateTime _selectedDate;

          private WindowsFormsHost host;
          private ICommand _buttonRefreshCommand;

          #endregion Fields

          #region Constructors

          public ReportViewModel(Report _report)
          {
               var length = _report.Name.Length;

               if (length > 20) {
                    length = 20;
               }

               base.DisplayName = _report.Name.Substring(0, length);

               this.Report = _report;
               SetDate();

               if (printDlg == null) {
                    printDlg = new System.Windows.Forms.PrintDialog();
               }
          }

          #endregion Constructors

          #region Properties

          //allows aux report to be attached to main report
          public ReportViewModel AuxReport { get; set; } = null;

          public Report Report { get; private set; }
          public DataTable ReportDataTable { get; set; }
          public RdlViewer ReportViewer { get; set; } = new RdlViewer();

          public WindowsFormsHost ReportViewerForm
          {
               get
               {
                    if (host == null && !this.Closing) {
                         host = new WindowsFormsHost() { Child = ReportViewer };
                    }
                    return host;
               }
          }

          public bool Closing { get; set; } = false;

          public ICommand SaveCsvCommand
          {
               get
               {
                    if (_saveCsvCommand == null) {
                         _saveCsvCommand = new RelayCommand(async x => {
                              //await DispatcherHelper.GetDispatcher().Invoke(async() => {
                                   IsBusy = true;
                                   await SaveZipFile();
                                   IsBusy = false;
                              //});
                         });
                    }
                    return _saveCsvCommand;
               }
          }

          public DateTime SelectedDate
          {
               get
               {
                    return _selectedDate;
               }
               set
               {
                    if (value != _selectedDate) {
                         _selectedDate = value;

                         if(ShouldAutoInit) {
                              DispatcherHelper.GetDispatcher().Invoke(async () => {
                                   await Initialize();
                              });
                         }
                    }
                    OnPropertyChanged(nameof(SelectedDate));
               }
          }

          public bool ShowDateTime { get; set; } = true;

          public bool DateBoxEnabled
          {
               get
               {
                    return _dateBoxEnabled;
               }
               set
               {
                    _dateBoxEnabled = value;
                    OnPropertyChanged(nameof(DateBoxEnabled));
               }
          }

          public bool PrintButtonEnabled
          {
               get
               {
                    return _printButtonEnabled;
               }
               set
               {
                    _printButtonEnabled = value;
                    OnPropertyChanged(nameof(PrintButtonEnabled));
               }
          }

          public ICommand ButtonPrintCommand
          {
               get
               {
                    if (_buttonPrintCommand == null) {
                         _buttonPrintCommand = new RelayCommand(x => {
                              PrintButtonEnabled = false;
                              ReportPrinter.PrintDocument(ReportViewer, printDlg);
                              PrintButtonEnabled = true;
                         });
                    }
                    return _buttonPrintCommand;
               }
          }

          public ICommand ButtonRefreshCommand
          {
               get
               {
                    if (_buttonRefreshCommand == null) {
                         _buttonRefreshCommand = new RelayCommand(async (x) => {
                              await Initialize();
                         });
                    }
                    return _buttonRefreshCommand;
               }
          }

          //auto init on date change
          private bool ShouldAutoInit { get; set; } = false;

          #endregion Properties

          #region Methods

          public async Task Initialize()
          {
               try {
                    await ReportPrinter.Initialize(this, ReportViewer);
                    UpdateDateBox();
                    ShouldAutoInit = true;

                    if (AuxReport != null) {
                         await AuxReport.Initialize();
                         AuxReport.UpdateDateBox();
                    }
               }
               catch (Exception e) {
                    MessageBox.Show($"Could not open report: {e.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    Trace.TraceError($"Could not open report: Exception {e.GetType()} {e.Message}");
               }
          }

          public override void OnRequestClose()
          {
               Closing = true;
               base.OnRequestClose();
          }

          public void GenerateCsvFile(string filename)
          {
               using (var f = File.CreateText(filename)) {
                    //write columns
                    foreach (DataColumn c in ReportDataTable.Columns) {
                         f.Write($"{c.ColumnName}, ");
                    }

                    f.WriteLine();

                    //write rows
                    foreach (DataRow r in ReportDataTable.Rows) {
                         foreach (DataColumn c in ReportDataTable.Columns) {
                              f.Write($"{r[c.ColumnName]} ,");
                         }
                         f.WriteLine();
                    }
               }
          }

          public void GenerateExcelFile(string filename)
          {
               try {
                    using (ExcelPackage excelPackage = new ExcelPackage()) {
                         ExcelWorksheet worksheet = excelPackage.Workbook.Worksheets.Add("Sheet 1");

                         int row = 1;
                         int col = 1;

                         var textInfo = new CultureInfo("en-US", false).TextInfo;

                         //write columns
                         foreach (DataColumn c in ReportDataTable.Columns) {
                              worksheet.Cells[row, col++].Value = textInfo.ToTitleCase(c.ColumnName);
                         }

                         row++;
                         col = 1;

                         //write rows
                         foreach (DataRow r in ReportDataTable.Rows) {
                              foreach (DataColumn c in ReportDataTable.Columns) {
                                   var cell = worksheet.Cells[row, col];
                                   cell.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Left;

                                   if (r[c.ColumnName] is string) {
                                        //Handle datetime
                                        DateTime dttm;
                                        Double d;
                                        if (Double.TryParse((string)r[c.ColumnName], out d)) {
                                             cell.Value = d;
                                             cell.Style.Numberformat.Format = "General";
                                        } else if (DateTime.TryParse((string)r[c.ColumnName], out dttm)) {
                                             //reparse it so it doesn't assume current date
                                             dttm = DateTime.Parse((string)r[c.ColumnName], CultureInfo.CreateSpecificCulture("en-US"), DateTimeStyles.NoCurrentDateDefault);

                                             //see if it has a time
                                             if (dttm.TimeOfDay == TimeSpan.Zero) {
                                                  cell.Style.Numberformat.Format = "MMM-dd-yyyy";
                                             }
                                             //try to check if it isn't a date (doesn't work right now)
                                             else if (dttm.Year == 1 && dttm.Month == 1 && dttm.Day == 1) {
                                                  cell.Style.Numberformat.Format = "HH:mm";
                                             } else {
                                                  cell.Style.Numberformat.Format = "MMM-dd HH:mm";
                                             }
                                             cell.Value = dttm;
                                        } else {
                                             cell.Value = r[c.ColumnName];
                                        }
                                   } else {
                                        cell.Value = r[c.ColumnName];
                                   }

                                   col++;
                              }
                              row++;
                              col = 1;
                         }

                         //setup as table
                         ExcelRange range = worksheet.Cells[1, 1, ReportDataTable.Rows.Count + 1, ReportDataTable.Columns.Count];
                         ExcelTable table = worksheet.Tables.Add(range, "Table1");
                         table.TableStyle = TableStyles.Light1;

                         worksheet.Cells[range.ToString()].AutoFitColumns();

                         excelPackage.SaveAs(new FileInfo(filename));
                    }
               }
               catch (Exception e) {
                    MessageBox.Show($"Error generating excel file - {e.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    Trace.TraceError($"Error generating excel file: {filename} - {e.GetType()} {e.Message}");
               }
          }

          public async Task SaveZipFile(string filename = null, string filepath = null)
          {
               if (filename == null) {
                    var dlg = new System.Windows.Forms.SaveFileDialog {
                         FileName = Report.Name,
                         DefaultExt = ".Zip",
                         Filter = "ZIP File (.ZIP)|*.ZIP"
                    };

                    if (Directory.Exists(PathSettings.Default.DefaultSaveLocation)) {
                         dlg.InitialDirectory = PathSettings.Default.DefaultSaveLocation;
                    }
                    var result = dlg.ShowDialog();
                    if (result == System.Windows.Forms.DialogResult.Cancel) return;
                    filename = Path.GetFileName(dlg.FileName);
                    filepath = Path.GetDirectoryName(dlg.FileName);
               }

               await Task.Run(() => {
                    try {
                         var exportDir = @"c:\_Export\";
                         ReportPrinter.SetupDirectory(exportDir, createSubdir: true);

                         //setup CSV
                         var csvFilename = filename.Replace(".ZIP", ".csv");
                         GenerateCsvFile(exportDir + "\\csv\\" + csvFilename);
                         if (AuxReport != null) {
                              var auxName = "Raw Access Log Data.csv";
                              AuxReport.GenerateCsvFile(exportDir + "\\csv\\" + auxName);
                              AuxReport.GenerateExcelFile(exportDir + "\\Excel\\" + "Raw Access Log Data.xlsx");
                         }

                         //save pdf
                         var rawFilename = filename.Substring(0, filename.Length - 4);
                         ReportViewer.SaveAs($"{exportDir}{rawFilename}.pdf", fyiReporting.RDL.OutputPresentationType.PDF);

                         //save excel
                         GenerateExcelFile($"{exportDir}\\Excel\\{rawFilename}.xlsx");

                         if (AuxReport != null) {
                              AuxReport.ReportViewer.SaveAs($"{exportDir}Raw Access Log Data.pdf", fyiReporting.RDL.OutputPresentationType.PDF);
                         }

                         ZipFile.CreateFromDirectory(exportDir, filepath + "\\" + filename);

                         DispatcherHelper.GetDispatcher().Invoke(() =>
                              MainWindowViewModel.MainWindowInstance.PrintStatusText("Report file exported", Brushes.Black));
                    }
                    catch (Exception e) {
                         MessageBox.Show($"There was a problem saving the file - try changing the name.\n\n{e.Message}", "Error");
                    }
               });
          }

          protected override void OnDispose()
          {
               DispatcherHelper.GetDispatcher().Invoke(() => {
                    if (host != null) {
                         host.Dispose();
                         host = null;
                    }
                    AuxReport?.Dispose();
                    this.AuxReport = null;
                    this.Report = null;
                    this.ReportDataTable = null;
                    ReportViewer?.Dispose();
                    this.ReportViewer = null;
                    OnPropertyChanged(nameof(ReportViewerForm));
               });
          }

          private void UpdateDateBox()
          {
               DateBoxEnabled = Report.ShowDatePicker;
          }

          private void SetDate()
          {
               //offset date
               double offset = 0.0f;
               if (Report.DateOffset == -1) {
                    //if time > midnight && time <
                    DateTime now = DateTime.Now;
                    DateTime today = DateTime.Now.Date;
                    DateTime tommorow = today.AddDays(1);
                    DateTime midnight = new DateTime(today.Year, today.Month, today.Day, 0, 0, 0);
                    DateTime morning = new DateTime(today.Year, today.Month, today.Day, 7, 0, 0);
                    if (now > midnight && now < morning) {
                         offset = -1.0;
                    }
               } else {
                    offset = 0.0f;
               }
               SelectedDate = DateTime.Now.Date.AddDays(offset);
          }

          #endregion Methods
     }
}