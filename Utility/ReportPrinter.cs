using fyiReporting.RdlViewer;
using ReportApp.Data;
using ReportApp.Model;
using ReportApp.ViewModel;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Diagnostics;
using System.Drawing.Printing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace ReportApp.Utility
{
     /// <summary>
     /// Extracted methods to handle Initialization and Updating of RdlViewer
     /// </summary>
     public static class ReportPrinter
     {
          #region Methods

          /// <summary>
          ///
          /// </summary>
          /// <param name="query"></param>
          /// <param name="selectedDate"></param>
          /// <param name="reportvm"></param>
          /// <returns></returns>
          public static async Task<DataTable> GetDataTable(string query, DateTime selectedDate, ReportViewModel reportvm)
          {
               string filename = PathSettings.Default.DatabasePath;
               string connectionString = String.Format("Data Source ={0};Version=3;", filename);

               var taskQueue = GlobalScheduler.DBQueue;
               DataTable dt = null;

               var task = new System.Threading.Tasks.Task(() => {
                    using (SQLiteConnection cn = new SQLiteConnection(connectionString)) {
                         AttachDatabase(cn, reportvm);
                         using (SQLiteCommand cmd = new SQLiteCommand()) {
                              cmd.CommandType = CommandType.Text;
                              cmd.CommandText = query;
                              cmd.CommandText = cmd.CommandText.Replace("DATE", selectedDate.ToString("yyyy-MM-dd"));

                              cmd.Connection = cn;

                              TraceEx.PrintLog("GetTable");
                              TraceEx.PrintLog(cmd.CommandText);
                              
                              dt = GetTable(cmd);
                         }
                    }
               });
               taskQueue.QueueTaskEnd("Report::GetDataTable", task);

               await task;

               return dt;
          }

          /// <summary>
          /// Used to obtain table from SQLiteCommand
          /// </summary>
          /// <param name="cmd"></param>
          /// <returns>Table of data based on command</returns>
          public static DataTable GetTable(SQLiteCommand cmd)
          {
               System.Data.ConnectionState original = cmd.Connection.State;
               if (cmd.Connection.State == ConnectionState.Closed) {
                    cmd.Connection.Open();
               }

               DataTable dt = new DataTable();
               using (SQLiteDataAdapter da = new SQLiteDataAdapter(cmd)) {
                    da.Fill(dt);
                    if (original == ConnectionState.Closed) {
                         cmd.Connection.Close();
                    }
               }
               return dt;
          }

          /// <summary>
          /// Initialize RdlViewer based on data from ReportViewModel
          /// </summary>
          /// <param name="reportvm"></param>
          /// <param name="reportViewer"></param>
          public static async Task Initialize(ReportViewModel reportvm, RdlViewer reportViewer)
          {
               reportViewer.SourceFile = new Uri(reportvm.Report.FileName);
               Debug.WriteLine("In ReportView: OnLoad");
               TraceEx.PrintLog("Opening report: " + reportvm.Report.Name);

               await UpdateReportViewer(reportvm, reportViewer);
          }

          /// <summary>
          /// Print report
          /// </summary>
          /// <param name="reportViewer"></param>
          /// <param name="printDlg">Previously initialized PrintDialog</param>
          public static void PrintDocument(RdlViewer reportViewer, System.Windows.Forms.PrintDialog printDlg)
          {
               PrintDocument pd = new PrintDocument();
               pd.DocumentName = reportViewer.SourceFile.LocalPath;
               pd.PrinterSettings.FromPage = 1;
               pd.PrinterSettings.ToPage = reportViewer.PageCount;
               pd.PrinterSettings.MaximumPage = reportViewer.PageCount;
               pd.PrinterSettings.MinimumPage = 1;
               pd.DefaultPageSettings.Landscape = reportViewer.PageWidth > reportViewer.PageHeight ? true : false;

               printDlg.Document = pd;
               printDlg.AllowSomePages = true;
               printDlg.AllowCurrentPage = true;
               var dlgResult = printDlg.ShowDialog();
               if (dlgResult == System.Windows.Forms.DialogResult.OK) {
                    try {
                         reportViewer.Print(pd);
                    }
                    catch (Exception e) {
                         MessageBox.Show($"Error Printing - {e.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    }
               }
          }

          /// <summary>
          /// Bypasses dialog to print faster
          /// </summary>
          /// <param name="reportViewer"></param>
          /// <param name="printDlg"></param>
          public static void PrintFast(RdlViewer reportViewer, System.Windows.Forms.PrintDialog printDlg)
          {
               PrintDocument pd = new PrintDocument();
               pd.DocumentName = reportViewer.SourceFile.LocalPath;
               pd.PrinterSettings.FromPage = 1;
               pd.PrinterSettings.ToPage = reportViewer.PageCount;
               pd.PrinterSettings.MaximumPage = reportViewer.PageCount;
               pd.PrinterSettings.MinimumPage = 1;
               pd.DefaultPageSettings.Landscape = reportViewer.PageWidth > reportViewer.PageHeight ? true : false;

               printDlg.Document = pd;
               printDlg.AllowSomePages = true;
               printDlg.AllowCurrentPage = true;

               try {
                    reportViewer.Print(pd);
               }
               catch (Exception e) {
                    MessageBox.Show($"Error Printing - {e.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Exclamation);
               }
          }

          /// <summary>
          /// Print a report based on name from list of reports
          /// </summary>
          /// <param name="name"></param>
          /// <param name="reports"></param>
          /// <param name="dateToPrint"></param>
          public static async Task PrintReport(string name, List<Report> reports, DateTime? dateToPrint = null)
          {
               Report report = FindReportInList(name, reports);
               await PrintReport(dateToPrint, report);
          }

          public static Report FindReportInList(string name, List<Report> reports)
          {
               var reportquery = (from r in reports
                                  where r.Name.Contains(name)
                                  select r);
               Report report = null;
               if (!reportquery.Any()) {
                    Trace.TraceWarning("PrintReport: could not find report starting with: " + name);
               } else {
                    report = reportquery.First();
               }

               return report;
          }

          public static async Task PrintReport(DateTime? dateToPrint, Report report)
          {
               if (report == null) return;

               ReportViewModel reportVM = CreateReportVM(dateToPrint, report);

               RdlViewer rdlviewer = new RdlViewer();
               await ReportPrinter.Initialize(reportVM, rdlviewer);
               ReportPrinter.PrintFast(rdlviewer, new System.Windows.Forms.PrintDialog());
          }

          public static ReportViewModel CreateReportVM(DateTime? dateToPrint, Report report)
          {
               var reportVM = new ReportViewModel(report);
               if (dateToPrint != null) {
                    reportVM.SelectedDate = dateToPrint.GetValueOrDefault();
               }

               return reportVM;
          }

          public static async Task ZipRollCalls()
          {
               try {
                    var dir = @"C:\_rollcall export\";
                    SetupDirectory(dir, false);
                    SetupDirectory(dir + "\\Excel", false);
                    MainWindowViewModel.MainWindowInstance.IsBusy = true;

                    //get total headcount
                    int headcount = DataRepository.RollCallDict.Count();

                    //file prompt
                    var dlg = new Microsoft.Win32.SaveFileDialog();
                    var dateString = DateTime.Now.ToString("yyyy-MMM-dd HHmm");
                    string rollCallName = $"RollCall {dateString}hrs TotalCount={headcount}";
                    dlg.FileName = rollCallName;
                    dlg.DefaultExt = ".ZIP";
                    dlg.Filter = "Zip Documents (.ZIP)|*.ZIP";

                    if (Directory.Exists(PathSettings.Default.DefaultSaveLocation)) {
                         dlg.InitialDirectory = PathSettings.Default.DefaultSaveLocation;
                    }
                    var result = dlg.ShowDialog();

                    if (result == false) return;
                    var fileName = dlg.FileName;

                    //initialize required variables
                    var rollCallReport = FindReportInList("Roll Call - Company", Report.Reports).Copy();
                    var vm = ReportPrinter.CreateReportVM(DateTime.Now, rollCallReport);
                    RdlViewer rdlviewer = new RdlViewer();
                    await ReportPrinter.Initialize(vm, rdlviewer);

                    //save main rollcall
                    SaveFiles(dir, rollCallName, vm, rdlviewer, saveCsv: false, saveExcel: false, savePDF:true);
                    SaveFiles(dir, "_All Companies", vm, rdlviewer, saveCsv: false, saveExcel: true, savePDF: false);

                    //get list of companies
                    List<string> compList;
                    using (var db = NetboxDatabase.GetReadOnlyInstance()) {
                         var context = db.GetContext();
                         var compQuery = from c in context.RollCalls
                                         join p in context.People
                                         on c.PersonId equals p.PersonId
                                         select p.Company;
                         compList = compQuery.Distinct().ToList();
                    }

                    //generate roll call for each company
                    foreach (var company in compList) {
                         string companyName = company;

                         var companyNameForQuery = company.Replace("'", "''");

                         //save to dir
                         var report = FindReportInList("Roll Call - Company", Report.Reports).Copy();
                         report.Query = $@"select company as 'Company', lastname as 'Last', firstname as 'First', dttm as 'In Time', printf(' % .1f',((strftime('%s', 'now')-strftime('%s', dttm))/3600.0)-7) as 'Time In', reader as 'Reader', employeecategory as cat
                                        from rollcall
                                        inner join person
                                        on rollcall.personid = person.personid
                                        where deleted = 0
                                             and company like '{companyNameForQuery}'
                                        order by company, lastname, firstname";
                         var reportvm2 = CreateReportVM(DateTime.Now, report);
                         await UpdateReportViewer(reportvm2, rdlviewer);
                         await reportvm2.Initialize();
                         SaveFiles(dir, companyName, reportvm2, rdlviewer, saveCsv: false, saveExcel: true, savePDF:false);
                    }

                    //zip file
                    ZipFile.CreateFromDirectory(dir, fileName);
               }
               finally {
                    MainWindowViewModel.MainWindowInstance.IsBusy = false;
               }
          }

          public static void UpdateData(RdlViewer reportViewer, DataTable dataTable)
          {
               reportViewer.Report.DataSets["Data"].SetData(dataTable);
          }

          public static void SaveFiles(string directory, string reportName, ReportViewModel vm, RdlViewer rdlviewer, bool saveCsv, bool saveExcel, bool savePDF)
          {
               //remove slashes to prevent crash - other puct will be removed
               var modifiedReportName = reportName.Replace('\\', ' ');
               modifiedReportName = modifiedReportName.Replace('/', ' ');

               if (saveCsv) {
                    vm.GenerateCsvFile($@"{directory}\CSV\{modifiedReportName}.CSV");
               }
               if (saveExcel) {
                    vm.GenerateExcelFile($@"{directory}\Excel\{modifiedReportName}.xlsx");
               }
               if (savePDF) {
                    rdlviewer.SaveAs($@"{directory}{modifiedReportName}.pdf", fyiReporting.RDL.OutputPresentationType.PDF);
               }
          }

          public static void SetupDirectory(string directory, bool createSubdir = true)
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

               if (createSubdir) {
                    di.CreateSubdirectory("CSV");
                    di.CreateSubdirectory("Excel");
               }
          }

          /// <summary>
          /// Attach a secondary database to main database
          /// </summary>
          /// <param name="cn"></param>
          /// <param name="reportvm"></param>
          private static void AttachDatabase(SQLiteConnection cn, ReportViewModel reportvm)
          {
               if (String.IsNullOrEmpty(reportvm.Report.DatabasePath)) {
                    return;
               }
               using (SQLiteCommand cmd = new SQLiteCommand()) {
                    var report = reportvm.Report;
                    cmd.CommandText = String.Format("ATTACH DATABASE '{0}' as '{1}'", report.DatabasePath, report.DatabaseName);
                    cmd.Connection = cn;
                    cn.Open();
                    cmd.ExecuteNonQuery();
               }
          }

          /// <summary>
          /// Update RdlViewer based on information from ReportVM
          /// </summary>
          /// <param name="reportvm"></param>
          /// <param name="reportViewer"></param>
          private static async Task UpdateReportViewer(ReportViewModel reportvm, RdlViewer reportViewer)
          {
               string query = reportvm.Report.Query;
               DateTime selectedDate = reportvm.SelectedDate;

               reportViewer.Hide();
               var dataTable = await GetDataTable(query, selectedDate, reportvm);

               UpdateData(reportViewer, dataTable);

               reportvm.ReportDataTable = dataTable;
               reportvm.ReportViewer = reportViewer;

               //handle showtime
               string showTimeText = "";
               if (reportvm.Report.ShowTime) {
                    showTimeText = String.Format("({0})", DateTime.Now.ToString("HH:mm"));
               }

               reportViewer.Parameters = null;

               if (reportvm.Report.IsMonthReport) {
                    reportViewer.Parameters += $"&NewTitle={reportvm.Report.Name} - {reportvm.SelectedDate.ToString("MMMMM yyyy")}";
               } else if (reportvm.ShowDateTime == false) {
                    reportViewer.Parameters += $"&NewTitle={reportvm.Report.Name}";
               } else {
                    reportViewer.Parameters += string.Format("&NewTitle={0} - {1} {2}", reportvm.Report.Name, reportvm.SelectedDate.ToString("MMMMM dd, yyyy"), showTimeText);
               }

               reportViewer.HideRunButton();
               reportViewer.Rebuild();
               reportViewer.Show();
          }

          #endregion Methods
     }
}