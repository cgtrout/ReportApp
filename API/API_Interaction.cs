using ReportApp.Model;
using ReportApp.Utility;
using ReportApp.ViewModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Xml.Linq;

namespace API_Interface
{
     public enum DeleteType
     {
          All,      //all names are returned
          True,     //only deleted names are returned
          False     //only non deleted names are returned
     };

     /// <summary>
     /// This class is responsible for interacting with API.
     /// - uses API_Command to send commands to api
     /// </summary>
     public class API_Interaction
     {
          /// <summary>
          /// Load list of "Person" from API
          /// </summary>
          /// <returns>List of Persons</returns>
          public static async Task<List<Person>> LoadPersonDetails(DeleteType deleteType)
          {
               DBLoadStatus.WriteLine("Loading people list");

               var xdoc = await ExecuteSearchPersonDataAsync(0, deleteType);

               if (xdoc == null) {
                    throw new ApplicationException("Execute command returned null.");
               }

               List<Person> mainlist = new List<Person>();

               var extractTask = Task.Run(() => {
                    List<Person> list = ExtractPersonList(xdoc);
                    //mainlist.AddRange(list);
                    foreach (var p in list) {
                         var f = mainlist.Find(x => x.PersonId == p.PersonId);
                         if (f == null) {
                              mainlist.Add(p);
                         }
                    }
               });

               //api returns nextkey which tells us where to start to get the remaining data
               var nextkey = xdoc.Descendants("NEXTKEY");

               while (true) {
                    if (nextkey.Any()) {
                         if (nextkey.First().Value == "-1") {
                              break;
                         }

                         var xdoc2 = await ExecuteSearchPersonDataAsync(Convert.ToInt64(nextkey.First().Value), deleteType);

                         //handle null
                         if (xdoc2 == null) {
                              throw new ApplicationException("Execute command returned null.");
                         }
                         extractTask = extractTask.ContinueWith((x) => {
                              List<Person> loopList = ExtractPersonList(xdoc2);
                              //mainlist.AddRange(loopList);
                              foreach (var cperson in loopList) {
                                   var f = mainlist.Find(fx => fx.PersonId == cperson.PersonId);
                                   if (f == null) {
                                        mainlist.Add(cperson);
                                   }
                              }
                         });

                         nextkey = xdoc2.Descendants("NEXTKEY");
                    }
               }
               Task.WaitAll(extractTask);

               return mainlist;
          }

          /// <summary>
          /// Executes API command SearchPersonData
          /// </summary>
          /// <param name="startFromKey"></param>
          /// <param name="deleteType"></param>
          /// <returns></returns>
          public static async Task<XDocument> ExecuteSearchPersonDataAsync(long startFromKey, DeleteType deleteType)
          {
               var paramList = new List<Param>();

               if (startFromKey != 0) {
                    Param p = new Param("STARTFROMKEY", startFromKey.ToString());
                    paramList.Add(p);
               }

               paramList.Add(new Param("DELETED", deleteType.ToString().ToUpper()));

               XDocument xdoc2 = await API_Command.ExecuteCommandAsync(Command_Name.SearchPersonData, paramList);
               return xdoc2;
          }

          /// <summary>
          /// Executes API command SearchPersonData
          /// </summary>
          /// <param name="startFromKey"></param>
          /// <param name="deleteType"></param>
          /// <returns></returns>
          public static XDocument ExecuteSearchPersonData(long startFromKey, DeleteType deleteType)
          {
               var paramList = new List<Param>();

               if (startFromKey != 0) {
                    Param p = new Param("STARTFROMKEY", startFromKey.ToString());
                    paramList.Add(p);
               }

               paramList.Add(new Param("DELETED", deleteType.ToString().ToUpper()));

               XDocument xdoc2 = API_Command.ExecuteCommand(Command_Name.SearchPersonData, paramList);
               return xdoc2;
          }

          /// <summary>
          /// Load one person from api
          /// </summary>
          /// <returns></returns>
          public static async Task<Person> LoadSinglePerson(string id)
          {
               XDocument xdoc = null;
               try {
                    xdoc = await LoadSinglePersonXDoc(id);

                    bool isValid = xdoc.Descendants("CODE").First().FirstNode.ToString() != "NOT FOUND";
                    if(!isValid) {
                         xdoc = null;
                    }
               }
               catch (WebException) {
                    Trace.TraceError("LoadSinglePerson:: Connection issue");
               }
               catch (InvalidOperationException) {
                    xdoc = null;
               }
               if (xdoc == null) {
                    return null;
               }
               List<Person> list = ExtractPersonList(xdoc);
               Person first = null;
               if (list.Any()) {
                    first = list.First();
               }

               if (first != null) {
                    if (first.VehicleList.Any()) {
                         if (string.IsNullOrEmpty(first.VehicleList[0].LicNum)) {
                              first.VehicleList.Clear();
                         }
                    }
               }
               return first;
          }

