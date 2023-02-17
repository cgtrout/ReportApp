using API_Interface;
using ReportApp.Data;
using ReportApp.Model;
using ReportApp.Utility;
using ReportApp.ViewModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Xml.Linq;

namespace ReportApp.API
{
     /// <summary>
     /// API loader handles multithreaded situation of loading database and updating from API so that they occur in background
     /// </summary>
     public class API_Loader
     {
          #region Fields

          //used to track last key used
          private static long lastKey = -1;

          private static int lastUpdateCount = 0;

          #endregion Fields

          #region Constructors

          public API_Loader()
          {
          }

          #endregion Constructors

          #region Methods

          /// <summary>
          /// Updates person data from API
          /// </summary>
          /// <param name="databaseFilename">string path to the database file</param>
          /// <param name="firstLoad">Set to true if first load</param>
          /// <param name="deleteEmptyCredential">Should we delete any people with empty credentials</param>
          /// <returns>bool true on completion</returns>
          public static async Task<bool> UpdatePerson(string databaseFilename, bool firstLoad, bool deleteEmptyCredential)
          {
               var dbTaskQueue = GlobalScheduler.DBQueue;
               var personTaskQueue = GlobalScheduler.PersonUpdateQueue;
               var apiTaskQueue = GlobalScheduler.APIUpdateQueue;

               bool cancel = false;

               Benchmarker.Start("UpdatePerson() All");

               if (firstLoad) {
                    dbTaskQueue.QueueTaskEnd("VACUUM", new Task(() => {
                         using (var db = NetboxDatabase.GetWriteInstance()) {
                              db.Vacuum();
                              db.Analyze();
                         }
                    }));
               }

               //
               //TASK APILoadPeopleList
               // - loads person list from API
               // - starts up task to copy to db as well
               var APILoadPeopleList = new Task<Dictionary<string, Person>>(() => {
                    return apiLoadPeopleList(firstLoad, dbTaskQueue, apiTaskQueue, ref cancel, deleteEmptyCredential);
               });
               personTaskQueue.QueueTaskEnd("APILoadPeopleList", APILoadPeopleList);

               await Task.WhenAll(APILoadPeopleList);
               Benchmarker.Stop("UpdatePerson() All");
               return cancel;
          }

