using ReportApp.Model;
using ReportApp.Utility;
using ReportApp.ViewModel;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Linq;
using System.Data.SQLite;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace ReportApp.Data
{
     /// <summary>
     /// DataContext for database portion of application
     /// </summary>
     public class NetboxDatabaseContext : DataContext
     {
          public Table<Person> People;

          public Table<AccessEntry> AccessEntries;
          public Table<AccessReprocess> AccessReprocessEntries;
          public Table<ShiftEntry> ShiftEntries;

          public Table<RollCall> RollCalls;

          public Table<Vehicle> Vehicles;

          public Table<Report> Reports;
          public Table<Parameter> Parameters;

          public NetboxDatabaseContext(IDbConnection connection) : base(connection)
          {
          }
     }

     /// <summary>
     /// Description of Database.
     /// </summary>
     public sealed class NetboxDatabase : Database
     {
          private NetboxDatabaseContext netboxContext;
          private static NetboxDatabase _instance;

          private NetboxDatabase(string _FileName, bool readOnly = false)
          {
               var FileName = _FileName;
               string connectionString = String.Format("Data Source ={0};Version=3;PRAGMA cache_size = -10000", FileName);
               if (readOnly) {
                    //connectionString += "; Read Only=True";
               }

               base.Connect(connectionString);

               InitializeContext();

               CreateTables();
          }

          //only use for testing
          public static NetboxDatabase CreateTestingInstance(string _FileName)
          {
               return new NetboxDatabase(_FileName);
          }

          /// <summary>
          /// Get db instance to be used for reads only
          /// - will fail if used for write operations
          /// </summary>
          /// <returns></returns>
          public static NetboxDatabase GetReadOnlyInstance()
          {
               var newDb = new NetboxDatabase(PathSettings.Default.DatabasePath, readOnly: true);
               newDb.DisableObjectTracking();
               return newDb;
          }

          public static NetboxDatabase GetWriteInstance()
          {
               var newDb = new NetboxDatabase(PathSettings.Default.DatabasePath, readOnly: false);
               return newDb;
          }

          private void InitializeContext()
          {
               netboxContext = new NetboxDatabaseContext(connection);
               context = netboxContext;
          }

          private static bool tablesCreated = false;

          /// <summary>
          /// Creates all tables for application
          /// </summary>
          public override void CreateTables()
          {
               //if tables already created once, don't need to do it again
               if (tablesCreated) return;

               TableDefinition PersonDef = new TableDefinition("person");
               PersonDef.Add("firstname", "text");
               PersonDef.Add("lastname", "text");
               PersonDef.Add("personid", "text primary key");
               PersonDef.Add("company", "text");
               PersonDef.Add("deleted", "integer");
               PersonDef.Add("isnetbox", "integer");
               PersonDef.Add("fobnumber", "integer");
               PersonDef.Add("fobcredential", "text");
               PersonDef.Add("pinnumber", "integer");
               PersonDef.Add("activationdate", "text");
               PersonDef.Add("expirationdate", "text");

               PersonDef.Add("orientationnumber", "integer");
               PersonDef.Add("orientationdate", "text");
               PersonDef.Add("orientationlevel", "text");
               PersonDef.Add("orientationtestedby", "text");

               PersonDef.Add("lastmodified", "text");
               PersonDef.Add("OldCompcontact", "text");
               PersonDef.Add("vehiclesactivated", "int");

               PersonDef.Add("employeecategory", "text");
               PersonDef.Add("vehiclereader", "integer");
               PersonDef.Add("accesslevel", "text");
               
               CreateTable(PersonDef);

               TableDefinition AccessDef = new TableDefinition("accessentry");
               AccessDef.Add("accessentryid", "integer primary key");
               AccessDef.Add("personid", "text");
               AccessDef.Add("reader", "text");
               AccessDef.Add("dttm", "text");
               AccessDef.Add("type", "int");
               AccessDef.Add("reason", "int");
               AccessDef.Add("readerkey", "int");
               AccessDef.Add("portalkey", "int");
               AccessDef.Add("ShiftEntryProcessed", "int");
               CreateTable(AccessDef);

               TableDefinition AccessCacheDef = new TableDefinition("accessentrycache");
               AccessCacheDef.Add("accessentryid", "integer primary key");
               AccessCacheDef.Add("personid", "text");
               AccessCacheDef.Add("reader", "text");
               AccessCacheDef.Add("dttm", "text");
               AccessCacheDef.Add("type", "int");
               AccessCacheDef.Add("reason", "int");
               AccessCacheDef.Add("readerkey", "int");
               AccessCacheDef.Add("portalkey", "int");
               AccessCacheDef.Add("ShiftEntryProcessed", "int");
               CreateTable(AccessCacheDef);

               TableDefinition ReprocessDef = new TableDefinition("accessreprocess");
               ReprocessDef.Add("accessentryid", "integer primary key");
               CreateTable(ReprocessDef);

               TableDefinition ShiftEntryDef = new TableDefinition("shiftentry");
               ShiftEntryDef.Add("shiftentryid", "int primary key");
               ShiftEntryDef.Add("personid", "text");
               ShiftEntryDef.Add("intime", "text");
               ShiftEntryDef.Add("outtime", "text");
               ShiftEntryDef.Add("inlogid", "int");
               ShiftEntryDef.Add("outlogid", "int");
               ShiftEntryDef.Add("hoursworked", "real");
               ShiftEntryDef.Add("shiftentryprocessed", "int");
               CreateTable(ShiftEntryDef);

               TableDefinition ReportDef = new TableDefinition("report");
               ReportDef.Add("reportid", "int primary key");
               ReportDef.Add("name", "text");
               ReportDef.Add("query", "text");
               ReportDef.Add("filename", "text");
               ReportDef.Add("dateoffset", "int");
               ReportDef.Add("showtime", "int");
               ReportDef.Add("showdatepicker", "int");
               CreateTable(ReportDef);

               TableDefinition ParamDef = new TableDefinition("parameter");
               ParamDef.Add("parameterid", "int primary key");
               ParamDef.Add("name", "text");
               ParamDef.Add("value", "text");
               CreateTable(ParamDef);

               //TableDefinition RollcallDef = new TableDefinition("rollcall");
               RollcallDef.Add("rollcallid", "int primary key");
               RollcallDef.Add("personid", "text");
               RollcallDef.Add("dttm", "text");
               RollcallDef.Add("reader", "text");
               RollcallDef.Add("timein", "real");
               CreateTable(RollcallDef);

               TableDefinition VehicleDef = new TableDefinition("vehicle");
               VehicleDef.Add("vehicleid", "int primary key");
               VehicleDef.Add("personid", "text");
               VehicleDef.Add("color", "text");
               VehicleDef.Add("make", "text");
               VehicleDef.Add("model", "text");
               VehicleDef.Add("licnum", "text");
               CreateTable(VehicleDef);

               tablesCreated = true;
          }

          internal void DeleteShiftEntryTestData(string idToDelete)
          {
               var query = GetContext().ShiftEntries.Where(x => x.PersonId == idToDelete);

               if (query.Any()) {
                    foreach (var v in query) {
                         GetContext().ShiftEntries.DeleteOnSubmit(v);
                    }
                    SubmitChanges();
               }
          }

          internal void DeleteAccessEntryTestData()
          {
               var query = GetContext().AccessEntries.Where(x => x.ReaderKey == ReaderKeyEnum.TestIn || x.ReaderKey == ReaderKeyEnum.TestOut);

               if (query.Any()) {
                    foreach (var v in query) {
                         GetContext().AccessEntries.DeleteOnSubmit(v);
                    }
                    SubmitChanges();
               }
          }

          private TableDefinition RollcallDef = new TableDefinition("rollcall");

          public void ClearTablePerson()
          {
               netboxContext.ExecuteCommand("Delete From Person");
          }

          public void ClearTableVehicle()
          {
               netboxContext.ExecuteCommand("Delete From Vehicle");
          }

          public void ClearTableRollCall()
          {
               netboxContext.ExecuteCommand("Delete From Rollcall");
          }

          public void DeleteTableRollCall()
          {
               netboxContext.ExecuteCommand("DROP Table Rollcall");
          }

          public void Vacuum()
          {
               netboxContext.ExecuteCommand("Vacuum");
          }

          public void Analyze()
          {
               netboxContext.ExecuteCommand("ANALYZE");
          }

          public void CreateTableRollCall()
          {
               CreateTable(RollcallDef);
          }

          public void MarkDeletedPerson(List<string> deletedList)
          {
               string personId = string.Empty;
               try {
                    foreach (var id in deletedList) {
                         var query = netboxContext.People.Where(x => x.PersonId == id);
                         if (query != null && query.Any()) { //null reference?? - how??
                              var first = query.ToList().First(); //must tolist to get around linqtosql bug

                              //credentials are deleted by netbox on delete
                              first.FobNumber = 0;
                              first.PinNumber = 0;
                              first.FobCredential = string.Empty;

                              first.Deleted = true;
                         } else {
                              TraceEx.PrintLog($"Problem in MarkDeletedPerson - could not find {id}");
                         }
                    }
                    SubmitChanges();
               }
               catch (NullReferenceException e) {
                    //unsure of what is causing this null reference exception - so handle it here and try to find what happened
                    Trace.TraceError($"Null Reference exception: NetboxDatabase: MarkDeletedPerson {e.Message}");
               }
          }

          public override void SubmitChanges()
          {
               lock (DatabaseLock) {
                    try {
                         netboxContext.SubmitChanges(failureMode: ConflictMode.FailOnFirstConflict);
                    }
                    catch (ChangeConflictException e) {
                         LogChangeConflict(e);

                         netboxContext.ChangeConflicts.ResolveAll(RefreshMode.KeepChanges);
                         netboxContext.SubmitChanges();
                    }
               }
          }

          private void LogChangeConflict(ChangeConflictException e)
          {
               Trace.TraceError($"NetboxDB:SubmitChange ChangeConflictException: {e.Message}");
               foreach (ObjectChangeConflict occ in netboxContext.ChangeConflicts) {
                    TraceEx.PrintLog($"Conflict Object Type={nameof(occ.Object)} ToString={occ.Object.ToString()} isDeleted={occ.IsDeleted} isResolved={occ.IsResolved}");
                    foreach (MemberChangeConflict mcc in occ.MemberConflicts) {
                         TraceEx.PrintLog($"  MemberChangeConflict name={mcc.Member.Name} curr={mcc.CurrentValue.ToString()} orig={mcc.OriginalValue.ToString()} db={mcc.DatabaseValue.ToString()}");
                    }
               }
          }

          public NetboxDatabaseContext GetContext()
          {
               return netboxContext;
          }

          public object DatabaseLock
          {
               get
               {
                    if (_databaseLock == null) {
                         _databaseLock = new object();
                    }
                    return _databaseLock;
               }
          }

          private static object _databaseLock;

          /// <summary>
          /// Updates person in database
          ///  - will add person if it doesn't exist in database
          /// </summary>
          /// <param name="p">person to update</param>
          public void UpdatePerson(Person p)
          {
               Trace.Assert(!String.IsNullOrEmpty(p.PersonId), "UpdatePerson: personid must not be null/empty");

               var task = new Task(() => {
                    TraceEx.PrintLog("Calling UpdatePerson from in task");
                    UpdatePersonInternal(p);
               });

               GlobalScheduler.DBQueue.QueueTaskEnd("DB:UpdatePerson", task);
          }

          private void UpdatePersonInternal(Person p)
          {
               var query = from dbentry in netboxContext.People
                           where p.PersonId == dbentry.PersonId
                           select dbentry;

               //check if query null
               if (query is null) {
                    Trace.TraceError("UpdatePersonInternal: Query is null");
                    throw new NullReferenceException("Query object is null");
               } else if (p is null) {
                    Trace.TraceError("UpdatePersonInternal: Query is null");
                    throw new NullReferenceException("Person object is null");
               } else {
                    //null ref can be thrown here - yet isn't triggered by above checks
                    int tryCount = 2;
                    while (tryCount > 0) {
                         try {
                              if (query.Any()) {
                                   Person dbEntry = query.ToList().First();
                                   dbEntry.CopyFromOther(p);
                              } else {
                                   netboxContext.People.InsertOnSubmit(p);
                              }
                              goto next;
                         }
                         catch (NullReferenceException) {
                              Trace.TraceError($"UpdatePersonInternal NullReference {p}  Trying again");
                              tryCount--;
                         }
                    }

                    throw new NullReferenceException("UpdatePersonInteral: Failed after multiple attempts");
               }

          next:
               //change vehicle id from max
               var vehicleId = getNextVehicleId();

               //delete existing vehicles
               var vehicles = netboxContext.Vehicles.Where(x => x.PersonId == p.PersonId);
               netboxContext.Vehicles.DeleteAllOnSubmit(vehicles);

               //re-add
               foreach (var v in p.VehicleList) {
                    v.VehicleId = vehicleId++;
                    v.PersonId = p.PersonId;

                    //create copy so it is recognized as separate object by linq
                    netboxContext.Vehicles.InsertOnSubmit(v.Copy() as Vehicle);
               }

               SubmitChanges();
          }

          /// <summary>
          /// Update From List
          /// - updates shift entry table in database from list
          /// </summary>
          /// <param name="list">list containing new/updated shift entry data</param>
          /// <returns>int of number of updates</returns>
          public int UpdateFromList(List<ShiftEntry> list)
          {
               Benchmarker.Start("UpdateFromList(ShiftEntry)");
               DBLoadStatus.WriteLine("DB:  Copying shiftlist to database");
               var onlyGetUpdatedList = from x in list
                                        where x.IsChanged
                                        select x;
               int updatedCount = 0;

               var messageHandler = DataRepository.ShiftEntryMessageHandler;

               foreach (var elem in onlyGetUpdatedList) {
                    var query = from dbentry in netboxContext.ShiftEntries
                                where elem.ShiftEntryId == dbentry.ShiftEntryId
                                select dbentry;

                    //for debugging / exceptions
                    SetLastCommandText<ShiftEntry>(query);

#if DEBUG
			//SetDebugText();
#endif

                    if (query.Any()) {
                         //edit
                         //have to use to list or query generates top command
                         //which is incompatable with sqlite
                         var updateElem = query.ToList().First();
                         updateElem.CopyFromOther(elem);
                         //netboxContext.SubmitChanges();
                         updatedCount++;

                         //handle event
                         CollectionChangedEventArgs e = new CollectionChangedEventArgs() {
                              ChangeType = CollectionChangeType.Edit,
                         };
                         DispatcherHelper.GetDispatcher().BeginInvoke(new Action(() => {
                              var newElem = new ShiftEntryViewModel(elem);
                              e.ChangedValue = newElem.Copy();
                              messageHandler.OnCollectionChanged(e);
                         }), System.Windows.Threading.DispatcherPriority.Render);
                    } else {
                         //add
                         netboxContext.ShiftEntries.InsertOnSubmit(elem);
                         //netboxContext.SubmitChanges();
                         updatedCount++;

                         //handle event
                         CollectionChangedEventArgs e = new CollectionChangedEventArgs() {
                              ChangedValue = new ShiftEntryViewModel(elem),
                              ChangeType = CollectionChangeType.Add
                         };
                         DispatcherHelper.GetDispatcher().BeginInvoke(new Action(() => {
                              messageHandler.OnCollectionChanged(e);
                         }), System.Windows.Threading.DispatcherPriority.Render);
                    }
               }
               SubmitChanges();
               DBLoadStatus.WriteLine("DB:  Done: Copying shiftlist to database. Updated: " + updatedCount);
               Benchmarker.Stop("UpdateFromList(ShiftEntry)");
               return updatedCount;
          }

          /// <summary>
          /// Edit an entry
          /// </summary>
          /// <param name="entry"></param>
          public void EditEntry(ShiftEntry entry)
          {
               var query = netboxContext.ShiftEntries.Where(e => entry.ShiftEntryId == e.ShiftEntryId);

               try {
                    if (query.Any()) {
                         var first = query.Single();
                         first.CopyFromOther(entry);
                         TraceEx.PrintLog($"Changing Shift Entry - {entry}");
                         SubmitChanges();
                    } else {
                         Trace.TraceWarning("NetboxDatabase: EditEntry (shiftentry): Cound not find entry");
                    }
               }
               catch (NullReferenceException) {
                    MessageBox.Show("There was an issue writing to the database.  Please double check that data was changed properly", "Warning");
                    Trace.TraceWarning("NetboxDatabase::EditEntry(shift) NullReference Exception e: " + entry.ShiftEntryId);
                    TraceEx.PrintLog(Environment.StackTrace);
               }
               catch (SQLiteException) {
                    MessageBox.Show("There was a issue writing to the database.  It is recommended you close and reopen the form.  Your data may not have been written.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    Trace.TraceWarning($"NetboxDatabase::EditEntry (shift) SQLiteException on Entry: {entry.ShiftEntryId}");
                    TraceEx.PrintLog(Environment.StackTrace);
               }
          }

          /// <summary>
          /// Copies list to actual database table
          /// </summary>
          public void CopyListToTable(Dictionary<long, AccessEntry> dict)
          {
               DBLoadStatus.WriteLine("DB:  Copying accesslist to database");
               if (dict == null || dict.Count == 0) return;

               var messageHandler = DataRepository.AccessLogMessageHandler;

               foreach (var elem in dict.Values) {
                    var findExistingQuery = netboxContext.AccessEntries.Where(x => elem.LogId == x.LogId);
                    if (findExistingQuery.Any() == false) {
                         FilterInvalidPassback(dict, elem);
                         try {
                              netboxContext.AccessEntries.InsertOnSubmit(elem.Copy() as AccessEntry);
                         }
                         catch (InvalidOperationException e) {
                              Trace.TraceError($"NetboxDB: CopyListToTable AccessEntry InvalidOperationException {e.Message} on elem={elem}");
                              throw;
                         }

                         TraceEx.PrintLog("NetboxDB: Sending access message");

                         DispatcherHelper.GetDispatcher().BeginInvoke(new Action(() => {
                              var args = new CollectionChangedEventArgs();
                              args.ChangedValue = elem;
                              messageHandler.OnCollectionChanged(args);
                         }), System.Windows.Threading.DispatcherPriority.Render);
                    } else {
                         //if shift entry has been marked, then change it
                         if (elem.ShiftEntryProcessed.HasValue && elem.ShiftEntryProcessed.Value == true) {
                              var first = findExistingQuery.ToList().First();
                              first.ShiftEntryProcessed = true;
                         }
                    }
               }
               SubmitChanges();
               DBLoadStatus.WriteLine("DB:  Done: Copying accesslist to database");
          }

          private void FilterInvalidPassback(Dictionary<long, AccessEntry> dict, AccessEntry elem)
          {
               //filter out invalid passback violations
               if (elem.Reason == ReasonCode.AntiPassbackViolation) {
                    try {  // catch any exception - this isn't critical so we don't want it to cause the program to crash
                           //find previous entry
                         AccessEntry foundEntry = null;

                         //first look in input dict
                         var previousDictQuery = from x in dict.Values
                                                 where x.PersonId == elem.PersonId && x.LogId != elem.LogId && x.DtTm < elem.DtTm
                                                 orderby x.LogId descending
                                                 select x;
                         if (previousDictQuery.Any()) {
                              foundEntry = previousDictQuery.First();
                         }

                         if (foundEntry == null) {
                              //look in database next
                              var previousQuery = (from x in netboxContext.AccessEntries
                                                   where x.PersonId == elem.PersonId && x.LogId != elem.LogId && x.DtTm < elem.DtTm
                                                   select x);
                              if (previousQuery.Any()) {
                                   var max = previousQuery.Max(x => x.LogId);
                                   foundEntry = netboxContext.AccessEntries.Where(x => x.LogId == max).ToList().First();
                              }
                         }
                         if (foundEntry != null) {
                              var timespan = (elem.DtTm - foundEntry.DtTm);
                              if ((foundEntry.Reader.Contains("IN") && elem.Reader.Contains("OUT"))
                                   || (foundEntry.Reader.Contains("OUT") && elem.Reader.Contains("IN"))) {
                                   elem.Reason = ReasonCode.InvalidPassback;
                              }
                              //check if it is within one hour
                              else if (timespan.TotalHours < 1) {
                                   elem.Reason = ReasonCode.InvalidPassback;
                              }
                         }
                    }
                    catch (Exception ex) {
                         Trace.TraceError($"NetboxDatabase::CopyListToTable exception {ex.Message}");
                    }
               }
          }

          /// <summary>
          /// Copies list of people to actual database table
          /// </summary>
          public void CopyListToTable(List<Person> list)
          {
               DBLoadStatus.WriteLine("DB:  Copying peopleList to database");
               if (list == null) return;

               int vehid = 0;
               foreach (var p in list) {
                    netboxContext.People.InsertOnSubmit(p);

                    //add vehicle info
                    foreach (var veh in p.VehicleList) {
                         veh.VehicleId = vehid++;
                         veh.PersonId = p.PersonId;
                         netboxContext.Vehicles.InsertOnSubmit(veh);
                    }
               }
               try {
                    SubmitChanges();
               }
               catch (DuplicateKeyException e) {
                    Trace.TraceError("DuplicateKeyException!");
                    list.Remove((Person)e.Object);
                    InitializeContext();
                    CopyListToTable(list);
               }
               DBLoadStatus.WriteLine("DB:  Done: Copying peopleList to database");
          }

          /// <summary>
          /// Uses merge and add list and submits changes to database
          /// </summary>
          /// <param name="mergeList">List of people to merge</param>
          public void MergeListToTable(List<Person> mergeList)
          {
               DBLoadStatus.WriteLine("DB:  Merging peopleList to database.");
               Benchmarker.Start("DB.MergeListToTable(List<Person>)");
               if (mergeList == null) return;

               //get largest vehicle id
               Int32 largestVehicleId = 0;
               if (netboxContext.Vehicles.Any()) {
                    largestVehicleId = getNextVehicleId();
               }
               int vehicleId = largestVehicleId;

               var debugVehicleList = new List<Vehicle>();

               //processMergeList
               foreach (var p in mergeList) {
                    var doesPersonExist = from dbperson in netboxContext.People
                                          where p.PersonId == dbperson.PersonId
                                          select dbperson;
                    if (doesPersonExist.Any()) {
                         var tlist = doesPersonExist.ToList();

                         var foundPerson = tlist.First();
                         foundPerson.CopyFromOther(p);
                    } else {
                         netboxContext.People.InsertOnSubmit(p);
                    }

                    //delete old vehicles
                    var vehQuery = from v in netboxContext.Vehicles
                                   where v.PersonId == p.PersonId
                                   select v;
                    foreach (var v in vehQuery) {
                         netboxContext.Vehicles.DeleteOnSubmit(v);
                    }

                    //now add new ones
                    foreach (var v in p.VehicleList) {
                         v.PersonId = p.PersonId;
                         v.VehicleId = vehicleId++;
                         netboxContext.Vehicles.InsertOnSubmit(v);
                         debugVehicleList.Add(v);
                    }
               }

               try {
                    SubmitChanges();
               }
               catch (SQLiteException e) {
                    Trace.TraceError("MergeListToTable: sqliteexception thrown!");
                    TraceEx.PrintLog($"SQLITE Exception: '{e.Message}'");
               }
               DBLoadStatus.WriteLine("DB:  Done: Merging peopleList to database");
               Benchmarker.Stop("DB.MergeListToTable(List<Person>)");
          }

          private int getNextVehicleId()
          {
               if (netboxContext.Vehicles.Any() == false) {
                    return 0;
               } else {
                    return netboxContext.Vehicles.Max(x => x.VehicleId) + 1;
               }
          }

          public void MergeListToTable(Dictionary<string, RollCall> inputDict)
          {
               if (inputDict == null) return;

               //get list from dict
               var dbRollCallList = netboxContext.RollCalls.ToList();
               var dbRollCallDict = new Dictionary<string, RollCall>(dbRollCallList.Count);

               //put list vals in temp dict
               foreach (var rc in dbRollCallList) {
                    dbRollCallDict[rc.PersonId] = rc;
               }

               //merge input dict
               foreach (var kvp in inputDict) {
                    //if current has no key, we need to add
                    if (dbRollCallDict.ContainsKey(kvp.Key) == false) {
                         netboxContext.RollCalls.InsertOnSubmit(kvp.Value);

                         //remove to ensure we don't add again
                         dbRollCallDict.Remove(kvp.Key);
                    }
               }

               //now go through existing dict to see if any need to be removed
               foreach (var kvp in dbRollCallDict) {
                    if (inputDict.ContainsKey(kvp.Key) == false) {
                         netboxContext.RollCalls.DeleteOnSubmit(kvp.Value);
                    }
               }

               SubmitChanges();
          }

          /// <summary>
          /// Copies list of access reprocess to actual database table
          /// </summary>
          public void CopyListToTable(List<AccessReprocess> list)
          {
               DBLoadStatus.WriteLine("DB:  Copying accessreprocess to database");
               if (list == null) return;

               //just to be sure no duplicates, throw in dict
               var dict = new Dictionary<long, AccessReprocess>();
               foreach (var v in list) {
                    if (dict.ContainsKey(v.LogId)) {
                         Trace.TraceError("Warning: AccessReprocess duplicate key");
                    } else {
                         dict[v.LogId] = v;
                    }
               }

               bool changesMade = false;

               foreach (var e in dict.Values) {
                    TraceEx.PrintLog($"CopyListToTable(AccessReprocess) LogId={e.LogId}");
                    if (netboxContext.AccessReprocessEntries.Any(x => x.LogId == e.LogId) == false) {
                         try {
                              netboxContext.AccessReprocessEntries.InsertOnSubmit(e);
                              changesMade = true;
                         }
                         catch (InvalidOperationException ex) {
                              Trace.TraceError("Invalid operation exception: " + ex.Message);
                         }
                    }
               }
               if (changesMade) {
                    SubmitChanges();
               }
               DBLoadStatus.WriteLine("DB:  Done: Copying access reprocess to database");
          }

          public List<Person> LoadTablePerson()
          {
               var peopleQuery = netboxContext.People;
               var peopleList = peopleQuery.ToList();

               //add vehicle information
               foreach (var veh in netboxContext.Vehicles) {
                    var findPersonQuery = peopleList.Where(x => x.PersonId == veh.PersonId);

                    if (findPersonQuery.Any()) {
                         findPersonQuery.First().VehicleList.Add(veh);
                    } else {
                         Trace.TraceWarning($"db.LoadTablePerson - could not find {veh.PersonId} for vehicle {veh}");
                    }
               }
               return peopleList;
          }

          /// <summary>
          /// Load AccessEntry data from database
          /// </summary>
          /// <param name="startTime">DateTime to start from</param>
          /// <returns>List of AccessEntry</returns>
          public List<AccessEntry> LoadTableAccessEntry(DateTime startTime)
          {
               DBLoadStatus.WriteLine("DB:  Loading AccessList from database");
               var query = from x in netboxContext.AccessEntries
                           where x.DtTm > startTime
                           select x;
               DBLoadStatus.WriteLine("DB:  Done: Loading AccessList from database");
               return query.ToList();
          }

          /// <summary>
          /// Load AccessEntry data from database - only loads those that haven't been processed yet
          /// </summary>
          /// <param name="startTime">DateTime to start from</param>
          /// <returns>List of AccessEntry</returns>
          public List<AccessEntry> LoadTableAccessEntryUnprocessed(DateTime startTime)
          {
               List<AccessEntry> list = new List<AccessEntry>();
               try {
                    //directly execute query since linq otherwise creates slow query
                    var anyQuery2 = context.ExecuteQuery<long>(@"
                         SELECT    accessentryid
                         FROM      AccessEntry
                         WHERE     shiftentryprocessed = 0");

                    if (anyQuery2.Any()) {
                         var query = from x in netboxContext.AccessEntries
                                     where x.ShiftEntryProcessed == false //&& x.DtTm > startTime
                                     select x;

                         list = query.ToList();
                    }
                    return list;
               }
               catch (NullReferenceException e) {
                    Trace.TraceError($"LoadTableAccessEntryUnprocessed NullRef Exception {e.Message}");
                    TraceEx.PrintLog("LoadTableAccessEntryUnProcessed: returning empty list");
                    return list;
               }
          }

          /// <summary>
          /// Loads ShiftEntry data from database
          /// </summary>
          /// <returns></returns>
          public List<ShiftEntry> LoadTableShiftEntry(DateTime afterDate)
          {
               DBLoadStatus.WriteLine("DB:  Loading EntryList from database");

               int tryCount = 2;

               while (tryCount > 0) {
                    try {
                         var query = from x in netboxContext.ShiftEntries
                                     where x.OutLogId == 0 && x.InTime > afterDate
                                     select x;
                         DBLoadStatus.WriteLine("DB:  Done: Loading EntryList from database");

                         if (query == null) {
                              return new List<ShiftEntry>();
                         } else {
                              return query.ToList();
                         }
                    }
                    catch (NullReferenceException e) {
                         Trace.TraceError("LoadTableShiftEntry: Null reference exception - trying again");
                         tryCount--;
                    }
               }

               throw new NullReferenceException("LoadTableShiftEntry: Tried multiple times to load, but still failed");
          }

          /// <summary>
          /// Search Access log table and find highest id
          /// </summary>
          /// <returns>highest id</returns>
          public long GetLastAccessId()
          {
               Int64 max = netboxContext.AccessEntries.Max(x => x.LogId);
               return max;
          }

          /// <summary>
          /// Get list of entries to reprocess from database
          /// </summary>
          /// <returns></returns>
          public List<AccessReprocess> GetReprocessList()
          {
               var query = (netboxContext.AccessReprocessEntries.Select(x => x));
               List<AccessReprocess> return_list = null;

               try {
                    if (query != null && query.Any()) {
                         return_list = query.ToList();

                         TraceEx.PrintLog("GetReprocessList:: there are entries here");

                         //delete access reprocess entries after loading them
                         netboxContext.AccessReprocessEntries.DeleteAllOnSubmit(query);
                         SubmitChanges();
                    } else {
                         return_list = null;
                    }
               }
               catch (NullReferenceException e) {
                    Trace.TraceError($"GetReprocessList:: Null reference exception caught! {e.Message}");
               }
               return return_list;
          }
     }
}