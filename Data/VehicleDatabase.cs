using ReportApp.Model;
using ReportApp.Utility;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Linq;
using System.Data.SQLite;
using System.Diagnostics;
using System.Linq;
using System.Windows;

namespace ReportApp.Data
{
     /// <summary>
     /// Description of Database.
     /// </summary>
     public sealed class VehicleDatabase : Database
     {
          #region Fields

          public static readonly object LockObject = new object();
          private VehicleDatabaseContext vehicleContext;

          #endregion Fields

          #region Constructors

          private VehicleDatabase(string _FileName, bool readOnly = false)
          {
               var FileName = _FileName;
               string connectionString = String.Format("Data Source ={0};Version=3;", FileName);
               base.Connect(connectionString);
               if (readOnly) {
                    //connectionString += "; Read Only=True";
               }

               vehicleContext = new VehicleDatabaseContext(connection);
               context = vehicleContext;

               CreateTables();
          }

          #endregion Constructors

          #region Methods

          public static VehicleDatabase GetReadOnlyInstance()
          {
               var newDb = new VehicleDatabase(PathSettings.Default.VehicleDatabasePath, readOnly: true);
               newDb.DisableObjectTracking();
               return newDb;
          }

          public static VehicleDatabase GetWriteInstance()
          {
               var newDb = new VehicleDatabase(PathSettings.Default.VehicleDatabasePath, readOnly: false);
               return newDb;
          }

          public long GetNextId()
          {
               //TODO handle empty database issue
               //lock(LockObject) {
               long id = vehicleContext.VehicleEntries.Max(x => x.EntryId) + 1;
               return id;
               //}
          }

          public Table<VehicleEntry> GetTable()
          {
               return GetContext().VehicleEntries;
          }

          /// <summary>
          /// Add a new entry
          /// </summary>
          /// <param name="entry"></param>
          public void AddEntry(VehicleEntry entry)
          {
               TraceEx.PrintLog($"VehicleDatabase::AddEntry p={entry.PersonId} l={entry.LicNum}");
               vehicleContext.VehicleEntries.InsertOnSubmit(entry);
               SubmitChanges();
          }

          /// <summary>
          /// Creates all tables for application
          /// </summary>
          public override void CreateTables()
          {
               TableDefinition VehicleDef = new TableDefinition("vehicleentry");
               VehicleDef.Add("entryid", "integer");
               VehicleDef.Add("intime", "text");
               VehicleDef.Add("outtime", "text");
               VehicleDef.Add("inid", "integer");
               VehicleDef.Add("outid", "integer");
               VehicleDef.Add("personid", "text");
               VehicleDef.Add("tagnum", "text");
               VehicleDef.Add("licnum", "text");
               VehicleDef.Add("make", "text");
               VehicleDef.Add("model", "text");
               VehicleDef.Add("color", "text");
               VehicleDef.Add("deleted", "integer");

               CreateTable(VehicleDef);
          }

          public void DeleteEntriesFromPerson(string personId)
          {
               var dbQuery = from v in GetContext().VehicleEntries
                             where v.PersonId == personId
                             select v;
               foreach (var v in dbQuery) {
                    GetContext().VehicleEntries.DeleteOnSubmit(v);
               }
               SubmitChanges();
          }

          public int GetVehicleCount(DateTime selectedDate)
          {
               var query = from x in GetContext().VehicleEntries
                           where x.InTime > selectedDate && x.InTime < selectedDate.AddHours(24)
                                && x.Deleted == false
                           select x;

               if (query.Any()) {
                    return query.ToList().Count();
               } else {
                    return 0;
               }
          }

          /// <summary>
          /// Get entry from DB based on id
          /// </summary>
          /// <param name="id"></param>
          /// <returns></returns>
          public VehicleEntry GetEntry(long id)
          {
               var query = from x in GetContext().VehicleEntries
                           where x.EntryId == id
                           select x;
               if (query.Any()) {
                    return query.ToList().First();
               } else {
                    Trace.TraceError($"VehicleDatabase::GetEntry:: Could not find id={id}");
                    return null;
               }
          }