          /// <summary>
          /// Update the Access information from the API
          ///   - creates tasks and runs them as "SerialTaskQueue"s
          /// </summary>
          /// <param name="cancel">Set to true if the rest of the loading should be cancelled (to recover from error)</param>
          /// <param name="firstLoad">Set to true if it is the first load</param>
          /// <returns></returns>
          public async Task UpdateAccess(bool cancel = false, bool firstLoad = false)
          {
               Benchmarker.Start("UpdateAccess() All");
               var api_Interaction = new API_Interaction();

               var dbTaskQueue = GlobalScheduler.DBQueue;
               var accessTaskQueue = GlobalScheduler.AccessUpdateQueue;
               var apiTaskQueue = GlobalScheduler.APIUpdateQueue;

               var now = DateTime.Now;
               DBLoadStatus.IsLoadingAccess = true;

               //
               //TASK checkCache
               // - prepares DataRepository memory cache of access log
               var checkCache = new Task(() => {
                    //if 25 hours has passed then clear access cache
                    var timeSpan = DateTime.Now - DataRepository.CacheClearTime;
                    if (timeSpan.Hours > 25) {
                         DataRepository.ClearAccessCache();
                    }

                    if (DataRepository.AccessCache.Count == 0) {
                         //fill up cache if it is empty
                         List<AccessEntry> dbAccessList = NetboxDatabase.GetReadOnlyInstance().LoadTableAccessEntry(DateTime.Now.AddHours(-25));

                         TraceEx.PrintLog("Access count = 0  -> Loading cache from DB ");

                         DataRepository.CopyAccessEntriesToCache(dbAccessList);
                    }
               });
               dbTaskQueue.QueueTaskEnd("checkCache", checkCache);

               //
               //TASK obtainAccessIdFromDB
               // - gets last access id from DB
               var obtainAccessIdFromDB = new Task<long>(() => {
                    return DataRepository.MaxAccessId;
               });
               dbTaskQueue.QueueTaskEnd("obtainAccessIdFromDB", obtainAccessIdFromDB);

               //
               //TASK obtainReprocessList
               // - gets reprocess list (list of entries that need to be reloaded)
               var obtainReprocessList = new Task<List<AccessReprocess>>(() => {
                    using (var db = NetboxDatabase.GetReadOnlyInstance()) {
                         return db.GetReprocessList();
                    }
               });
               dbTaskQueue.QueueTaskEnd("obtainReprocessList", obtainReprocessList);

               long lastId = 0;

               //
               //TASK APILoadAccessList
               // - loads access list from API
               // - returns tuple
               // - item1 is dict of access entries
               // - item2 is list of AccessReprocess (entries that need to be reprocessed)
               var APILoadAccessList = new Task<Tuple<Dictionary<long, AccessEntry>, List<AccessReprocess>>>(() => {
                    var accessIdFromDBResult = obtainAccessIdFromDB.Result;
                    var reprocessListResult = obtainReprocessList.Result;
                    return apiLoadAccessList(cancel, firstLoad, dbTaskQueue, apiTaskQueue, accessIdFromDBResult, reprocessListResult, ref lastId);
               });
               accessTaskQueue.QueueTaskEnd("APILoadAccessList", APILoadAccessList);

               //
               //TASK DBCopyAccessList
               // - copies access list and reprocess list to db
               dbTaskQueue.QueueTaskEnd("DBCopyAccessList", new Task(() => {
                    var tuple = APILoadAccessList.Result;

                    if(tuple == null) {
                         NetworkTools.PingLog();
                         Trace.TraceError("API Access List null -- connection issue?");
                         return;
                    }

                    dbCopyAccessList(tuple.Item1);
                    dbFillReprocessList(tuple.Item2);
               }));

               //
               //TASK APIFishInvalidEntries
               // - attempts to find invalid entries from the API
               var APIFishInvalidEntries = new Task<Dictionary<long, AccessEntry>>(() => {
                    Task.WaitAll(APILoadAccessList);
                    return apiFishInvalidEntries(lastId, apiTaskQueue);
               });
               //APIFishInvalidEntries.Start();
               accessTaskQueue.QueueTaskEnd("APIFishInvalidEntries", APIFishInvalidEntries);

               //
               //TASK obtainUnprocessedAccessInfoFromDB
               // - obtains any access entries from db that need to be processed
               var obtainUnprocessedAccessInfoFromDB = new Task<List<AccessEntry>>(() => {
                    using (var db = NetboxDatabase.GetReadOnlyInstance()) {
                         return db.LoadTableAccessEntryUnprocessed(now.Date.AddHours(-24));
                    }
               });
               dbTaskQueue.QueueTaskEnd("DBobtainUnprocessedAccessInfoFromDB", obtainUnprocessedAccessInfoFromDB);

               await Task.WhenAll(obtainUnprocessedAccessInfoFromDB, APILoadAccessList);

               //skip processing shift entry and roll call if we don't need to
               bool shouldSkip = (obtainUnprocessedAccessInfoFromDB.Result != null && obtainUnprocessedAccessInfoFromDB.Result.Count == 0) && (APILoadAccessList.Result != null && APILoadAccessList.Result.Item1.Count == 0);

               if (shouldSkip == false || firstLoad == true) {
                    //
                    //TASK obtainShiftEntryInfoFromDB
                    // - obtains shift entries from db for later processing
                    //.02
                    var obtainShiftEntryInfoFromDB = new Task<List<ShiftEntry>>((() => {
                         return obtainShiftEntryInfoFromDb(cancel, now.Date.AddHours(-24));
                    }));
                    dbTaskQueue.QueueTaskEnd("DBObtainShiftEntryFromDB", obtainShiftEntryInfoFromDB);

                    //
                    //TASK APIGenerateShiftList
                    // - runs as separate task so it will run in parallel with taskqueues
                    // - generates shift list from given accessEntries and shiftEntries lists
                    var APIGenerateShiftList = new Task<Tuple<List<ShiftEntry>, Dictionary<long, AccessEntry>>>(() => {
                         return apiGenerateShiftList(obtainUnprocessedAccessInfoFromDB, obtainShiftEntryInfoFromDB);
                    });
                    APIGenerateShiftList.Start();

                    //
                    //TASK ApiLoadRollCall
                    // - uses access list to generate and return roll call dict
                    var APILoadRollCall = new Task<Dictionary<string, RollCall>>(() => {
                         return apiLoadRollCall(DataRepository.AccessCache.Values);
                    });
                    //accessTaskQueue.QueueTaskEnd("APILoadRollCall", APILoadRollCall);
                    APILoadRollCall.Start();

                    //
                    //TASK DBCopyRollCallToDB
                    // - copies roll call dict to db
                    dbTaskQueue.QueueTaskEnd("DBCopyRollCallToDB", new Task(() => {
                         dbCopyRollCallToDb(APILoadRollCall);
                    }));

                    //
                    //TASK DBCopyShiftList
                    // - copies shift list to db
                    var DBCopyShiftList = new Task(() => {
                         var shiftList = APIGenerateShiftList.Result.Item1;
                         dbCopyShiftList(shiftList);
                    });
                    dbTaskQueue.QueueTaskEnd("DBCopyShiftList", DBCopyShiftList);

                    //
                    //TASK DBCopyProcessedAccessList
                    // - copies processed access to db
                    var DBCopyProcessedAccessList = new Task(() => {
                         using (var db = NetboxDatabase.GetWriteInstance()) {
                              db.CopyListToTable(APIGenerateShiftList.Result.Item2);
                         }
                    });
                    dbTaskQueue.QueueTaskEnd("DBCopyProcessedAccessList", DBCopyProcessedAccessList);
                    await Task.WhenAll(DBCopyShiftList);
               }

               //
               //TASK DBCopyFishList
               // - copies fish list (invalid entries) to db
               var DBCopyFishList = new Task(() => {
                    dbCopyFishList(APIFishInvalidEntries);
               });
               dbTaskQueue.QueueTaskEnd("DBCopyFishList", DBCopyFishList);

               await Task.WhenAll(DBCopyFishList, APIFishInvalidEntries);
               DBLoadStatus.IsLoadingAccess = false;
               Benchmarker.Stop("UpdateAccess() All");
          }

