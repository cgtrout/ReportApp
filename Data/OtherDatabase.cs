using ReportApp.Model;
using System;
using System.Data;
using System.Data.Linq;
using System.Diagnostics;
using System.Linq;
using System.Windows;

namespace ReportApp.Data
{
     /// <summary>
	/// DataContext for vehicle database
	/// </summary>
	public class OtherDatabaseContext : DataContext
     {
          #region Fields

          public Table<AwayList> AwayListEntry;

          #endregion Fields

          #region Constructors

          public OtherDatabaseContext(IDbConnection connection) : base(connection)
          {
          }

          #endregion Constructors
     }

     internal class OtherDatabase : Database
     {
          #region Fields

          private static readonly object lockObject = new object();
          private OtherDatabaseContext otherContext;

          #endregion Fields

          #region Constructors

          public OtherDatabase(string _FileName)
          {
               var FileName = _FileName;
               string connectionString = String.Format("Data Source ={0};Version=3;", FileName);
               base.Connect(connectionString);

               otherContext = new OtherDatabaseContext(connection);
               context = otherContext;

               CreateTables();
          }

          #endregion Constructors

          #region Methods

          /// <summary>
          /// Clear all phone entries from database
          /// </summary>
          public void ClearEntries()
          {
               otherContext.ExecuteCommand("delete from awaylist");
               otherContext.ExecuteCommand("vacuum");
          }

          public override void CreateTables()
          {
               TableDefinition AwayListDef = new TableDefinition("awayList");
               AwayListDef.Add("AwayListId", "integer primary key");
               AwayListDef.Add("PersonId", "text");
               AwayListDef.Add("ReturnDate", "text");
               AwayListDef.Add("Notes", "text");
               CreateTable(AwayListDef);
          }

          public long GetNextId()
          {
               if (otherContext.AwayListEntry.Any()) {
                    long id = otherContext.AwayListEntry.Max(x => x.AwayListId) + 1;
                    return id;
               } else {
                    return 0;
               }
          }

          //TODO catch exceptions
          public void AddEntry(AwayList item)
          {
               otherContext.AwayListEntry.InsertOnSubmit(item);
               SubmitChanges();
          }

          //TODO catch exceptions
          public void EditEntry(AwayList entry)
          {
               var query = from x in otherContext.AwayListEntry
                           where x.AwayListId == entry.AwayListId
                           select x;
               if (query.Any()) {
                    var first = query.ToList().First();
                    first = entry;
                    SubmitChanges();
               } else {
                    MessageBox.Show("Could not edit entry", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    Trace.TraceError($"OtherDatabase::EditEntry - could not find {entry}");
                    return;
               }
               SubmitChanges();
          }

          public OtherDatabaseContext GetContext()
          {
               return otherContext;
          }

          public AwayList GetEntry(long awayListId)
          {
               var query = from x in otherContext.AwayListEntry
                           where x.AwayListId == awayListId
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

          internal void Delete(long awayListId)
          {
               var query = from x in otherContext.AwayListEntry
                           where x.AwayListId == awayListId
                           select x;
               if (query.Any()) {
                    otherContext.AwayListEntry.DeleteOnSubmit(query.First());
                    SubmitChanges();
               } else {
                    MessageBox.Show("Could not delete entry", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    Trace.TraceError("OtherDatabase Delete - could not find entry to delete");
               }
          }

          #endregion Methods
     }
}