          public static async Task<XDocument> LoadSinglePersonXDoc(string id)
          {
               System.Xml.Linq.XDocument xdoc;

               id = id.Trim();

               DBLoadStatus.WriteLine("Loading single person");

               var paramList = new List<Param>();
               paramList.Add(new Param("PERSONID", id));

               xdoc = await API_Command.ExecuteCommandAsync(Command_Name.SearchPersonData, paramList);
               return xdoc;
          }

          /// <summary>
          /// Extracts list of people from XDocument
          /// - must be formatted xml specically from SearchPersonData command
          /// </summary>
          /// <param name="xdoc">XDocument containing formatted xml</param>
          public static List<Person> ExtractPersonList(XDocument xdoc)
          {
               if (xdoc == null || xdoc.Descendants().First().Value == "NOT FOUND") {
                    return null;
               } else {
                    var personList = new List<Person>();
                    var elems = xdoc.Element("NETBOX").Element("RESPONSE").Element("DETAILS").Element("PEOPLE").Elements("PERSON");

                    foreach (var elem in elems) {
                         Person p = new Person();
                         p.FirstName = elem.Element("FIRSTNAME").Value;
                         p.LastName = elem.Element("LASTNAME").Value;
                         p.Deleted = Convert.ToBoolean(elem.Element("DELETED").Value);
                         p.PersonId = elem.Element("PERSONID").Value;
                         p.Company = elem.Element("UDF1").Value;

                         p.OrientationNumber = ConvertUtility.ConvertStrToInt64(elem.Element("UDF2").Value);
                         p.OrientationDate = ConvertUtility.ConvertStrToDateTime(elem.Element("UDF3").Value);
                         p.OrientationLevel = elem.Element("UDF4").Value;
                         p.OrientationTestedBy = elem.Element("UDF5").Value;

                         //OldComp contact - must handle null values
                         p.OldCompContact = ConvertUtility.ConvertStrHandleNull(elem.Element("UDF6").Value);

                         //get vehicles activated
                         var vehiclesActivatedString = elem.Element("UDF7").Value;
                         if (string.IsNullOrEmpty(vehiclesActivatedString)) {
                              p.VehiclesActivated = true;
                         } else {
                              p.VehiclesActivated = Convert.ToBoolean(vehiclesActivatedString);
                         }

                         p.EmployeeCategory = ConvertUtility.ConvertStrHandleNull(elem.Element("UDF8").Value);
                         p.VehicleReader = ConvertUtility.ConvertStrToInt32(elem.Element("UDF9").Value);

                         p.IsNetbox = true;

                         //load access cards
                         var accessCards = elem.Element("ACCESSCARDS").Elements("ACCESSCARD");
                         int facilityCodeCount = 0;
                         int pinCount = 0;
                         p.FobCredential = string.Empty;
                         foreach (var card in accessCards) {
                              if (card.Element("CARDFORMAT").Value.Contains("Facility Code")) {
                                   p.FobNumber = Convert.ToInt64(card.Element("ENCODEDNUM").Value);
                                   p.FobCredential = card.Element("CARDFORMAT").Value;
                                   facilityCodeCount++;
                              } else if (card.Element("CARDFORMAT").Value.Contains("OldComp")) {
                                   p.FobNumber = Convert.ToInt64(card.Element("ENCODEDNUM").Value);
                                   p.FobCredential = card.Element("CARDFORMAT").Value;
                                   facilityCodeCount++;
                              } else if (card.Element("CARDFORMAT").Value == "26 bit Wiegand") {
                                   p.PinNumber = Convert.ToInt64(card.Element("ENCODEDNUM").Value);
                                   pinCount++;
                              } else if(card.Element("CARDFORMAT").Value.Contains("34-bit HID Card")) {
                                   p.FobNumber = Convert.ToInt64(card.Element("ENCODEDNUM").Value);
                                   p.FobCredential = card.Element("CARDFORMAT").Value;
                                   facilityCodeCount++;
                              }
                         }

                         if (facilityCodeCount > 1 || pinCount > 1) {
                              try {
                                   var name = $"{ p.LastName }, { p.FirstName}";
                                   MainWindowViewModel.MainWindowInstance.PrintStatusText($"{name} too many credentials.", Brushes.Black);
                                   Trace.TraceError($"Found too many credentials on: {name}");
                              }
                              catch (Exception e) {
                                   Trace.TraceError("API_Interaction::ExtractPersonList Error printing status text. " + e.Message);
                              }
                         }

                         p.HasCredentials = accessCards.Any();

                         //load vehicles
                         var vehicles = elem.Element("VEHICLES").Elements("VEHICLE");
                         int i = 0;
                         foreach (var vehicle in vehicles) {
                              Vehicle v = new Vehicle();
                              v.Color = vehicle.Element("VEHICLECOLOR").Value;
                              v.LicNum = vehicle.Element("VEHICLELICNUM").Value;
                              v.Make = vehicle.Element("VEHICLEMAKE").Value;
                              v.Model = vehicle.Element("VEHICLEMODEL").Value;
                              v.TagNum = vehicle.Element("VEHICLETAGNUM").Value;

                              //if first is null, it is an api error on the return
                              if (string.IsNullOrEmpty(v.LicNum) && i == 0) {
                                   break;
                              } else {
                                   p.VehicleList.Add(v);
                              }
                              i++;
                         }

                         p.ActivationDate = ConvertUtility.ConvertStrToDateTime(elem.Element("ACTDATE").Value);

                         var expDate = elem.Element("EXPDATE");
                         if (expDate != null) {
                              p.ExpirationDate = ConvertUtility.ConvertStrToDateTime(expDate.Value);
                         }

                         var modifiedDate = elem.Element("LASTMOD");
                         if (modifiedDate != null) {
                              p.LastModified = ConvertUtility.ConvertStrToDateTime(modifiedDate.Value);
                         }
                                                  
                         if(elem.Element("ACCESSLEVELS").Elements("ACCESSLEVEL").Any()) {
                              var accessLevel = elem.Element("ACCESSLEVELS").Elements("ACCESSLEVEL").First();
                              p.AccessLevel = accessLevel.LastNode.ToString();
                         } else {
                              p.AccessLevel = Person.DEFAULT_ACCESS;
                         }
                         
                         personList.Add(p);
                    }
                    return personList;
               }
          }