          private static Dictionary<long, AccessEntry> apiFishInvalidEntries(long lastId, SerialTaskQueue queue)
          {
               var accessDict = new Dictionary<long, AccessEntry>();

               //fish for expired entries
               //we do this since the GetAccessDataLog command will return invalid entries while the main
               //command won't
               if (lastId != 0) {
                    FishForInvalidEntries(accessDict, lastId, queue);
               }
               return accessDict;
          }

          /// <summary>
          ///
          /// </summary>
          /// <param name="obtainAccessInfoFromDB"></param>
          /// <param name="obtainEntryInfoFromDB"></param>
          /// <returns>Tuple(ShiftEntry list, accessentry dict of processed entries)</returns>
          private static Tuple<List<ShiftEntry>, Dictionary<long, AccessEntry>> apiGenerateShiftList(Task<List<AccessEntry>> obtainAccessInfoFromDB, Task<List<ShiftEntry>> obtainEntryInfoFromDB)
          {
               var accessInfo = obtainAccessInfoFromDB.Result;
               var entryInfo = obtainEntryInfoFromDB.Result;
               if (accessInfo == null && entryInfo == null) {
                    return null;
               }
               Benchmarker.Start("APIGenerateShiftList");
               var tuple = API_Interaction.CreateShiftList(accessInfo, entryInfo);
               List<ShiftEntry> shiftList = tuple.Item1;
               var processedList = tuple.Item2;

               Benchmarker.Stop("APIGenerateShiftList");
               return Tuple.Create(shiftList, processedList);
          }