          public IEnumerable<VehicleEntry> GetYesterdayQuery(DateTime date)
          {
               return from x in GetContext().VehicleEntries
                      where x.InTime > date.AddHours(-12)
                            && x.InTime < date
                            && x.Deleted == false
                            && x.OutId == 0
                      select x;
          }

          public IEnumerable<VehicleEntry> GetTodayQuery(DateTime date)
          {
               return from x in GetContext().VehicleEntries
                      where x.InTime > date && x.InTime < date.AddDays(1) && x.Deleted == false
                      select x;
          }

          /// <summary>
          /// Edit an entry
          /// </summary>
          /// <param name="entry"></param>
          public void EditEntry(VehicleEntry entry)
          {
               try {
                    TraceEx.PrintLog($"VehicleDB: EditEntry {entry.PersonId} l={entry.LicNum} t={entry.TagNum}");
                    var query = vehicleContext.VehicleEntries.Where(e => entry.EntryId == e.EntryId);

                    if (query == null) {
                         Trace.TraceError($"VehicleDatabase:EditEntry: query object is null.");
                         TraceEx.PrintLog($"Entry={entry}");
                         return;
                    }

                    if (query.Any()) {
                         var first = query.Single();
                         first.CopyFromOther(entry);
                         SubmitChanges();
                    } else {
                         Trace.TraceWarning($"VehicleDatabase: EditEntry: Cound not find entry {entry.PersonId} l={entry.LicNum}");
                    }
               }
               catch (NullReferenceException nre) {
                    MessageBox.Show("There was an issue writing to the database.  Please double check that data was changed properly", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    Trace.TraceWarning("VehicleDatabase::EditEntry NullReference Exception e: " + nre.Message);
                    TraceEx.PrintLog(Environment.StackTrace);
               }
               catch (SQLiteException e) {
                    MessageBox.Show("There was a problem writing to the database.  It is recommended you close and reopen the form.  Your data may not have been written.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    Trace.TraceWarning($"VehicleDatabase::EditEntry SQLiteException on Entry: {entry.EntryId} ExceptionMessage: {e.Message}");
                    TraceEx.PrintLog(Environment.StackTrace);
               }
          }

          public override void SubmitChanges()
          {
               lock (LockObject) {
                    try {
                         GetContext().SubmitChanges(failureMode: ConflictMode.FailOnFirstConflict);
                    }
                    catch (ChangeConflictException e) {
                         LogChangeConflict(e);

                         GetContext().ChangeConflicts.ResolveAll(RefreshMode.KeepChanges);
                         GetContext().SubmitChanges();
                    }
               }
          }

          /// <summary>
          /// Delete an entry from the DB
          /// </summary>
          /// <param name="entry"></param>
          private void DeleteEntry(VehicleEntry entry)
          {
               //vehicleContext.ExecuteCommand("delete from vehicleentry where entryid={0}", entry.EntryId);
          }

          /// <summary>
          /// Get the context for this database
          /// </summary>
          /// <returns></returns>
          private VehicleDatabaseContext GetContext()
          {
               return vehicleContext;
          }

          private void LogChangeConflict(ChangeConflictException e)
          {
               Trace.TraceError($"VehicleDB:SubmitChange ChangeConflictException: {e.Message}");
               foreach (ObjectChangeConflict occ in GetContext().ChangeConflicts) {
                    TraceEx.PrintLog($"Conflict Object Type={nameof(occ.Object)} ToString={occ.Object.ToString()} isDeleted={occ.IsDeleted} isResolved={occ.IsResolved}");
                    foreach (MemberChangeConflict mcc in occ.MemberConflicts) {
                         TraceEx.PrintLog($"  MemberChangeConflict name={mcc.Member.Name} curr={mcc.CurrentValue.ToString()} orig={mcc.OriginalValue.ToString()} db={mcc.DatabaseValue.ToString()}");
                    }
               }
          }

          #endregion Methods
     }

     /// <summary>
     /// DataContext for vehicle database
     /// </summary>
     public class VehicleDatabaseContext : DataContext
     {
          #region Fields

          public Table<VehicleEntry> VehicleEntries;

          #endregion Fields

          #region Constructors

          public VehicleDatabaseContext(IDbConnection connection) : base(connection)
          {
          }

          #endregion Constructors
     }
}