          /// <summary>
          /// Load access log from API
          /// </summary>
          /// <param name="afterLogId">Only get values after this</param>
          /// <param name="dateToLoad">Loads one specific date</param>
          /// <param name="loadSpecificDate">If set to true will load data 'dateToLoad'</param>
          /// <returns>List of AccessEntry log data</returns>
          public static List<AccessEntry> LoadAccessLog(long afterLogId = -1, bool loadSpecificDate = false, DateTime? dateToLoad = null)
          {
               DBLoadStatus.WriteLine("Loading access list");
               var mainList = new List<AccessEntry>();

               //handle afterLogId
               var seqType = SequenceType.None;
               if (afterLogId != -1) {
                    seqType = SequenceType.AfterLogId;
               }

               var xdoc = ExecuteGetAccessHistory(seqType, afterLogId);

               if (xdoc == null) {
                    throw new ApplicationException("ExecuteCommand returned null.");
               }

               var codeElement = xdoc.Element("NETBOX").Element("RESPONSE").Element("CODE");
               if (codeElement.Value == "NOT FOUND") {
                    return mainList;
               }

               var extractTask = Task.Run(() => {
                    List<AccessEntry> list = ExtractAccessList(xdoc);
                    mainList.AddRange(list);
               });

               var nextkey = xdoc.Descendants("NEXTLOGID");

               while (true) {
                    if (nextkey.Any()) {
                         if (nextkey.First().Value == "-1") {
                              break;
                         }

                         //need to send different parameters depending on if we have loaded anything into the db yet
                         var sequenceType = afterLogId == -1 ? SequenceType.StartLogId : SequenceType.AfterLogId;

                         var xdoc2 = ExecuteGetAccessHistory(sequenceType, Convert.ToInt64(nextkey.First().Value));

                         if (xdoc2 == null) {
                              throw new ApplicationException("ExecuteCommand returned null.");
                         }

                         extractTask = extractTask.ContinueWith(x => {
                              mainList.AddRange(ExtractAccessList(xdoc2));
                         });

                         nextkey = xdoc2.Descendants("NEXTLOGID");
                    }
               }
               Task.WaitAll(extractTask);

               if (loadSpecificDate) {
                    var query = mainList.Where(x => x.DtTm.Date == dateToLoad.GetValueOrDefault().Date);
                    mainList = query.ToList();
               }

               return mainList;
          }