          private static Tuple<Dictionary<long, AccessEntry>, List<AccessReprocess>> apiLoadAccessList(bool cancel, bool firstLoad, SerialTaskQueue dbTaskQueue, SerialTaskQueue apiTaskQueue, long accessId, List<AccessReprocess> reprocessList, ref long lastId)
          {
               if (cancel) {
                    return null;
               }
               //try catch here
               try {
                    Benchmarker.Start("APILoadAccessList");
                    DateTime beforeTime = DateTime.Now;
                    List<AccessReprocess> invalidEntries = new List<AccessReprocess>();

                    Dictionary<long, AccessEntry> accessDict = LoadAccessLog(accessId, apiTaskQueue, invalidEntries);

                    //set last id
                    if (accessDict != null && accessDict.Any()) {
                         lastId = accessDict.Keys.Max() + 1;
                    } else {
                         lastId = accessId + 1;
                    }

                    //return if nothing in dict (nothing returned from api)
                    if (accessDict == null) {
                         return null;
                    }

                    //set estimated time drift
                    if (accessDict.Any() && !firstLoad && accessDict.First().Value.LogId != lastId) {
                         //get time
                         DateTime time = accessDict.First().Value.DtTm;
                         MainWindowViewModel.MainWindowInstance.SetNetboxDriftTime(time - beforeTime);
                    }
                    //handle reprocessed lists
                    if (reprocessList != null) {
                         Trace.TraceWarning("apiLoadAccessList::Reprocessed entries:\n");
                         foreach (var v in reprocessList) {
                              Trace.WriteLine($"LogId:{v.LogId}");
                         }

                         var rTuple = API_Interaction.GetReprocessedAccess(reprocessList);
                         var accessReturnList = rTuple.Item1;
                         var reprocessReturnList = rTuple.Item2;

                         if (reprocessReturnList.Any()) {
                              //create new task
                              var reprocessDBTask = new Task(() => {
                                   TraceEx.PrintLog("Adding reprocessed entries that 'failed' to be reprocessed again");
                                   using (var db = NetboxDatabase.GetWriteInstance()) {
                                        db.CopyListToTable(reprocessReturnList);
                                   }
                              });
                              dbTaskQueue.QueueTaskEnd("AddReprocessReprocessDB", reprocessDBTask);
                         }

                         //combine ensuring duplicate doesn't exist
                         foreach (var r in accessReturnList) {
                              accessDict[r.LogId] = r;
                         }
                    }
                    return Tuple.Create(accessDict, invalidEntries);
               }
               finally {
                    Benchmarker.Stop("APILoadAccessList");
               }
          }

