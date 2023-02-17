using ReportApp.Utility;
using System.Collections.Generic;
using System.Data.Linq.Mapping;
using System.IO;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Serialization;

namespace ReportApp.Model
{
     /// <summary>
     /// Description of Report.
     /// </summary>
     [Table(Name = "report")]
     [DataContract]
     public class Report
     {
          #region Constructors

          public Report()
          {
          }

          #endregion Constructors

          #region Properties

          public static List<Report> Reports { get; set; }

          [DataMember]
          [Column(Name = "databasename")]
          public string DatabaseName { get; set; }

          [DataMember]
          [Column(Name = "databasepath")]
          public string DatabasePath { get; set; }

          [DataMember]
          [Column(Name = "dateoffset")]
          public int DateOffset { get; set; }

          [DataMember]
          [Column(Name = "filename")]
          public string FileName { get; set; }

          [DataMember]
          [Column(Name = "hide")]
          public bool Hide { get; set; }

          [DataMember]
          [Column(Name = "isMonthReport")]
          public bool IsMonthReport { get; set; }

          [XmlAttribute]
          [DataMember]
          [Column(Name = "name")]
          public string Name { get; set; }

          [DataMember]
          [Column(Name = "query")]
          public string Query { get; set; }

          [XmlAttribute]
          [DataMember]
          [Column(Name = "reportid", IsPrimaryKey = true)]
          public long ReportId { get; set; }

          [DataMember]
          [Column(Name = "showdatepicker")]
          public bool ShowDatePicker { get; set; }

          [DataMember]
          [Column(Name = "showtime")]
          public bool ShowTime { get; set; }

          #endregion Properties

          #region Methods

          public static void ReportListToXML(List<Report> reportList, string filename)
          {
               FileStream writer = new FileStream(filename, FileMode.Create);
               DataContractSerializer ser = new DataContractSerializer(typeof(List<Report>));
               ser.WriteObject(writer, reportList);

               writer.Close();
          }

          public static List<Report> XMLToReportList(string filename)
          {
               List<Report> reportList = null;
               using (FileStream fs = new FileStream(filename, FileMode.Open)) {
                    XmlDictionaryReader reader = XmlDictionaryReader.CreateTextReader(fs, new XmlDictionaryReaderQuotas());
                    DataContractSerializer ser = new DataContractSerializer(typeof(List<Report>));

                    reportList = (List<Report>)ser.ReadObject(reader, true);
                    reader.Close();
               }
               return reportList;
          }

          public Report Copy()
          {
               Report report = new Report();

               report.Name = ConvertUtility.NullStringCopy(Name);
               report.ReportId = ReportId;
               report.FileName = ConvertUtility.NullStringCopy(FileName);
               report.Query = ConvertUtility.NullStringCopy(Query);
               report.DateOffset = DateOffset;
               report.ShowTime = ShowTime;
               report.ShowDatePicker = ShowDatePicker;
               report.IsMonthReport = IsMonthReport;

               if (!string.IsNullOrEmpty(DatabaseName)) {
                    report.DatabaseName = string.Copy(DatabaseName);
               }
               report.Hide = Hide;

               return report;
          }

          #endregion Methods
     }
}