          public enum SequenceType
          {
               StartLogId,
               AfterLogId,
               None
          }

          public static XDocument ExecuteGetAccessHistory(SequenceType sType, long nextKey)
          {
               var paramList = new List<Param>();
               if (sType != SequenceType.None) {
                    Param p = new Param(sType.ToString().ToUpper(), nextKey.ToString());
                    paramList.Add(p);
               }
               var xdoc = API_Command.ExecuteCommand(Command_Name.GetAccessHistory, paramList);
               return xdoc;
          }

          /// <summary>
          /// GetReprocessedAccess
          ///   - uses reprocessList to reobtain accessEntries from database
          ///   - this is used for cases where accessentry was obtained before api had pulled the personid
          /// </summary>
          /// <param name="reprocessList"></param>
          /// <returns>Tuple of AccessEntry and AccessReprocess List</returns>
          public static Tuple<List<AccessEntry>, List<AccessReprocess>> GetReprocessedAccess(List<AccessReprocess> reprocessList)
          {
               var returnAccessList = new List<AccessEntry>();
               var returnReprocessList = new List<AccessReprocess>();
               foreach (var item in reprocessList) {
                    var newAccess = GetSingleAccess(item.LogId);

                    //we need to verify that we actually got the data we need
                    //if not the name needs to be reprocessed again

                    if (newAccess != null) {
                         TraceEx.PrintLog("Adding reprocessed access");

                         if (string.IsNullOrEmpty(newAccess.PersonId)) {
                              //empty so re-add to reprocess list
                              TraceEx.PrintLog($"GetReprocessAccess: personid still not loaded on access log id: {item.LogId}");
                              returnReprocessList.Add(item);
                         } else {
                              TraceEx.PrintLog("Logid=" + newAccess.LogId);
                              TraceEx.PrintLog("GetReprocessedAccess Adding to returnAccessList");
                              Trace.WriteLine(newAccess);
                              returnAccessList.Add(newAccess);
                         }
                    }
               }
               return Tuple.Create(returnAccessList, returnReprocessList);
          }

          /// <summary>
          /// Gets a single access
          /// </summary>
          /// <param name="accessId">id of access to obtain</param>
          /// <returns></returns>
          public static AccessEntry GetSingleAccess(long accessId)
          {
               var paramList = new List<Param>() {
                    new Param("STARTLOGID", Convert.ToString(accessId)),
                    new Param("MAXRECORDS", "1")
               };
               var xdoc = API_Command.ExecuteCommand(Command_Name.GetAccessHistory, paramList);
               CheckError(xdoc, "GetSingleAccess");

               var list = ExtractAccessList(xdoc);

               var returnVal = list.First();
               if (returnVal.LogId == accessId) {
                    return returnVal;
               } else {
                    return null;
               }
          }

          /// <summary>
          /// Gets a single access
          /// </summary>
          /// <param name="accessId">id of access to obtain</param>
          /// <returns></returns>
          public static async Task<AccessEntry> GetAccessDataLog(long accessId)
          {
               var paramList = new List<Param>() {
                    new Param("LOGID", Convert.ToString(accessId))
               };
               var xdoc = await API_Command.ExecuteCommandAsync(Command_Name.GetAccessDataLog, paramList);

               if (xdoc == null) {
                    Trace.TraceError("GetAccessDataLog - null returned - probably a connection issue");
                    throw new ApplicationException("Problem obtaining access data log.");
               }

               CheckError(xdoc, "GetSingleAccess2");
               var val = ExtractAccessEntry(xdoc);
               return val;
          }

          public static List<AccessEntry> GetAccessDataLog(long[] accessIds)
          {
               var length = accessIds.Length;
               var commands = new Command_Name[length];
               var paramLists = new List<Param>[length];

               //build command / paramlist arrays
               for (int i = 0; i < length; ++i) {
                    paramLists[i] = new List<Param>() {
                         new Param("LOGID", Convert.ToString(accessIds[i]))
                    };
                    commands[i] = Command_Name.GetAccessDataLog;
               }

               var xdoc = API_Command.ExecuteCommands(commands, paramLists);

               if (xdoc == null) {
                    Trace.TraceError("GetAccessDataLog - null returned - probably a connection issue");
                    throw new ApplicationException("Problem obtaining access data log.");
               }

               CheckError(xdoc, "GetAccessDataLogs");
               var val = ExtractMultiCommandAccessEntry(xdoc);
               return val;
          }