          private static Dictionary<string, Person> apiLoadPeopleList(bool firstLoad, SerialTaskQueue dbTaskQueue, SerialTaskQueue apiTaskQueue, ref bool cancel, bool deleteEmptyCredential = false)
          {
               Benchmarker.Start("APILoadPeopleList");
               bool tempCancel = cancel;

               try {
                    DeleteType deleteType = DeleteType.False;
                    DBLoadStatus.WriteLine("APILoadPeopleList - returning value");

                    //don't do full updates during peek hours
                    var now = DateTime.Now;
                    if ((now.Hour > 6 && now.Hour < 10) || (now.Hour > 15 && now.Hour < 20)) {
                         TraceEx.PrintLog("Skipping full person load based on time.");
                         return null;
                    }

                    TraceEx.PrintLog("Doing full person update -> loading from API");

                    var peopleDict = LoadPersonDict(deleteType, apiTaskQueue);

                    TraceEx.PrintLog("Doing full person update -> done loading from API");

                    if (peopleDict == null) {
                         ShowErrorMessage("Person");
                         return null;
                    }

                    if (deleteEmptyCredential == true) {
                         TraceEx.PrintLog("apiLoadPeopleList: deleteEmptyCredential = true");

                         //find all person with no credential
                         var query = from p in peopleDict.Values
                                     where p.HasCredentials == false && p.Deleted == false
                                     select p;
                         if (query.Any()) {
                              Trace.TraceWarning("Removing people with empty credentials");
                              foreach (var v in query) {
                                   if (v.PersonId != "_1" && v.PersonId != "Administrator") {
                                        TraceEx.PrintLog($"Deleting Person with no credentials {v.PersonId}");
                                        API_Interaction.RemovePerson(v.PersonId);
                                   }
                              }

                              //reload from api to get proper list
                              peopleDict = LoadPersonDict(deleteType, apiTaskQueue);
                         }
                    }

                    //on first load add entire list to DataRepository, otherwise merge (to ensure newest data stays in repos)
                    if (firstLoad) {
                         TraceEx.PrintLog("UpdatePerson::  First Load");

                         var valueList = peopleDict.Values.ToList();

                         //force a replace since we want to overwrite database
                         DataRepository.ForceMergeToDict(PersonViewModel.ConvertList(valueList));
                    } else {
                         var tuple = DataRepository.MergeList(peopleDict, handleDeleted: deleteEmptyCredential);
                         var deletedList = tuple.Item1;
                         var modifiedList = tuple.Item2;

                         if (deletedList.Any()) {
                              //create new db task to mark db person as deleted
                              var DBMarkDeleted = new Task(() => {
                                   using (var db = NetboxDatabase.GetWriteInstance()) {
                                        db.MarkDeletedPerson(deletedList);
                                   }
                              });
                              dbTaskQueue.QueueTaskEnd("DBMarkDeleted", DBMarkDeleted);
                         }

                         if (modifiedList.Any()) {
                              //add db task queue
                              var DBModifyPerson = new Task(() => {
                                   using (var db = NetboxDatabase.GetWriteInstance()) {
                                        db.MergeListToTable(modifiedList);
                                   }
                              });
                              dbTaskQueue.QueueTaskEnd("DBModifyPerson", DBModifyPerson);
                         }
                    }
                    DataRepository.SetSortedList();
                    DataRepository.RefreshStaticLists();

                    try {
                         //compare
                         var mainWindow = MainWindowViewModel.MainWindowInstance;
                         bool unequal = false;
                         foreach (var person in peopleDict.Values) {
                              var dataReposPerson = DataRepository.PersonDict[person.PersonId];
                              if (dataReposPerson.Equals(new PersonViewModel(person)) == false) {
                                   unequal = true;
                                   break;
                              }
                         }

                         if (unequal) {
                              mainWindow.PrintStatusText("API Data mismatch!", Brushes.DarkRed);
                         }
                    }
                    catch (Exception e) {
                         Trace.TraceError($"Issue with API DataRepos comparison: {e.Message}");
                    }

                    Benchmarker.Stop("APILoadPeopleList");
                    return peopleDict;
               }
               finally {
                    cancel = tempCancel;
                    TraceEx.PrintLog("Doing full person update -> completely done -> exiting");
               }
          }

          private static Dictionary<string, RollCall> apiLoadRollCall(IEnumerable<AccessEntry> accessEntryList)
          {
               if (accessEntryList == null) {
                    return null;
               }
               Benchmarker.Start("APILoadRollCall");
               Dictionary<string, RollCall> rollcalldict = API_Interaction.GetRollCallList(accessEntryList);
               DataRepository.MergeRollCallDictionary(rollcalldict);
               Benchmarker.Stop("APILoadRollCall");
               //DBLoadStatus.IsLoadingAccess = false;
               return rollcalldict;
          }

          private static void dbCopyAccessList(Dictionary<long, AccessEntry> accessDict)
          {
               Benchmarker.Start("DBCopyAccessList");
               DBLoadStatus.IsLoadingDatabase = true;

               using (var db = NetboxDatabase.GetWriteInstance()) {
                    if (accessDict == null) {
                         return;
                    }
                    try {
                         db.CopyListToTable(accessDict);
                         DataRepository.CopyAccessEntriesToCache(accessDict);
                    }
                    finally {
                         DBLoadStatus.IsLoadingDatabase = false;
                         Benchmarker.Stop("DBCopyAccessList");
                    }
               }
          }

