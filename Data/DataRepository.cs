using ReportApp.Model;
using ReportApp.Utility;
using ReportApp.ViewModel;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace ReportApp.Data
{
     /// <summary>
     /// DataRepository Class
     ///  - used to store local cache of data
     /// </summary>
     public static class DataRepository
     {
          #region Fields

          private static List<string> _OldCompContactList;
          private static List<string> _companyList;
          private static ConcurrentDictionary<string, PersonViewModel> _personDict;

          private static List<string> _testedByList;
          private static Dictionary<long, PersonViewModel> _pinList;
          private static Dictionary<string, PersonViewModel> _fobList;

          #endregion Fields

          #region Constructors

          static DataRepository()
          {
          }

          #endregion Constructors

          #region Events

          public static event DictionaryChangedEventHandler PersonChanged;

          public static event DictionaryChangedEventHandler RollcallChanged;

          #endregion Events

          #region Properties

          public static CollectionMessageHandler AccessLogMessageHandler { get; set; } = new CollectionMessageHandler();

          /// <summary>
          /// OldComp Contact Static list
          /// </summary>
          public static List<string> OldCompContactList
          {
               get
               {
                    if (_OldCompContactList == null) {
                         _OldCompContactList = new List<string>();
                    }
                    return _OldCompContactList;
               }
          }

          /// <summary>
          /// List of companies used in the system
          /// </summary>
          public static List<string> CompanyList
          {
               get
               {
                    if (_companyList == null) {
                         _companyList = new List<string>();
                    }
                    return _companyList;
               }
          }

          /// <summary>
          /// List of pins used in the system
          /// </summary>
          public static Dictionary<long, PersonViewModel> PinList
          {
               get
               {
                    if (_pinList == null) {
                         _pinList = new Dictionary<long, PersonViewModel>();
                    }
                    return _pinList;
               }
          }

          public static Dictionary<string, PersonViewModel> FobList
          {
               get
               {
                    if (_fobList == null) {
                         _fobList = new Dictionary<string, PersonViewModel>();
                    }
                    return _fobList;
               }
          }

          public static ConcurrentDictionary<string, PersonViewModel> PersonDict
          {
               get
               {
                    if (_personDict == null) {
                         _personDict = new ConcurrentDictionary<string, PersonViewModel>();
                    }
                    return _personDict;
               }
               set
               {
                    _personDict = value;
               }
          }

          public static ConcurrentDictionary<string, SingleRollCallViewModel> RollCallDict { get; set; } = new ConcurrentDictionary<string, SingleRollCallViewModel>();
          public static CollectionMessageHandler ShiftEntryMessageHandler { get; set; } = new CollectionMessageHandler();
          public static List<PersonViewModel> SortedList { get; set; } = new List<PersonViewModel>();

          /// <summary>
          /// TestedBy Static list
          /// </summary>
          public static List<string> TestedByList
          {
               get
               {
                    if (_testedByList == null) {
                         _testedByList = new List<string>();
                    }
                    return _testedByList;
               }
          }

          //ACCESS LOG CACHE
          public static Dictionary<long, AccessEntry> AccessCache { get; set; } = new Dictionary<long, AccessEntry>();

          public static DateTime CacheClearTime { get; private set; } = DateTime.Now;

          //used for storing test data that will be injected to db
          public static ConcurrentQueue<AccessEntry> AccessTestData { get; set; } = new ConcurrentQueue<AccessEntry>();

          public static long MaxAccessId { get; private set; } = 0;

          #endregion Properties

          #region Methods

          /// <summary>
          /// Adds list of person to local dictionary - designed to be used on first load
          /// </summary>
          /// <param name="list"></param>
          public static void AddListToDict(List<PersonViewModel> list)
          {
               foreach (var p in list) {
                    PersonDict.TryAdd(p.PersonId, p);
               }
               RefreshStaticLists();
          }

          /// <summary>
          /// Add an individual person to internal repository
          /// </summary>
          /// <param name="p">PersonViewModel to add</param>
          public static void AddPerson(PersonViewModel p)
          {
               //ensure doesn't exist
               bool existed = PersonDict.ContainsKey(p.PersonId);
               PersonViewModel original = null;
               if (existed) {
                    original = PersonDict[p.PersonId];
               }
               p.IsNetbox = true;
               PersonDict[p.PersonId] = p;

               //create event
               DictionaryChangedEventArgs e = new DictionaryChangedEventArgs();
               e.ChangedValue = p;
               if (existed) {
                    e.Edit = true;
                    e.OriginalValue = original.Copy();
               } else {
                    e.Edit = false;
               }
               OnPersonChanged(e, refreshLists: true);
          }

          /// <summary>
          /// Search for a credential fob
          /// </summary>
          /// <param name="fob">long fob to search for</param>
          /// <returns>Person if found</returns>
          public static PersonViewModel FindCredentialFob(long fob, string credentialName)
          {
               PersonViewModel p = null;

               var key = credentialName + fob.ToString();

               if (FobList.ContainsKey(key)) {
                    p = DataRepository.FobList[key];
               }
               if (p != null) {
                    return p;
               } else {
                    return null;
               }
          }

          /// <summary>
          /// Search for a credential pin
          /// </summary>
          /// <param name="pin">long pin to search for</param>
          /// <returns>Person if found</returns>
          public static PersonViewModel FindCredentialPin(long pin)
          {
               PersonViewModel p = null;

               if (PinList.ContainsKey(pin)) {
                    p = PinList[pin];
               }
               if (p != null) {
                    return p;
               } else {
                    return null;
               }
          }

          /// <summary>
          /// Force merge - used where we want the list to absoultely take priority over what is already in dict
          /// </summary>
          /// <param name="list"></param>
          public static void ForceMergeToDict(List<PersonViewModel> list)
          {
               foreach (var p in list) {
                    PersonDict[p.PersonId] = p;
               }
               RefreshStaticLists();
          }

          /// <summary>
          /// Obtains the newest orientation number.
          /// </summary>
          /// <returns>long of newest orientation number</returns>
          public static long GetLastOrientationNumber()
          {
               var max = (from p in PersonDict.Values
                          select p.OrientationNumber).Max();
               TraceEx.PrintLog($"GetLastOrientationNumber() max={max}");
               return max + 1;
          }

          /// <summary>
          /// Merge other list into DataRepository
          /// </summary>
          /// <param name="inputPersonDict"></param>
          /// <returns>List of string of persons that are now deleted and list of persons that were modified</returns>
          public static Tuple<List<string>, List<Person>> MergeList(Dictionary<string, Person> inputPersonDict, bool handleDeleted)
          {
               var apiquery = from p in inputPersonDict.Values
                              where p.LastModified > DateTime.Now.AddMinutes(-10)
                              select p;
               Stopwatch sw = new Stopwatch();
               sw.Start();

               Dictionary<string, bool> notDeletedDictionary = new Dictionary<string, bool>();

               if (handleDeleted) {
                    //create dictionary to mark those that are deleted
                    //we only load nondeleted from api so we can save time
                    //we assume those that don't show up (ie get marked as true in dict) are now deleted

                    foreach (var v in DataRepository.PersonDict.Values) {
                         if (v.IsNetbox && v.Deleted == false) {
                              notDeletedDictionary.Add(v.PersonId, false);
                         }
                    }

                    foreach (var p in inputPersonDict.Values) {
                         notDeletedDictionary[p.PersonId] = true;
                    }

                    //now mark those not deleted as deleted in repos
                    var nowDeletedQuery = from keyvaluepair in notDeletedDictionary
                                          where keyvaluepair.Value == false
                                          select keyvaluepair;
                    foreach (var kvp in nowDeletedQuery) {
                         var person = PersonDict[kvp.Key];
                         person.Deleted = true;

                         //Netbox will delete credentials when a person is deleted so we need to clear these
                         person.FobNumber = 0;
                         person.PinNumber = 0;
                    }
               }

               var modifyDict = new Dictionary<string, Person>();

               foreach (var apiPerson in apiquery) {
                    bool doesPersonExist = PersonDict.ContainsKey(apiPerson.PersonId);

                    if (doesPersonExist) {
                         //exists, so check to see which was updated soonest
                         var reposPerson = PersonDict[apiPerson.PersonId].InternalPerson;

                         if (apiPerson.LastModified > reposPerson.LastModified) {
                              //only called if user submits data directly to Netbox software
                              TraceEx.PrintLog($"DataRepos::MergeList - modifying {apiPerson}");
                              Trace.WriteLine($"apiPerson.LastModified={apiPerson.LastModified} reposPerson.LastModified={reposPerson.LastModified}");
                              ModifyPerson(new PersonViewModel(apiPerson), false);
                              modifyDict[apiPerson.PersonId] = apiPerson;
                         }
                    } else {
                         //does not exist, so add it]
                         AddPerson(new PersonViewModel(apiPerson));
                         modifyDict[apiPerson.PersonId] = apiPerson;
                    }

                    //check credentials to see if they exist on others - if they do then add them to modify list as well

                    //fob
                    if (DataRepository.FobList.ContainsKey(apiPerson.FobCredential + apiPerson.FobNumber.Value.ToString())) {
                         var otherPerson = DataRepository.FobList[apiPerson.FobCredential + apiPerson.FobNumber.Value.ToString()];
                         if (otherPerson.PersonId != apiPerson.PersonId && !modifyDict.ContainsKey(otherPerson.PersonId)) {
                              modifyDict[otherPerson.PersonId] = inputPersonDict[otherPerson.PersonId];
                              ModifyPerson(new PersonViewModel(inputPersonDict[otherPerson.PersonId]), false);
                         }
                    }

                    //pin
                    if (DataRepository.PinList.ContainsKey(apiPerson.PinNumber.Value)) {
                         var otherPerson = DataRepository.PinList[apiPerson.PinNumber.Value];
                         if (otherPerson.PersonId != apiPerson.PersonId && !modifyDict.ContainsKey(otherPerson.PersonId)) {
                              modifyDict[otherPerson.PersonId] = inputPersonDict[otherPerson.PersonId];
                              ModifyPerson(new PersonViewModel(inputPersonDict[otherPerson.PersonId]), false);
                         }
                    }
               }

               sw.Stop();
               //TraceEx.PrintLog($"DataRepository:MergeList time={sw.ElapsedMilliseconds}ms");

               //generate deleted list (that need to be changed in database)
               var delQuery = notDeletedDictionary.Where(x => x.Value == false);
               var delList = new List<string>();
               foreach (var kvp in delQuery) {
                    delList.Add(kvp.Key);
               }
               return Tuple.Create(delList, modifyDict.Values.ToList());
          }

          /// <summary>
          /// Merge external rollcall into the concurrent version
          /// </summary>
          public static void MergeRollCallDictionary(Dictionary<string, RollCall> other)
          {
               foreach (var rollcall in other.Values) {
                    if (RollCallDict.ContainsKey(rollcall.PersonId) == false) {
                         RollCallDict[rollcall.PersonId] = new SingleRollCallViewModel(rollcall);

                         //send event
                         DictionaryChangedEventArgs e = new DictionaryChangedEventArgs() {
                              ChangedValue = RollCallDict[rollcall.PersonId],
                              Edit = false
                         };
                         OnRollcallChanged(e);
                    }
               }

               //iterate through repos dict unset any that don't exist
               foreach (var rollcall in RollCallDict.Values) {
                    if (other.ContainsKey(rollcall.PersonId) == false) {
                         SingleRollCallViewModel outValue = null;
                         var removedValue = RollCallDict[rollcall.PersonId];
                         bool removed = RollCallDict.TryRemove(rollcall.PersonId, out outValue);
                         if (removed == false) {
                              Trace.TraceError($"MergeRollCallDictionary:: Could not remove person {rollcall.PersonId}");
                         } else {
                              //send event
                              DictionaryChangedEventArgs e = new DictionaryChangedEventArgs() {
                                   ChangedValue = removedValue,
                                   Remove = true
                              };
                              OnRollcallChanged(e);
                         }
                    }
               }
          }

          public static void ModifyPerson(PersonViewModel inputPerson, bool refreshLists)
          {
               var reposPerson = DataRepository.PersonDict[inputPerson.PersonId];
               var originalReposPerson = reposPerson.Copy();
               reposPerson.CopyFromOther(inputPerson);

               //event
               DictionaryChangedEventArgs e = new DictionaryChangedEventArgs();

               e.ChangedValue = inputPerson;
               e.Edit = true;
               e.OriginalValue = originalReposPerson;

               TraceEx.PrintLog($"ModifyPerson : fire OnPersonChanged Orig={e.OriginalValue} Changed={e.ChangedValue} ");
               OnPersonChanged(e, refreshLists);
          }

          public static void OnPersonChanged(DictionaryChangedEventArgs e, bool refreshLists)
          {
               var dispatcher = DispatcherHelper.GetDispatcher();
               dispatcher.BeginInvoke(new Action(() => {
                    if (refreshLists) {
                         RefreshStaticLists();
                    }
                    if (PersonChanged != null) {
                         PersonChanged(e);
                    }
               }), DispatcherPriority.Render);
          }

          public static void OnRollcallChanged(DictionaryChangedEventArgs e)
          {
               var dispatcher = DispatcherHelper.GetDispatcher();
               dispatcher.BeginInvoke(new Action(() => {
                    if (RollcallChanged != null) {
                         RollcallChanged(e);
                    }
               }), DispatcherPriority.Render);
          }

          public static void RefreshOldCompContactList()
          {
               if (_personDict != null) {
                    var q = (from p in _personDict.Values
                             where !string.IsNullOrWhiteSpace(p.OldCompContact) && p.IsNetbox && !p.Deleted && p.OrientationDate > DateTime.Today.AddMonths(-8)
                             orderby p.OldCompContact
                             select p.OldCompContact).Distinct();
                    _OldCompContactList = q.ToList();
               }
          }

          public static void RefreshCompanyList()
          {
               if (_personDict != null) {
                    var q = (from p in _personDict.Values
                             where !string.IsNullOrWhiteSpace(p.Company) && p.IsNetbox
                             orderby p.Company
                             select p.Company).Distinct();
                    _companyList = q.ToList();
               }
          }

          public static void RefreshPinList()
          {
               if (_personDict != null) {
                    var q = (from p in _personDict.Values
                             where p.IsNetbox && !p.Deleted
                             orderby p.PinNumber
                             select p);
                    var newPinList = new Dictionary<long, PersonViewModel>();
                    PersonViewModel person = null;
                    foreach (var v in q) {
                         try {
                              person = v;
                              if (v.PinNumber != 0) {
                                   newPinList.Add(v.PinNumber, v);
                              }
                         }
                         catch (ArgumentException e) {
                              var pstring = string.Empty;
                              if (person != null) {
                                   pstring = $"{person}";
                              }
                              Trace.TraceError($"RefreshPinList: Duplicate key exception: {e.Message} {pstring}");
                         }
                    }
                    _pinList = newPinList;
               }
          }

          public static void RefreshFobList()
          {
               if (_personDict != null) {
                    var query = (from p in _personDict.Values
                                 where p.IsNetbox && !p.Deleted
                                 orderby p.FobNumber
                                 select p);
                    var newFobList = new Dictionary<string, PersonViewModel>();
                    foreach (var person in query) {
                         try {
                              if (person.FobNumber != 0) {
                                   newFobList.Add(person.FobCredential + person.FobNumber.ToString(), person);
                              }
                         }
                         catch (ArgumentException e) {
                              Trace.TraceError($"RefreshFobList: Duplicate key exception: {e.Message} Person:{person}");
                         }
                    }
                    _fobList = newFobList;
               }
          }

          /// <summary>
          /// Refreshes all static lists whenever data is changed
          /// </summary>
          public static void RefreshStaticLists()
          {
               Benchmarker.Start("RefreshStaticLists()");
               RefreshCompanyList();
               RefreshTestByList();
               RefreshOldCompContactList();
               RefreshPinList();
               RefreshFobList();
               Benchmarker.Stop("RefreshStaticLists()");
          }

          public static void RefreshTestByList()
          {
               if (_personDict != null) {
                    var q = (from p in _personDict.Values
                             where !string.IsNullOrWhiteSpace(p.OrientationTestedBy) && p.IsNetbox && !p.Deleted && p.OrientationDate > DateTime.Today.AddMonths(-8)
                             orderby p.OrientationTestedBy
                             select p.OrientationTestedBy).Distinct();
                    _testedByList = q.ToList();
               }
          }

          /// <summary>
          /// Remove a credential from a person
          /// </summary>
          /// <param name="person">Person to search for</param>
          /// <param name="encodedNum">long number of credential</param>
          /// <param name="cardFormat">string format of card</param>
          public static async Task RemoveCredential(PersonViewModel person, long encodedNum, string cardFormat)
          {
               TraceEx.PrintLog($"Repository::Remove Credential: {person.PersonId} {encodedNum} {cardFormat}");

               PersonViewModel p = null;
               if (PersonDict.ContainsKey(person.PersonId)) {
                    p = PersonDict[person.PersonId];
               } else {
                    Trace.TraceError("DataRepos::RemoveCred: Could not find person=" + person.PersonId);
                    return;
               }

               if (cardFormat.Contains("26 bit Wiegand")) {
                    p.PinNumber = 0;
               } else {
                    p.FobNumber = 0;
               }

               //get actual data from API
               var apiPerson = new PersonViewModel(await API_Interface.API_Interaction.LoadSinglePerson(person.PersonId));
               ModifyPerson(apiPerson, true);
          }

          /// <summary>
          /// Search for orientation number
          /// </summary>
          /// <param name="number"></param>
          /// <returns>true if orientation number found</returns>
          public static bool SearchOrientationNumber(long number)
          {
               var q = from p in _personDict.Values
                       where p.OrientationNumber == number
                       select p;
               return q.Any();
          }

          public static void SetSortedList()
          {
               var query = from x in PersonDict.Values
                           where x.IsNetbox == true && x.Deleted == false
                           orderby x.LastName, x.FirstName
                           select x;
               SortedList = new List<PersonViewModel>(query);
          }

          public static void ClearAccessCache()
          {
               AccessCache.Clear();
               CacheClearTime = DateTime.Now;

               TraceEx.PrintLog("Clearing Access Cache");
          }

          public static void CopyAccessEntriesToCache(Dictionary<long, AccessEntry> entries)
          {
               foreach (var v in entries) {
                    AccessCache[v.Key] = v.Value;
                    if (v.Value.LogId > MaxAccessId) {
                         MaxAccessId = v.Value.LogId;
                    }
               }
          }

          public static void CopyAccessEntriesToCache(List<AccessEntry> entries)
          {
               foreach (var v in entries) {
                    AccessCache[v.LogId] = v;
                    if (v.LogId > MaxAccessId) {
                         MaxAccessId = v.LogId;
                    }
               }
          }

          internal static void AddCredential(PersonViewModel person, long encodedNumber, string cardFormat)
          {
               TraceEx.PrintLog($"Repository::Add Credential: {person.PersonId} {encodedNumber} {cardFormat}");
               PersonViewModel p = null;

               if (PersonDict.ContainsKey(person.PersonId)) {
                    p = PersonDict[person.PersonId];
               } else {
                    Trace.TraceWarning("DataRepos::AddCred: Could not find person=" + person.PersonId);
                    return;
               }

               if (cardFormat.Contains("26 bit Wiegand")) {
                    p.PinNumber = encodedNumber;
               } else {
                    p.FobNumber = encodedNumber;
               }
          }

          #endregion Methods
     }
}