          private static List<AccessEntry> ExtractMultiCommandAccessEntry(XDocument xdoc)
          {
               var list = new List<AccessEntry>();

               //process each response
               var responses = xdoc.Element("NETBOX").Elements("RESPONSE");
               foreach (var elem in responses) {
                    if (elem.Element("CODE").Value == "NOT FOUND") {
                         continue;
                    } else {
                         XElement accessElem = elem.Element("DETAILS");
                         AccessEntry accessEntry = ExtractAccessEntryFromXElement(accessElem);
                         list.Add(accessEntry);
                    }
               }

               return list;
          }

          /// <summary>
          /// Extracts list of access from XDocument
          /// - must be formatted xml specically from GetAccessHistory command
          /// </summary>
          /// <param name="xdoc">Formatted xdoc to parse</param>
          public static List<AccessEntry> ExtractAccessList(XDocument xdoc)
          {
               if (xdoc.Descendants().First().Value == "NOT FOUND") {
                    return null;
               } else
                    return (from _access in xdoc.Element("NETBOX").Element("RESPONSE").Element("DETAILS").Element("ACCESSES").Elements("ACCESS")
                            select new AccessEntry {
                                 LogId = ConvertUtility.ConvertStrToInt64(_access.Element("LOGID").Value),
                                 PersonId = _access.Element("PERSONID").Value,
                                 Reader = _access.Element("READER").Value,
                                 DtTm = DateTime.Parse(_access.Element("DTTM").Value),
                                 Type = (ReportApp.Model.TypeCode)Convert.ToInt32(_access.Element("TYPE").Value),
                                 Reason = (ReasonCode)ConvertUtility.ConvertStrToInt32(_access.Element("REASON").Value),
                                 ReaderKey = (ReaderKeyEnum)ConvertUtility.ConvertStrToInt32(_access.Element("READERKEY").Value),
                                 PortalKey = ConvertUtility.ConvertStrToInt32(_access.Element("PORTALKEY").Value),
                                 ShiftEntryProcessed = false
                            }).ToList();
          }

          /// <summary>
          /// Extracts list of access from XDocument
          /// - must be formatted xml specically from GetAccessHistory command
          /// </summary>
          /// <param name="xdoc">Formatted xdoc to parse</param>
          private static AccessEntry ExtractAccessEntry(XDocument xdoc)
          {
               if (xdoc.Descendants().First().Value == "NOT FOUND") {
                    return null;
               } else {
                    var elem = xdoc.Element("NETBOX").Element("RESPONSE").Element("DETAILS");
                    try {
                         var accessEntry = ExtractAccessEntryFromXElement(elem);
                         return accessEntry;
                    }
                    catch (NullReferenceException) {
                         Trace.TraceError("ExtractAccessEntry:: NullReference error!");
                         TraceEx.PrintLog(elem.ToString());
                         return null;
                    }
               }
          }

          private static AccessEntry ExtractAccessEntryFromXElement(XElement elem)
          {
               var accessEntry = new AccessEntry {
                    LogId = ConvertUtility.ConvertStrToInt64(elem.Element("LOGID").Value),
                    PersonId = elem.Element("PERSONID")?.Value,
                    Reader = elem.Element("READER").Value,
                    DtTm = DateTime.Parse(elem.Element("DTTM").Value),
                    Type = (ReportApp.Model.TypeCode)Convert.ToInt32(elem.Element("TYPE").Value),
                    Reason = (ReasonCode)ConvertUtility.ConvertStrToInt32(elem.Element("REASON")?.Value),
                    ReaderKey = (ReaderKeyEnum)ConvertUtility.ConvertStrToInt32(elem.Element("READERKEY").Value),
                    ShiftEntryProcessed = false
                    //PortalKey = ConvertUtility.ConvertStrToInt32(elem.Element("PORTALKEY").Value),
               };
               return accessEntry;
          }