          private static void dbCopyFishList(Task<Dictionary<long, AccessEntry>> APIFishInvalidEntries)
          {
               Benchmarker.Start("DBCopyFishList");
               DBLoadStatus.IsLoadingDatabase = true;
               using (var db = NetboxDatabase.GetWriteInstance()) {
                    try {
                         var result = APIFishInvalidEntries.Result;
                         if (result != null) {
                              db.CopyListToTable(result);
                              DataRepository.CopyAccessEntriesToCache(result);
                         }
                    }
                    finally {
                         DBLoadStatus.IsLoadingDatabase = false;
                         Benchmarker.Stop("DBCopyFishList");
                    }
               }
          }

          private static void dbCopyRollCallToDb(Task<Dictionary<string, RollCall>> APILoadRollCall)
          {
               var result = APILoadRollCall.Result;
               DBLoadStatus.IsLoadingDatabase = true;
               using (var db = NetboxDatabase.GetWriteInstance()) {
                    try {
                         if (result != null) {
                              Benchmarker.Start("DBCopyRollCallToDb");

                              db.MergeListToTable(APILoadRollCall.Result);
                              //db.ClearTableRollCall();
                              //db.CopyListToTable(APILoadRollCall.Result);
                         }
                    }
                    finally {
                         DBLoadStatus.IsLoadingDatabase = false;
                         Benchmarker.Stop("DBCopyRollCallToDb");
                    }
               }
          }

          private static void dbCopyShiftList(List<ShiftEntry> shiftList)
          {
               DBLoadStatus.IsLoadingDatabase = true;
               using (var db = NetboxDatabase.GetWriteInstance()) {
                    try {
                         Benchmarker.Start("DBCopyShiftList");
                         int updateCount = 0;

                         if (shiftList == null) {
                              return;
                         }

                         try {
                              updateCount = db.UpdateFromList(shiftList);
                         }
                         catch (System.Data.SQLite.SQLiteException e) {
                              WriteExceptionErrorFile(e, db);
                              throw;
                         }

                         DBLoadStatus.WriteLine("Last Query: " + db.LastCommandText);
                    }
                    finally {
                         Benchmarker.Stop("DBCopyShiftList");
                         DBLoadStatus.IsLoadingDatabase = false;
                    }
               }
          }

          private static XDocument ExecuteLoadAccessCommand(long afterLogId, API_Interaction.SequenceType seqType)
          {
               XDocument xdoc = null;
               try {
                    xdoc = API_Interaction.ExecuteGetAccessHistory(seqType, afterLogId);
               }
               catch (WebException we) {
                    Trace.TraceError("WebException: loading access info: " + we.Message);
                    ShowErrorMessage("access");
               }
               catch (ApplicationException ae) {
                    Trace.TraceError("ApplicationException: loading access info: " + ae.Message);
                    ShowErrorMessage("access");
               }
               return xdoc;
          }

          private static void extractXDoc(XDocument xdoc, Dictionary<long, AccessEntry> mainDict, List<AccessReprocess> invalidEntries)
          {
               List<AccessEntry> list = API_Interaction.ExtractAccessList(xdoc);
               foreach (var v in list) {
                    //check for invalid entry - if invalid add to invalid entries list
                    if ((v.PersonId == null || v.PersonId == "") && v.Reason != ReasonCode.CardNotInS2NCDatabase) {
                         invalidEntries.Add(new AccessReprocess() { LogId = v.LogId });
                         continue;
                    }

                    mainDict[v.LogId] = v;
               }
          }

