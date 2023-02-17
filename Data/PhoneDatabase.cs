using ReportApp.Model;
using ReportApp.Utility;
using System;
using System.Data;
using System.Data.Linq;
using System.Linq;

namespace ReportApp.Data
{
     /// <summary>
	/// DataContext for vehicle database
	/// </summary>
	public class PhoneDatabaseContext : DataContext
     {
          #region Fields

          public Table<PhoneInfo> PhoneInfo;

          #endregion Fields

          #region Constructors

          public PhoneDatabaseContext(IDbConnection connection) : base(connection)
          {
          }

          #endregion Constructors
     }

     internal class PhoneDatabase : Database
     {
          #region Fields

          private static readonly object lockObject = new object();
          private PhoneDatabaseContext phoneContext;

          #endregion Fields

          #region Constructors

          public PhoneDatabase(string _FileName)
          {
               var FileName = _FileName;
               string connectionString = String.Format("Data Source ={0};Version=3;", FileName);
               base.Connect(connectionString);

               phoneContext = new PhoneDatabaseContext(connection);
               context = phoneContext;

               CreateTables();
          }

          #endregion Constructors

          #region Methods

          /// <summary>
          /// Clear all phone entries from database
          /// </summary>
          public void ClearEntries()
          {
               phoneContext.ExecuteCommand("delete from phoneinfo");
               phoneContext.ExecuteCommand("vacuum");
          }

          public override void CreateTables()
          {
               TableDefinition PhoneDef = new TableDefinition("phoneInfo");
               PhoneDef.Add("phoneinfoid", "integer");
               PhoneDef.Add("personid", "text");
               PhoneDef.Add("pager", "text");
               PhoneDef.Add("worknumber", "text");
               PhoneDef.Add("homenumber", "text");
               PhoneDef.Add("cellnumber", "text");
               PhoneDef.Add("importedname", "text");
               PhoneDef.Add("fullname", "text");
               CreateTable(PhoneDef);
          }

          public void EditEntry(PhoneInfo entry)
          {
               var query = from x in phoneContext.PhoneInfo
                           where x.ImportedName == entry.ImportedName
                           select x;
               if (query.Any()) {
                    var first = query.ToList().First();
                    first.PersonId = entry.PersonId;
                    first.HomeNumber = entry.HomeNumber;
                    first.CellNumber = entry.CellNumber;
                    first.WorkNumber = entry.WorkNumber;
                    first.FullName = entry.FullName;
                    first.Pager = entry.Pager;
               } else {
                    TraceEx.PrintLog($"PhoneDatabase::EditEntry - could not find {entry.ImportedName}");
                    return;
               }
               SubmitChanges();
          }

          public PhoneDatabaseContext GetContext()
          {
               return phoneContext;
          }

          public PhoneInfo GetPhone(string personId)
          {
               var query = from x in phoneContext.PhoneInfo
                           where x.PersonId == personId
                           select x;

               if (query.Any()) {
                    return query.ToList().First();
               } else {
                    return null;
               }
          }

          public override void SubmitChanges()
          {
               lock (lockObject)
                    GetContext().SubmitChanges();
          }

          #endregion Methods
     }
}