          /// <summary>
          /// Takes list of access entries and creates list of shift entries
          /// </summary>
          /// <param name="list">list of AccessEntries</param>
          /// <returns>List of new shift entries AND list of access entries that were processed</returns>
          /// <param name = "existing">The existing shiftentry data</param>
          public static Tuple<List<ShiftEntry>, Dictionary<long, AccessEntry>> CreateShiftList(List<AccessEntry> list, List<ShiftEntry> existing)
          {
               DBLoadStatus.WriteLine("Generating shift list");

               //int index = 0;
               if (list == null) {
                    return null;
               }
               List<ShiftEntry> shiftList = new List<ShiftEntry>();
               var processedDict = new Dictionary<long, AccessEntry>();

               if (existing is null) {
                    Trace.TraceError("CreateShiftList: existing is null");
               } else {
                    shiftList.AddRange(existing);
               }

               var q = from a in list
                       orderby a.DtTm
                       select a;

               foreach (var entry in q) {
                    if (entry.Reader.Contains("IN")) {
                         ShiftEntry newEntry = new ShiftEntry();
                         newEntry.ShiftEntryId = entry.LogId;
                         newEntry.PersonId = entry.PersonId;
                         newEntry.InLogId = entry.LogId;
                         newEntry.InTime = entry.DtTm;
                         newEntry.Hours = 0;
                         newEntry.OutLogId = 0;

                         //search to find out if they already have a shift entry
                         bool alreadyExists = (from x in shiftList
                                               where
                                                     x.PersonId == newEntry.PersonId &&
                                                      ((x.OutLogId == 0)  //don't have active entry
                                                       || x.InLogId == newEntry.InLogId   //entry hasn't already been added
                                                       || ((newEntry.InTime > x.InTime && newEntry.InTime < x.OutTime) //entry was active at this time
                                                            && x.OutLogId != 0 && newEntry.InTime.Date == x.InTime.Date)
                                                      )
                                               select x).Any();
                         //Trace.WriteLine($"Reader contains IN: alreadyExists={alreadyExists}");
                         if (!alreadyExists && !String.IsNullOrEmpty(newEntry.PersonId)) {
                              if (entry.Reason != ReasonCode.WrongLocation) {
                                   newEntry.IsChanged = true;
                                   shiftList.Add(newEntry);
                              }
                         }
                    } else if (entry.Reader.Contains("OUT")) {
                         //Trace.WriteLine($"Reader Contains OUT");
                         //check to see if in entry exists
                         var outQuery = from x in shiftList
                                        where x.PersonId == entry.PersonId && x.OutLogId == 0
                                        orderby x.InTime descending
                                        select x;
                         //found one, so place inside of exisintg
                         if (outQuery.Any()) {
                              ShiftEntry existing2 = outQuery.FirstOrDefault();

                              //ensure datetime is actually after the existing dttm
                              if (entry.DtTm > existing2.InTime) {
                                   existing2.OutLogId = entry.LogId;
                                   existing2.OutTime = entry.DtTm;
                                   TimeSpan difference = (existing2.OutTime.ToUniversalTime() - existing2.InTime.ToUniversalTime());
                                   existing2.Hours = (float)difference.TotalHours;
                                   existing2.IsChanged = true;
                              }
                         }
                    }
                    entry.ShiftEntryProcessed = true;
                    processedDict[entry.LogId] = entry;
               }
#if DEBUG
			//context.Log = new DebugTextWriter();
#endif
               DBLoadStatus.WriteLine("Completed: Generating shift list");
               return Tuple.Create(shiftList, processedDict);
          }

          /// <summary>
          /// Generates roll call list from access entry list
          /// </summary>
          /// <param name="accessList"></param>
          /// <returns>Generated roll call list</returns>
          public static Dictionary<string, RollCall> GetRollCallList(IEnumerable<AccessEntry> accessList)
          {
               DBLoadStatus.WriteLine("Generating roll call list");
               //get access data from up to 22 hours ago
               var query = from x in accessList
                           where x.DtTm > DateTime.Now.AddHours(-22) && (x.Reason == ReasonCode.AntiPassbackViolation || x.Reason == 0 || x.Reason == ReasonCode.InvalidPassback || x.Reason == ReasonCode.CardExpired)
                           orderby x.DtTm
                           select x;

               var rollcall = new Dictionary<string, RollCall>();

               foreach (var item in query) {
                    //if signed in
                    try {
                         if (item.Reader.Contains("IN")) {
                              if (rollcall.ContainsKey(item.PersonId) == false) {
                                   var rc = new RollCall();
                                   rc.PersonId = item.PersonId;
                                   rc.RollCallId = item.LogId;
                                   rc.DtTm = item.DtTm;
                                   rc.Reader = item.Reader;
                                   TimeSpan timespan = DateTime.Now - item.DtTm;
                                   rc.TimeIn = timespan.TotalHours;
                                   rollcall[item.PersonId] = rc;
                              }
                         } else if (item.Reader.Contains("OUT")) {
                              bool found = rollcall.ContainsKey(item.PersonId);
                              if (found) {
                                   rollcall.Remove(item.PersonId);
                              }
                         }
                    }
                    catch (Exception e) {
                         Trace.TraceWarning($"Error in get roll call list {e.Message}");
                    }
               }
               return rollcall;
          }