          private static void FishForInvalidEntries(Dictionary<long, AccessEntry> accessDict, long lastEntry, SerialTaskQueue queue)
          {
               Benchmarker.Start("FishForInvalidEntries");

               var numIters = 5;
               //Task[] tasks = new Task[numIters];
               long[] nums = new long[numIters];

               for (int i = 0; i < numIters; i++) {
                    nums[i] = lastEntry + i;
               }
               List<AccessEntry> returnList = null;
               var task = new Task((() => {
                    try {
                         returnList = API_Interaction.GetAccessDataLog(nums);
                    }
                    catch (WebException e) {
                         Trace.TraceError("WebException: loading invalid entries info: " + e.Message);
                         ShowErrorMessage("person");
                    }
                    catch (ApplicationException e) {
                         Trace.TraceError("ApplicationException: loading invalid entries info: " + e.Message);
                         ShowErrorMessage("invalid entries");
                    }
               }));
               queue.QueueTaskEnd("FishUpdate", task);

               Task.WaitAll(task);

               if (returnList != null) {
                    var query = from x in returnList
                                where string.IsNullOrEmpty(x.PersonId)
                                select x;
                    foreach (var v in query) {
                         accessDict[v.LogId] = v;
                         v.ShiftEntryProcessed = true;
                    }
               }

               Benchmarker.Stop("FishForInvalidEntries");
          }

          private static Dictionary<string, Person> LoadPersonDict(DeleteType deleteType, SerialTaskQueue queue)
          {
               int updateNum = 1;
               DBLoadStatus.WriteLine("Loading people list");
               XDocument xdoc = null;
               Dictionary<string, Person> mainDict = new Dictionary<string, Person>();
               long nextkey = -1;

               DBLoadStatus.PersonPage = updateNum;
               var task = new Task<XDocument>((() => {
                    return PersonTask(0, deleteType);
               }));
               queue.QueueTaskEnd($"PersonUpdate{updateNum}", task);

               xdoc = task.Result;
               updateNum++;

               if (xdoc == null) {
                    return null;
               }

               List<Person> list = API_Interaction.ExtractPersonList(xdoc);
               PutListInDict(mainDict, list);

               //api returns nextkey which tells us where to start to get the remaining data
               nextkey = Convert.ToInt64(xdoc.Descendants("NEXTKEY").First().Value);

               while (true) {
                    if (nextkey != -1) {
                         DBLoadStatus.PersonPage = updateNum;
                         lastKey = nextkey;
                         var task2 = new Task<XDocument>((() => {
                              return PersonTask(nextkey, deleteType);
                         }));
                         queue.QueueTaskEnd($"PersonUpdate{updateNum}", task2);

                         xdoc = task2.Result;
                         updateNum++;

                         if (xdoc == null) {
                              return null;
                         }
                         List<Person> loopList = API_Interaction.ExtractPersonList(xdoc);
                         PutListInDict(mainDict, loopList);

                         nextkey = Convert.ToInt64(xdoc.Descendants("NEXTKEY").First().Value);
                    } else {
                         break;
                    }
               }
               //Task.WaitAll(extractTask);
               lastUpdateCount = updateNum;
               return mainDict;
          }

          private static void PutListInDict(Dictionary<string, Person> mainDict, List<Person> list)
          {
               foreach (var p in list) {
                    mainDict[p.PersonId] = p;
               }
          }

          private static Dictionary<long, AccessEntry> LoadAccessLog(long afterLogId, SerialTaskQueue queue, List<AccessReprocess> invalidEntries)
          {
               XDocument xdoc = null;
               int updateNum = 0;

               DBLoadStatus.WriteLine("Loading access list");
               var mainDict = new Dictionary<long, AccessEntry>();

               var seqType = API_Interaction.SequenceType.AfterLogId;

               var task = new Task<XDocument>((() => {
                    return ExecuteLoadAccessCommand(afterLogId, seqType);
               }));
               queue.QueueTaskEnd($"AccessUpdate{updateNum}", task);

               xdoc = task.Result;
               updateNum++;

               if (xdoc == null) {
                    ShowErrorMessage("access");
                    return null;
               }

               var codeElement = xdoc.Element("NETBOX").Element("RESPONSE").Element("CODE");
               if (codeElement.Value == "NOT FOUND") {
                    ObtainTestData(mainDict);

                    return mainDict;
               }

               extractXDoc(xdoc, mainDict, invalidEntries);

               var nextkey = xdoc.Descendants("NEXTLOGID");

               while (true) {
                    if (nextkey.Any()) {
                         if (nextkey.First().Value == "-1") {
                              break;
                         }

                         var task2 = new Task<XDocument>((() => {
                              return ExecuteLoadAccessCommand(Convert.ToInt64(nextkey.First().Value) - 1, seqType);
                         }));
                         queue.QueueTaskEnd($"AccessUpdate{updateNum}", task2);

                         xdoc = task2.Result;
                         updateNum++;

                         if (xdoc == null) {
                              throw new ApplicationException("ExecuteCommand returned null.");
                         }

                         extractXDoc(xdoc, mainDict, invalidEntries);

                         nextkey = xdoc.Descendants("NEXTLOGID");
                    }
               }

               return mainDict;
          }

