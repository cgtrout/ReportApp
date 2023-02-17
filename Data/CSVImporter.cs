using ReportApp.Model;
using ReportApp.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ReportApp.Data
{
     /// <summary>
     /// AccessDataImporter Class
     ///  - used to parse access data files and reformat the data
     /// </summary>
     public class AccessDataImporter
     {
          #region Fields

          private readonly CSVImporter importer;

          #endregion Fields

          #region Constructors

          public AccessDataImporter(string filePath = "")
          {
               importer = new CSVImporter();
               FilePath = filePath;
          }

          #endregion Constructors

          #region Properties

          public string FilePath { get; set; }

          #endregion Properties

          #region Methods

          /// <summary>
          /// Build list of person from other lists and return
          /// </summary>
          /// <returns>List of Person</returns>
          public List<Person> BuildPersonList()
          {
               var companyList = GetCompanyList();
               var employeeList = GetEmployeeList();
               var orientationList = GetOrientationList();

               Int64 key = Int64.MaxValue - 1;

               //linq query to combine all into one person
               var q = from orientation in orientationList
                       join employee in employeeList
                            on orientation.EmployeeId equals employee.EmployeeId
                       join company in companyList
                            on orientation.CompanyId equals company.CompanyId
                       select new Person() {
                            PersonId = (key--).ToString(),
                            LastName = employee.LastName,
                            FirstName = employee.FirstName,
                            Company = company.CompanyName,
                            OrientationNumber = orientation.Number,
                            OrientationDate = orientation.DateTested,
                            OrientationLevel = orientation.Level,
                            ExpirationDate = orientation.Level.Contains("2") ? orientation.DateTested.AddMonths(12) : orientation.DateTested.AddMonths(6),
                            IsNetbox = false
                       };

               return q.ToList();
          }

          private List<Company> GetCompanyList()
          {
               return CSVImporter.ParseFile<Company>(FilePath + "Company.txt");
          }

          private List<Employee> GetEmployeeList()
          {
               return CSVImporter.ParseFile<Employee>(FilePath + "Employee.txt");
          }

          private List<Orientation> GetOrientationList()
          {
               return CSVImporter.ParseFile<Orientation>(FilePath + "Orientation.txt");
          }

          #endregion Methods
     }

     public class Company
     {
          #region Properties

          public long CompanyId { get; set; }
          public string CompanyName { get; set; }

          #endregion Properties
     }

     /// <summary>
     /// CSVImporter Class
     ///  - imports csv data into representative class
     /// </summary>
     public class CSVImporter
     {
          #region Methods

          /// <summary>
          /// Parse csv file into list of objects of type T
          /// </summary>
          /// <param name="file">File to parse</param>
          /// <returns>List of objects of T</returns>
          public static List<T> ParseFile<T>(string file) where T : new()
          {
               var reader = new StreamReader(file);
               string line;
               var list = new List<T>();

               //don't want the header line
               reader.ReadLine();

               while ((line = reader.ReadLine()) != null) {
                    list.Add(ParseLine(line, new T()));
               }

               return list;
          }

          private static T ParseLine<T>(string line, T input)
          {
               Type type = typeof(T);
               var elems = line.Split(',');
               int i = 0;
               var properties = type.GetProperties();
               foreach (var info in properties) {
                    Type t = info.PropertyType;
                    System.TypeCode typecode = Type.GetTypeCode(t);
                    switch (typecode) {
                         case System.TypeCode.Int64:
                              Int64 ival = 0;
                              bool res = Int64.TryParse(elems[i], out ival);

                              if (res == false) {
                                   TraceEx.PrintLog($"CSVImporter::ParseLine:: Could not parse '{line}'");
                              }

                              info.SetMethod.Invoke(input, new Object[] { ival });
                              break;

                         case System.TypeCode.String:
                              string sval = elems[i];
                              sval = sval.Trim('"');
                              sval = sval.Trim();
                              info.SetMethod.Invoke(input, new Object[] { sval });
                              break;

                         case System.TypeCode.DateTime:
                              DateTime dt = new DateTime();
                              res = DateTime.TryParse(elems[i], out dt);
                              if (res == false) {
                                   TraceEx.PrintLog($"CSVImporter::ParseLine::Could not parse '{line}'");
                              }
                              info.SetMethod.Invoke(input, new Object[] { dt });
                              break;

                         default:
                              throw new NotSupportedException("Unsupported data type");
                    }
                    i++;
               }

               return input;
          }

          #endregion Methods
     }

     public class Employee
     {
          #region Properties

          public string LastName { get; set; }
          public string FirstName { get; set; }
          public long EmployeeId { get; set; }

          #endregion Properties
     }

     public class Orientation
     {
          #region Properties

          public long Number { get; set; }
          public long EmployeeId { get; set; }
          public long CompanyId { get; set; }
          public DateTime DateTested { get; set; }
          public string Level { get; set; }

          #endregion Properties
     }
}