          /// <summary>
          /// Check for error in xdoc returned
          /// </summary>
          /// <param name="xdoc">XDocument to check</param>
          /// <param name="errorLocation">Where the error took place</param>
          private static void CheckError(XDocument xdoc, string errorLocation)
          {
               bool isError = false;

               if (xdoc == null) {
                    isError = true;
               } else {
                    isError = (from a in xdoc.Descendants("ERRMSG")
                               select a).Any();
               }

               if (isError) {
                    TraceEx.PrintLog($"Error in APICOMMAND: {errorLocation}");
                    TraceEx.PrintLog(xdoc.ToString());
                    throw new InvalidOperationException("API Error: " + errorLocation);
               }
          }

          /// <summary>
          /// Submits a new person to the API
          /// </summary>
          /// <param name="person">Person to add</param>
          /// <param name="writeOrientation">If set to true, write orientation data on submit</param>
          /// <returns></returns>
          public static async Task<string> AddPerson(Person person, bool writeOrientation)
          {
               List<Param> paramList = CreatePersonParamList(person);
               if (writeOrientation) {
                    AddOrientationParams(person, paramList);
               }

               GetVehicleParameters(person, paramList);
               TraceEx.PrintLog(String.Format("AddPerson {0}, {1} id={2}", person.LastName, person.FirstName, person.PersonId));
               TraceEx.PrintLog(String.Format("Parameters: {0}", Param.ToStringList(paramList)));

               var xDoc = await API_Command.ExecuteCommandAsync(Command_Name.AddPerson, paramList);
               CheckError(xDoc, "AddPerson");
               var PersonID = xDoc.Element("NETBOX").Element("RESPONSE").Element("DETAILS").Element("PERSONID");
               if (PersonID != null) {
                    return PersonID.Value;
               }
               return null;
          }

          /// <summary>
          /// Submits a person to the API to modify
          /// </summary>
          /// <param name="person">Person to modify</param>
          /// <param name="WriteOrientation">If set to true, writes orientation data</param>
          /// <returns></returns>
          public static async Task ModifyPerson(Person person, bool WriteOrientation)
          {
               List<Param> paramList = CreatePersonParamList(person);
               paramList.Add(new Param("PERSONID", person.PersonId));

               if (WriteOrientation) {
                    AddOrientationParams(person, paramList);
               }

               GetVehicleParameters(person, paramList);

               TraceEx.PrintLog(String.Format("ModifyPerson {0}, {1} id={2}", person.LastName, person.FirstName, person.PersonId));
               TraceEx.PrintLog(String.Format("Parameters: {0}", Param.ToStringList(paramList)));
               var xDoc = await API_Command.ExecuteCommandAsync(Command_Name.ModifyPerson, paramList);
               CheckError(xDoc, "ModifyPerson");
          }

          private static void AddOrientationParams(Person person, List<Param> paramList)
          {
               paramList.Add(new Param("UDF3", person.OrientationDate.ToString("yyyy-M-dd")));
          }

          public static List<Param> CreatePersonParamList(Person person)
          {
               return new List<Param>() { new Param("LASTNAME", person.LastName),
                     new Param("FIRSTNAME", person.FirstName),
                     new Param("LASTNAME", person.LastName),
                     new Param("UDF1", person.Company),
                     new Param("ACCESSLEVELS", $"<ACCESSLEVEL>{person.GetValidAccessLevel()}</ACCESSLEVEL>"),
                     new Param("ACTDATE", person.ActivationDate.GetValueOrDefault().ToString("yyyy-M-dd")),
                     new Param("UDF2", person.OrientationNumber.ToString()),
                     new Param("UDF6", person.OldCompContact.ToString()),
                     new Param("UDF7", person.VehiclesActivated.ToString()),
                     new Param("EXPDATE", ConvertUtility.EmptyStringIfDateZero(person.ExpirationDate.Value)),
                     new Param("UDF4", person.OrientationLevel),
                     new Param("UDF5", person.OrientationTestedBy),
                     new Param("UDF8", person.EmployeeCategory),
                     new Param("UDF9", person.VehicleReader.ToString())
                 };
          }