          /// <summary>
          /// Obtains test data (if there is any)
          /// </summary>
          /// <param name="mainDict"></param>
          private static void ObtainTestData(Dictionary<long, AccessEntry> mainDict)
          {
               if (DataRepository.AccessTestData.IsEmpty) {
                    return;
               }

               while (DataRepository.AccessTestData.TryPeek(out AccessEntry testEntry)) {
                    //check time
                    if (DateTime.Now >= testEntry.DtTm) {
                         bool popped = DataRepository.AccessTestData.TryDequeue(out testEntry);
                         if (popped == false) {
                              MessageBox.Show("Problem popping off access log test stack");
                         }

                         mainDict.Add(testEntry.LogId, testEntry);
                         Debug.WriteLine($"Adding testdata {testEntry.LogId}");
                    } else {
                         break;
                    }
               }
          }

          private static List<AccessEntry> obtainAccessEntryInfo(bool cancel, NetboxDatabase db, DateTime currentDate)
          {
               if (cancel) {
                    return null;
               }
               Benchmarker.Start("obtainAccessEntryInfo");

               var _loadedAccessList = db.LoadTableAccessEntry(currentDate.AddHours(-25));

               Benchmarker.Stop("obtainAccessEntryInfo");
               return _loadedAccessList;
          }

          private static List<ShiftEntry> obtainShiftEntryInfoFromDb(bool cancel, DateTime afterDate)
          {
               if (cancel) {
                    return null;
               }
               using (var db = NetboxDatabase.GetWriteInstance()) {
                    Benchmarker.Start("obtainEntryInfoFromDb");
                    var loadedEntryList = db.LoadTableShiftEntry(afterDate);
                    Benchmarker.Stop("obtainEntryInfoFromDb");
                    return loadedEntryList;
               }
          }

          private static XDocument PersonTask(long startFromKey, DeleteType deleteType)
          {
               XDocument xdoc = null;
               try {
                    xdoc = API_Interaction.ExecuteSearchPersonData(startFromKey, deleteType);
               }
               catch (WebException we) {
                    Trace.TraceError("WebException: loading person info: " + we.Message);
                    ShowErrorMessage("person");
               }
               catch(ApplicationException ae) {
                    Trace.TraceError("ApplicationException: loading person info: " + ae.Message);
                    ShowErrorMessage("person");
               }                    
               return xdoc;
          }

          private static void ShowErrorMessage(string type)
          {
               NetworkTools.PingLog();
               MainWindowViewModel.MainWindowInstance.PrintStatusText($"API CONNECTION ISSUE!", Brushes.Red);
          }

          /// <summary>
          /// Write exception error file
          /// </summary>
          /// <param name="e">Exception</param>
          /// <param name="db">Netbox db - used to get last query</param>
          private static void WriteExceptionErrorFile(Exception e, NetboxDatabase db)
          {
               using (var f = File.CreateText("SQLITE Exception.txt")) {
                    f.WriteLine(e.Message);
                    f.WriteLine(db.LastCommandText);
                    Trace.TraceError("SQLITE Exception: " + e.Message + " " + db.LastCommandText);
               }
          }

          private void dbFillReprocessList(List<AccessReprocess> reprocessList)
          {
               using (var db = NetboxDatabase.GetWriteInstance()) {
                    db.CopyListToTable(reprocessList);
               }
          }

          #endregion Methods
     }
}