          public static string GenerateVehicleXML(List<Vehicle> vehicles)
          {
               var xelement = new XElement("VEHICLES",
                                        from v in vehicles
                                        select new XElement("VEHICLE",
                                             new XElement("VEHICLECOLOR", v.Color),
                                             new XElement("VEHICLEMAKE", v.Make),
                                             new XElement("VEHICLEMODEL", v.Model),
                                             new XElement("VEHICLELICNUM", v.LicNum)));

               string xmlString = String.Empty;
               var vehicleElements = xelement.Elements("VEHICLE");
               foreach (var v in vehicleElements) {
                    xmlString += v.ToString() + "\r\n";
               }
               return xmlString;
          }

          /// <summary>
          /// Adds vehicle information to parameter list
          /// </summary>
          /// <param name="person"></param>
          /// <param name="paramList"></param>
          private static void GetVehicleParameters(Person person, List<Param> paramList)
          {
               //handle vehicles
               bool addedDummyVehicle = false;
               if (!person.VehicleList.Any()) {
                    //add "dummy" vehicle - workaround
                    var v1 = new Vehicle();
                    person.VehicleList.Add(v1);
                    addedDummyVehicle = true;
               }
               var xmlString = GenerateVehicleXML(person.VehicleList);
#if DEBUG

            TraceEx.PrintLog($"GENERATED VEHICLE XML\n{xmlString}\n");
#endif
               paramList.Add(new Param("VEHICLES", xmlString));

               if (addedDummyVehicle) {
                    person.VehicleList.Clear();
               }
          }

          /// <summary>
          /// Add a Credential (key information) to API
          /// </summary>
          /// <param name="personid">string id of person to add</param>
          /// <param name="encodedNum">long number of key/pin</param>
          /// <param name="cardFormat">type of credential</param>
          /// <returns></returns>
          public static async Task AddCredential(string personid, long encodedNum, string cardFormat)
          {
               Trace.Assert(!string.IsNullOrWhiteSpace(personid), "AddCredential: personid is null");
               TraceEx.PrintLog(String.Format("API::AddCredential: personid={0} encodedNum={1} cardFormat={2}", personid, encodedNum, cardFormat));

               var paramList = new List<Param>() {
                    new Param("PERSONID", personid),
                    new Param("ENCODEDNUM", encodedNum.ToString()),
                    new Param("CARDFORMAT", cardFormat),
               };
               XDocument xDoc = await API_Command.ExecuteCommandAsync(Command_Name.AddCredential, paramList, logXmlInput: false);
               CheckError(xDoc, "AddCredential");
          }

          /// <summary>
          /// Removes a credential from a person in the NETBOX system
          /// </summary>
          /// <param name="personid">string id of person to modify</param>
          /// <param name="encodedNum">long number of card/pin</param>
          /// <param name="cardFormat">type of credential</param>
          /// <returns></returns>
          public static async Task RemoveCredential(string personid, long encodedNum, string cardFormat)
          {
               Trace.Assert(!string.IsNullOrWhiteSpace(personid), "RemoveCredential: personid is null");
               TraceEx.PrintLog($"API::RemoveCredential: personid={personid} encodedNum={encodedNum} cardFormat={cardFormat}");

               var paramList = new List<Param>() {
                    new Param("PERSONID", personid),
                    new Param("ENCODEDNUM", encodedNum.ToString()),
                    new Param("CARDFORMAT", cardFormat)
               };
               XDocument xDoc = await API_Command.ExecuteCommandAsync(Command_Name.RemoveCredential, paramList);
               CheckError(xDoc, "RemoveCredential");
          }

          /// <summary>
          /// Remove a person from the Netbox system (really only sets delete)
          /// </summary>
          /// <param name="personid">string id of person to remove</param>
          /// <returns></returns>
          public static void RemovePerson(string personid)
          {
               TraceEx.PrintLog("Deleteing person: " + personid);
               var paramList = new List<Param>() {
                    new Param("PERSONID", personid)
               };
               XDocument xDoc = API_Command.ExecuteCommand(Command_Name.RemovePerson, paramList);
               CheckError(xDoc, "RemovePerson");
          }
     }
}