using API_Interface;
using ProxCard2;
using ReportApp.Data;
using ReportApp.Model;
using ReportApp.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Media;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace ReportApp.ViewModel
{
     /// <summary>
     /// Description of AddPerson.
     /// </summary>
     public class AddPersonViewModel : WorkspaceViewModel
     {
          #region Fields

          private bool _controlsEnabled = true;
          private ICommand _currentDateClickedCommand;
          private ICommand _expirationClearCommand;
          private ICommand _getOrientationCommand;
          private ICommand _saveCommand;
          private bool _vehiclesExpanded = false;
          private PersonViewModel OrigPerson;

          private bool openInNetbox = false;

          //public AddPersonViewModel()
          //{
          //}

          private ProxCard2.ReaderList proxCard;

          private PersonViewModel _currentPerson;

          private ICommand _shiftEntryCommand;

          private ICommand _netboxLoadCommand;

          private bool _inReadMode = false;

          private ICommand _readButtonCommand;
          private ICommand _copyCommand;
          private ICommand _randomPinCommand;
          private ICommand _addAwayListCommand;
          private ICommand _recallLastCommand;
          private ICommand _recallLastCompanyCommand;

          #endregion Fields

          #region Enums

          public enum CredentialType
          {
               Pin,
               Fob
          };

          private enum ErrorLevels { Question, Critical };

          #endregion Enums

          #region Properties

          public string DisplayVerb => EditMode ? "Edit " : "Add ";

          public List<string> OldCompContactList => DataRepository.OldCompContactList;

          public List<string> CompanyList => DataRepository.CompanyList;

          public bool ControlsEnabled
          {
               get { return _controlsEnabled; }
               set
               {
                    if (value == false) {
                         DispatcherHelper.GetDispatcher().Invoke(() => Mouse.OverrideCursor = Cursors.Wait);
                         CurrentPerson.CompanyPopupIsOpen = false;
                         CurrentPerson.FobPopupIsOpen = false;
                         CurrentPerson.OrientationPopupIsOpen = false;
                         CurrentPerson.PinPopupIsOpen = false;
                    } else {
                         DispatcherHelper.GetDispatcher().Invoke(() => Mouse.OverrideCursor = null);
                    }
                    _controlsEnabled = value;
                    OnPropertyChanged(nameof(ControlsEnabled));
               }
          }

          public bool InReadMode
          {
               get
               {
                    return _inReadMode;
               }
               set
               {
                    _inReadMode = value;
                    OnPropertyChanged(nameof(InReadMode));
                    OnPropertyChanged(nameof(ReadButtonBackground));
               }
          }

          public Brush ReadButtonBackground
          {
               get
               {
                    return InReadMode ? Brushes.Red : Brushes.Transparent;
               }
          }

          public ICommand AddAwayListCommand
          {
               get
               {
                    if (_addAwayListCommand == null) {
                         _addAwayListCommand = new RelayCommand((x) => {
                              DispatcherHelper.GetDispatcher().BeginInvoke(new Action(() => {
                                   AwayListRequestEventArgs e = new AwayListRequestEventArgs();
                                   if (EditMode == true) {
                                        e.PersonId = CurrentPerson.PersonId;
                                        AwayListMessageRequester.Instance.AwayListRequest(e);
                                   } else {
                                        MessageBox.Show("You must add the person before adding them to the away list", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                                   }
                              }), System.Windows.Threading.DispatcherPriority.Render);
                         });
                    }
                    return _addAwayListCommand;
               }
          }

          public ICommand CurrentDateClickedCommand
          {
               get
               {
                    if (_currentDateClickedCommand == null) {
                         _currentDateClickedCommand = new RelayCommand(x => SelectCurrentDateButtonClicked());
                    }
                    return _currentDateClickedCommand;
               }
          }

          public ICommand CopyCommand
          {
               get
               {
                    if (_copyCommand == null) {
                         _copyCommand = new RelayCommand(copyToClipboard);
                    }
                    return _copyCommand;
               }
          }

          public PersonViewModel CurrentPerson
          {
               get { return _currentPerson; }
               set
               {
                    _currentPerson = value;
                    OnPropertyChanged(nameof(CurrentPerson));
               }
          }

          //Used to copy values from
          public static PersonViewModel PreviousPerson { get; private set; } = null;

          public bool EditMode { get; private set; }

          public ICommand ExpirationClearCommand
          {
               get
               {
                    if (_expirationClearCommand == null) {
                         _expirationClearCommand = new RelayCommand(x => ExpirationClear());
                    }
                    return _expirationClearCommand;
               }
          }

          public ICommand GetOrientationCommand
          {
               get
               {
                    if (_getOrientationCommand == null) {
                         _getOrientationCommand = new RelayCommand(x => GetOrientation());
                    }
                    return _getOrientationCommand;
               }
          }

          public ICommand SaveCommand
          {
               get
               {
                    if (_saveCommand == null) {
                         _saveCommand = new RelayCommand(async x => await Save());
                    }
                    return _saveCommand;
               }
          }

          public ICommand RandomPinCommand
          {
               get
               {
                    if (_randomPinCommand == null) {
                         _randomPinCommand = new RelayCommand(x => RandomPinGeneration());
                    }
                    return _randomPinCommand;
               }
          }

          public ICommand ReadButtonCommand
          {
               get
               {
                    if (_readButtonCommand == null) {
                         _readButtonCommand = new RelayCommand(ReadButton);
                    }
                    return _readButtonCommand;
               }
          }

          public List<string> TestedByList => DataRepository.TestedByList;

          public bool VehiclesExpanded
          {
               get { return _vehiclesExpanded; }
               set
               {
                    _vehiclesExpanded = value;
                    OnPropertyChanged(nameof(VehiclesExpanded));
               }
          }

          public ICommand ShiftEntryCommand
          {
               get
               {
                    if (_shiftEntryCommand == null) {
                         _shiftEntryCommand = new RelayCommand(x => { OpenShiftEntry(); });
                    }
                    return _shiftEntryCommand;
               }
          }

          //NetboxLoadCommand
          public ICommand NetboxLoadCommand
          {
               get
               {
                    if (_netboxLoadCommand == null) {
                         _netboxLoadCommand = new RelayCommand(async x => {
                              openInNetbox = true;
                              await Save();
                         });
                    }
                    return _netboxLoadCommand;
               }
          }

          public ICommand RecallLastCommand
          {
               get
               {
                    if (_recallLastCommand == null) {
                         _recallLastCommand = new RelayCommand(x => {
                              if(PreviousPerson != null) {
                                   TraceEx.PrintLog("User clicked to Recall last");
                                   CurrentPerson.OrientationDate = PreviousPerson.OrientationDate;
                                   CurrentPerson.OrientationTestedBy = ConvertUtility.NullStringCopy(PreviousPerson.OrientationTestedBy);
                                   CurrentPerson.OrientationLevel = ConvertUtility.NullStringCopy(PreviousPerson.OrientationLevel);
                                   CurrentPerson.OldCompContact = ConvertUtility.NullStringCopy(PreviousPerson.OldCompContact);
                              }
                         });
                    }
                    return _recallLastCommand;
               }
          }

          public ICommand RecallLastCompanyCommand
          {
               get
               {
                    if (_recallLastCompanyCommand == null) {
                         _recallLastCompanyCommand = new RelayCommand(x => {
                              if (PreviousPerson != null) {
                                   TraceEx.PrintLog("User clicked to Recall last company");
                                   CurrentPerson.Company = ConvertUtility.NullStringCopy(PreviousPerson.Company);
                                   CurrentPerson.CredentialActive = PreviousPerson.CredentialActive;
                              }
                         });
                    }
                    return _recallLastCompanyCommand;
               }
          }


          #endregion Properties

          #region Methods

          //factory creation method
          public static AddPersonViewModel CreateAsync(PersonViewModel inputPerson, bool editMode)
          {
               var dispatcher = DispatcherHelper.GetDispatcher();

               AddPersonViewModel ret = new AddPersonViewModel();
               ret.IsBusy = true;

               ret.DisplayName = "Loading...";

               var t = dispatcher.InvokeAsync(new Action(async () => {
                    Benchmarker.Start("AddPersonViewModel::LoadAsync");
                    var p = await API_Interaction.LoadSinglePerson(inputPerson.PersonId);
                    if (p == null) {
                         MessageBox.Show("Could not connect to the API to load this person's data.  This means there may be a network connection issue.", "EditPerson Error", MessageBoxButton.OK, MessageBoxImage.Error);
                         ret.OnRequestClose();
                         return;
                    }

                    var loadedPerson = new PersonViewModel(p);
                    ret.Initialize(inputPerson, loadedPerson, editMode);
                    Benchmarker.Stop("AddPersonViewModel::LoadAsync");
               }), DispatcherPriority.Background);

               return ret;
          }

          public static AddPersonViewModel Create(PersonViewModel inputPerson, bool editMode)
          {
               AddPersonViewModel ret = new AddPersonViewModel();

               var loadedPerson = DataRepository.PersonDict.ContainsKey(inputPerson.PersonId) ? DataRepository.PersonDict[inputPerson.PersonId] : null;
               ret.Initialize(inputPerson, loadedPerson, editMode);

               return ret;
          }

          /// <summary>
          /// AddPerson
          ///  - takes care of all work on adding person to system
          /// </summary>
          /// <param name="p">Person to add</param>
          /// <returns>id of person added</returns>
          /// <param name = "writeOrientation">Should orientation data be written?</param>
          public async Task<string> AddPerson(PersonViewModel p, bool writeOrientation)
          {
               PersonViewModel apiPerson;
               bool isPersonEqual;
               string apiReturnedId;

               //try a couple of times since API call can randomly fail
               TraceEx.PrintLog($"AddPersonViewModel::AddPerson() {p}");
               TraceEx.PrintLog(p.ToStringLong());
               TraceEx.PrintLog("");

               IsBusy = true;
               apiReturnedId = await API_Interaction.AddPerson(p.InternalPerson, writeOrientation);
               p.InternalPerson.PersonId = apiReturnedId;
               p.InternalPerson.LastModified = DateTime.Now;
               ConvertNullVehicleData(p);
               await AddFob(p);
               await AddPin(p);

               var tuple = await GetAndCompareApiPerson(p);

               apiPerson = tuple.Item1;
               isPersonEqual = tuple.Item2;

               DataRepository.AddPerson(apiPerson);
               DBWritePerson(apiPerson.InternalPerson);

               //if it returns false, try resubmitting to
               if (isPersonEqual == false) {
                    Trace.TraceError("AddPerson: api returned different result - will try again with modify");
                    //try a modify operation
                    await ModifyPerson(p, true);
                    return p.PersonId;
               }

               TraceEx.PrintLog("AddPerson: save: added Person");

               string outputMessage = string.Empty;

               if (isPersonEqual) {
                    outputMessage = $"{p.LastName}, {p.FirstName} was added successfully";
               } else {
                    outputMessage = $"{p.LastName}, {p.FirstName} was added but there were issues.";
               }

               MainWindowViewModel.MainWindowInstance.PrintStatusText(outputMessage, Brushes.Black);

               return apiReturnedId;
          }

          /// <summary>
          /// ModifyPerson
          ///  - takes care of all work on modifying person in system
          /// </summary>
          /// <param name="p"></param>
          /// <param name = "writeOrientation">Should orientation data be written?</param>
          public async Task ModifyPerson(PersonViewModel p, bool writeOrientation)
          {
               PersonViewModel apiPerson;
               bool isPersonEqual;

               TraceEx.PrintLog($"AddPersonViewModel::ModifyPerson() {p}");
               TraceEx.PrintLog(p.ToStringLong());
               TraceEx.PrintLog("");

               //try multiple times incase api does not submit properly
               var tryCount = 1;
               do {
                    IsBusy = true;
                    p.InternalPerson.Deleted = false;
                    p.InternalPerson.LastModified = DateTime.Now;
                    await API_Interaction.ModifyPerson(p.InternalPerson, writeOrientation);
                    await AddFob(p);
                    await AddPin(p);

                    ConvertNullVehicleData(p);

                    var tuple = await GetAndCompareApiPerson(p);
                    apiPerson = tuple.Item1;
                    isPersonEqual = tuple.Item2;

                    //break out of loop if value is confirmed equal
                    if (isPersonEqual) {
                         break;
                    }

                    Trace.TraceWarning("ModifyPerson: API Return was not equal - will run again (if it hasn't already)");
               } while (tryCount-- > 0);

               //write actual value obtained from api to ensure it is the same
               DataRepository.ModifyPerson(apiPerson, refreshLists: true);
               DBWritePerson(apiPerson.InternalPerson);

               string outputMessage = string.Empty;

               if (isPersonEqual) {
                    outputMessage = $"{p.LastName}, {p.FirstName} was modified successfully";
               } else {
                    outputMessage = $"{p.LastName}, {p.FirstName} was modified but there were issues.";
               }

               MainWindowViewModel.MainWindowInstance.PrintStatusText(outputMessage, Brushes.Black);
          }

          //save to database
          public async Task Save(bool testing = false)
          {
               if (!ValidPerson()) {
                    return;
               }

               ControlsEnabled = false;
               
               TraceEx.PrintLog("AddPerson: save() start");
               TraceEx.PrintLog(String.Format("{0}, {1} {2}", CurrentPerson.LastName, CurrentPerson.FirstName, CurrentPerson.PersonId));
               if (!EditMode) {
                    CurrentPerson.ActivationDate = DateTime.Today;
               }

               bool writeOrientation = CurrentPerson.OrientationNumber != 0;

               Benchmarker.Start("AddPerson:Save()");

               try {
                    if (!EditMode) {
                         //see if person already is in Netbox

                         //get matches from repos
                         var query = from p in DataRepository.PersonDict.Values
                                     where p.FirstName == CurrentPerson.FirstName && p.LastName == CurrentPerson.LastName && p.IsNetbox == true
                                     orderby p.Deleted, p.OrientationNumber descending
                                     select p;

                         if (query.Any()) {
                              CurrentPerson.PersonId = query.First().PersonId;
                              MessageBoxResult mbres;
                              if (testing) {
                                   mbres = MessageBoxResult.Yes;
                              } else {
                                   mbres = MessageBox.Show("A person with this name exists in the Netbox database.  Do you wish to overwrite the existing person?", "Person Exists", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                              }
                              switch (mbres) {
                                   case MessageBoxResult.Yes:
                                        try {
                                             TraceEx.PrintLog($"Overwriting existing person {CurrentPerson}");

                                             //get any existing credentials from original person in system
                                             var apiPerson = await API_Interaction.LoadSinglePerson(CurrentPerson.PersonId);
                                             //remove fob
                                             if (apiPerson.FobNumber.GetValueOrDefault() != 0) {
                                                  await RemoveCredentials(apiPerson.FobNumber.GetValueOrDefault(), apiPerson.FobCredential, new PersonViewModel(apiPerson));
                                             }

                                             //remove pin
                                             if (apiPerson.PinNumber.GetValueOrDefault() != 0) {
                                                  await RemoveCredentials(apiPerson.PinNumber.GetValueOrDefault(), "26 bit Wiegand", new PersonViewModel(apiPerson));
                                             }
                                        }
                                        catch (Exception e) {
                                             TraceEx.PrintLog("Modifying existing person: There was a problem removing credentials: " + e.Message);
                                        }

                                        await ModifyPerson(CurrentPerson, writeOrientation);
                                        TraceEx.PrintLog("AddPerson: modified existing (on new)");
                                        Close();

                                        return;

                                   case MessageBoxResult.No:
                                        //break out to continue to write new person to API
                                        break;

                                   case MessageBoxResult.Cancel:
                                        MessageBox.Show("Person was not added");

                                        return;
                              }
                         }
                         //add new person
                         string id = null;

                         string messageToShowUser;
                         var tup = GetConfirmationMessage("add", isAdd: true);
                         messageToShowUser = tup.Item1;
                         ErrorLevels errorLevel = tup.Item2;

                         MessageBoxResult r;
                         if (testing) {
                              r = MessageBoxResult.Yes;
                         } else {
                              r = MessageBox.Show(messageToShowUser, "Warning", MessageBoxButton.YesNo, MessageBoxImage.Question);
                         }
                         if (r == MessageBoxResult.Yes) {
                              id = await AddPerson(CurrentPerson, writeOrientation);
                              CurrentPerson.PersonId = id;
                         } else {
                              return;
                         }
                         Close();
                    } else { // Edit Mode
                         string messageToShowUser = string.Empty;
                         var tup = GetConfirmationMessage("submit the changes to", isAdd: false);
                         messageToShowUser = tup.Item1;
                         var errorLevel = tup.Item2;

                         MessageBoxResult r;

                         //don't show if testing
                         if (testing) {
                              r = MessageBoxResult.Yes;
                         } else {
                              if (errorLevel == ErrorLevels.Critical) {
                                   r = MessageBox.Show(messageToShowUser, "Critical Warning", MessageBoxButton.YesNo, MessageBoxImage.Exclamation);
                              } else {
                                   r = MessageBox.Show(messageToShowUser, "Warning", MessageBoxButton.YesNo, MessageBoxImage.Question);
                              }
                         }

                         if (r == MessageBoxResult.Yes) {
                              await ModifyPerson(CurrentPerson, writeOrientation);
                              TraceEx.PrintLog("AddPerson: save: modified Person (edit mode)");

                              if (errorLevel == ErrorLevels.Critical) {
                                   Trace.TraceWarning("User selected to overwrite existing name with substantial changes.");
                              }

                              Close();
                         } else {
                              return;
                         }
                    }
               }
               catch (WebException e) {
                    Trace.TraceError("AddPerson: webException thrown: " + e.Message);
                    MessageBox.Show(String.Format("There was a problem connecting to the API ({0}).\nTry again.", e.Message), "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
               }
               catch (InvalidOperationException e) {
                    Trace.TraceError("AddPerson: InvalidOperationException thrown: " + e.Message);
                    MessageBox.Show("There was a problem adding this person to the NETBOX", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
               }
               finally {
                    Benchmarker.Stop("AddPerson:Save()");
                    DispatcherHelper.GetDispatcher().Invoke(() => App.Current.MainWindow.Focus());
                    ControlsEnabled = true;
                    IsBusy = false;
                    PreviousPerson = CurrentPerson;
               }
          }

          protected override void OnDispose()
          {
               //if (!EditMode) {
               TraceEx.PrintLog("AddPerson: OnDispose()");

               if (proxCard != null) {
                    proxCard.CardPresented -= x_CardPresented;
                    proxCard.Dispose();
                    proxCard = null;
               }
               //}
          }

          private static async Task<Tuple<PersonViewModel, bool>> GetAndCompareApiPerson(PersonViewModel p)
          {
               //get person from apid
               var apiPerson = new PersonViewModel(await API_Interaction.LoadSinglePerson(p.PersonId));

               bool compareResult = true;

               //compare to original
               if (apiPerson.InternalPerson.EqualsIgnoreVehicle(p.InternalPerson) == false) {
                    var compareText = apiPerson.InternalPerson.CompareAndPrint(p.InternalPerson, "Returned", "Submitted");
                    MessageBox.Show($"There may have been a problem copying the values to Netbox (the data you submitted is different from the data the API just returned).  Double check that the values are correct.\n\n{compareText}", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    Trace.TraceWarning("AddPerson: data mismatch");
                    Trace.Write(compareText);
                    TraceEx.PrintLog("AddPerson:: apiPerson does not match current person");
                    compareResult = false;
               }

               var tuple = Tuple.Create(apiPerson, compareResult);
               return tuple;
          }

          private static async Task RemoveCredentials(long number, string credentialName, PersonViewModel personWithCredential)
          {
               await API_Interaction.RemoveCredential(personWithCredential.PersonId, number, credentialName);
               await DataRepository.RemoveCredential(personWithCredential, number, credentialName);
          }

          private static string ConvertNull(string str)
          {
               if (str == null)
                    return "";
               else
                    return str;
          }

          /// <summary>
          /// Converts null data from vehicle to empty string so equals works properly
          /// </summary>
          /// <param name="p"></param>
          private static void ConvertNullVehicleData(PersonViewModel p)
          {
               foreach (var v in p.VehicleList) {
                    v.Color = ConvertNull(v.Color);
                    v.Make = ConvertNull(v.Make);
                    v.Model = ConvertNull(v.Model);
                    v.LicNum = ConvertNull(v.LicNum);
                    v.TagNum = ConvertNull(v.TagNum);
               }
          }

          private void copyToClipboard(object obj)
          {
               var outStr = $"//ra {CurrentPerson.Company.ToUpper()}//{CurrentPerson.FirstName.ToUpper()}//{CurrentPerson.LastName.ToUpper()}";
               System.Windows.Clipboard.SetText(outStr);
               TraceEx.PrintLog($"Copied info to clipboard: {outStr}");
          }

          private void Initialize(PersonViewModel person, PersonViewModel loadedPerson, bool editMode)
          {
               EditMode = editMode;
               DisplayName = DisplayVerb;

               proxCard = new ProxCard2.ReaderList();
               proxCard.CardPresented += x_CardPresented;
               proxCard.Refresh();

               OrigPerson = (PersonViewModel)loadedPerson?.Copy();
               if (OrigPerson == null) {
                    OrigPerson = person.Copy() as PersonViewModel;
               }

               if (!EditMode) {
                    //add one to orientation offset so autocomplete selects the correct number
                    CurrentPerson = new PersonViewModel(new Person());
                    CurrentPerson.OrientationNumber = DataRepository.GetLastOrientationNumber() + MainWindowViewModel.MainWindowInstance.GetAddPersonFormsOpen();
                    CurrentPerson.IsEdit = false;
               } else {
                    if (loadedPerson != null) {
                         CurrentPerson = (PersonViewModel)loadedPerson.Copy();
                         CurrentPerson.IsEdit = true;
                         CurrentPerson.OriginalPerson = OrigPerson;

                         //need to set orientation number to get property notification to take place so that error message doesn't
                         //appear while in edit mode
                         CurrentPerson.OrientationNumber = CurrentPerson.OrientationNumber;

                         //print information for debugging purposes
                         Trace.Write(CurrentPerson.ToStringLong());

                         string firstName = string.IsNullOrEmpty(CurrentPerson.FirstName) ? "" : $"{CurrentPerson.FirstName[0]}";

                         DisplayName = $"{firstName}. {CurrentPerson.LastName}";
                         OnPropertyChanged("DisplayName");
                    }
               }

               TraceEx.PrintLog("CurrentPerson.IsEditing set to true");
               CurrentPerson.IsEditing = true;

               TraceEx.PrintLog("Opening AddPerson");

               //since API does not clear vehicle list we have to handle it a bit strange by having empty entry
               if (CurrentPerson.VehicleList.Count == 1 && String.IsNullOrEmpty(CurrentPerson.VehicleList[0].LicNum)) {
                    CurrentPerson.VehicleList.Clear();
               }
               IsBusy = false;
          }

          //callback called by card reader
          private void x_CardPresented(string reader, byte[] cardData)
          {
               try {
                    Extractor.ScanCard(cardData, CurrentPerson, InReadMode);
               }
               catch (Exception ex) {
                    Trace.TraceError(ex.GetType().ToString() + " Error in CardPresented " + ex.Message);
               }
          }

          private void ReadButton(object obj)
          {
               if (obj?.GetType() == typeof(bool) && ((bool)obj == true)) {
                    InReadMode = true;
               } else {
                    InReadMode = !InReadMode;
               }
          }

          private void OpenShiftEntry()
          {
               var mainWindow = MainWindowViewModel.MainWindowInstance;
               ShiftEntriesViewModel vm = new ShiftEntriesViewModel(ShiftEntriesViewModel.TimeFrameSelection.Month);
               vm.IsUserSearch = false;
               vm.ShiftEntrySearchList.Add(new ShiftEntrySearch {
                    Company = OrigPerson?.Company,
                    LastName = OrigPerson?.LastName,
                    FirstName = OrigPerson?.FirstName
               });
               vm.ReportName = $"{OrigPerson?.LastName}, { OrigPerson?.FirstName}";
               vm.IsUserSearch = true;
               vm.InitializeQueryAndView("AddPersonVM");
               mainWindow.ShowView(vm);
          }

          private void NetboxLoad()
          {
               WebBrowserViewModel vm = WebBrowserViewModel.Instance;
               vm.Person = CurrentPerson;
               vm.Initialize(WebBrowserViewModel.BrowserPageMode.Person);
               MainWindowViewModel.MainWindowInstance.ShowView(vm);
          }

          private void Close()
          {
               this.OnRequestClose();
               if (openInNetbox) {
                    NetboxLoad();
               }
          }

          private async Task AddCredential(PersonViewModel inputPerson, long number, CredentialType credentialType)
          {
               string credentialName = string.Empty;
               string credentialTitle = string.Empty;
               long originalNumber = 0;
               string originalCredential = string.Empty;

               if (credentialType == CredentialType.Fob) {
                    credentialName = inputPerson.FobCredential;
                    originalNumber = OrigPerson.FobNumber;
                    originalCredential = OrigPerson.FobCredential;
                    credentialTitle = credentialName;
               } else {
                    credentialName = "26 bit Wiegand";
                    originalCredential = credentialName;
                    originalNumber = OrigPerson.PinNumber;
                    credentialTitle = "Pin";
               }

               try {
                    if (number != 0) {
                         PersonViewModel otherPersonWithCredential = null;
                         if (credentialType == CredentialType.Fob) {
                              otherPersonWithCredential = DataRepository.FindCredentialFob(number, credentialName);
                         } else {
                              otherPersonWithCredential = DataRepository.FindCredentialPin(number);
                         }

                         if (otherPersonWithCredential != null) {
                              //if it is the same person do nothing
                              if (!(otherPersonWithCredential.LastName == OrigPerson.LastName && otherPersonWithCredential.FirstName == OrigPerson.FirstName)) {
                                   //remove OTHER person's credentials
                                   TraceEx.PrintLog($"Removing other person credentials: {otherPersonWithCredential}");
                                   await RemoveCredentials(number, credentialName, otherPersonWithCredential);
                                   DBWritePerson(otherPersonWithCredential.InternalPerson);
                              }
                         }

                         //if this person has a previous fob/pin, delete it
                         if (EditMode && originalNumber != 0 && originalNumber != number) {
                              TraceEx.PrintLog($"Removing current person credentials: {OrigPerson}");
                              await RemoveCredentials(originalNumber, originalCredential, OrigPerson);
                         }
                         TraceEx.PrintLog($"Adding current person credentials: {inputPerson}");
                         if (!EditMode || (EditMode && originalNumber != number)) {
                              await API_Interaction.AddCredential(inputPerson.PersonId, number, credentialName);
                              DataRepository.AddCredential(inputPerson, number, credentialName);
                         }
                    } else { //handle zero (which means delete)
                         if (originalNumber != 0) {
                              await RemoveCredentials(originalNumber, originalCredential, inputPerson);
                         }
                    }
               }
               catch (InvalidOperationException) {
                    Trace.TraceWarning("InvalidOperationException Caught: Problem uploading {credentialTitle} information");
                    Close();
               }
          }

          private async Task AddFob(PersonViewModel p)
          {
               await AddCredential(p, p.FobNumber, CredentialType.Fob);
          }

          private async Task AddPin(PersonViewModel p)
          {
               await AddCredential(p, p.PinNumber, CredentialType.Pin);
          }

          private string CheckValidParamater(string paramName, string fullName)
          {
               string errorMsg = string.Empty;
               if (!string.IsNullOrEmpty(CurrentPerson[paramName])) {
                    errorMsg = GenerateErrorLine(fullName, CurrentPerson[paramName]);
               }
               return errorMsg;
          }

          /// <summary>
          /// Write person to database
          /// </summary>
          /// <param name="p"></param>
          private void DBWritePerson(Person p)
          {
               TraceEx.PrintLog("AddPerson: DBWritePerson");
               using (var db = NetboxDatabase.GetWriteInstance()) {
                    db.UpdatePerson(p);
               }
          }

          private void ExpirationClear()
          {
               CurrentPerson.ExpirationDate = DateTime.MinValue;
          }

          private string GenerateErrorLine(string paramName, string errorMessage)
          {
               return String.Format("\"{0}\" is invalid.  {1}\n", paramName, errorMessage);
          }

          /// <summary>
          /// Gets confirmation and error level based on type of edit
          /// </summary>
          /// <param name="verb">verb that will be used in description given to user</param>
          /// <param name="isAdd">is this an add or edit?</param>
          /// <returns></returns>
          private (string, ErrorLevels) GetConfirmationMessage(string verb, bool isAdd)
          {
               ErrorLevels errorLevels = ErrorLevels.Question;

               bool orientationExists = DataRepository.SearchOrientationNumber(CurrentPerson.OrientationNumber);
               var messageToShowUser = $"Do you want to {verb} this person?";
               if (OrigPerson.OrientationNumber != CurrentPerson.OrientationNumber && orientationExists && CurrentPerson.OrientationNumber != 0) {
                    messageToShowUser = "Warning: the selected orientation number already exists.  Do you still want to save the changes?";
               } else if (isAdd) {
                    //do nothing
               } else if (StringStatistics.CalculateStringDistance(OrigPerson.LastName, CurrentPerson.LastName) > 2
                      || StringStatistics.CalculateStringDistance(OrigPerson.FirstName, CurrentPerson.FirstName) > 2) {
                    messageToShowUser = "Warning: the name associated with this account is being changed!  \n\n" +
                                        $"'{OrigPerson.FullName}' is about to be changed to '{CurrentPerson.FullName}' \n\n" +
                                        "Are you sure you want to change the person's name associated with this account?\n\n" +
                                        "Generally it is a mistake to change an account from one name to another.  If this is for a minor spelling change you can disregard this message and click 'yes'.\n\n" +
                                        "Click 'Yes' to overwrite this person.  Click 'No' to cancel.";
                    errorLevels = ErrorLevels.Critical;
               }

               return (messageToShowUser, errorLevels);
          }

          /// <summary>
          /// GetOrientation
          ///  - set orientation number from DataRepos
          /// </summary>
          private void GetOrientation()
          {
               CurrentPerson.OrientationNumber = DataRepository.GetLastOrientationNumber() + MainWindowViewModel.MainWindowInstance.GetAddPersonFormsOpen();
               CurrentPerson.OrientationTestedBy = string.Empty;
               CurrentPerson.OrientationLevel = string.Empty;
          }

          private void ReportError(string errorMsg)
          {
               MessageBox.Show(errorMsg, "Invalid data", MessageBoxButton.OK, MessageBoxImage.Error);
          }

          private void SelectCurrentDateButtonClicked()
          {
               CurrentPerson.OrientationDate = DateTime.Today;
          }

          private void RandomPinGeneration()
          {
               try {
                    TraceEx.PrintLog("User clicked to generate random PIN");

                    Stopwatch sw = new Stopwatch();
                    sw.Start();
                    Utility.PinHistogram hist = new PinHistogram();

                    hist.IntitializeBins();

                    foreach (var v in DataRepository.PinList.Values) {
                         hist.SetPin(v.PinNumber);
                    }

                    var sortedList = hist.GetSortedList();

                    var rand = new Random();
                    int tryCount = 0;
                    int randPin;
                    do {
                         var randBin = rand.Next(0, 5);
                         var bin = sortedList[randBin];
                         var randLow = bin.StartNumber;
                         var randHigh = randLow + 100;

                         randPin = rand.Next(randLow, randHigh);
                         if (++tryCount > 10) {
                              MessageBox.Show("Random Pin Generation Failed. Could not find free PIN", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                              return;
                         }
                    } while (DataRepository.PinList.ContainsKey(randPin) == true);

                    CurrentPerson.PinNumber = randPin;
                    TraceEx.PrintLog($"Random Pin Generated: {randPin}");

                    sw.Stop();
                    TraceEx.PrintLog($"Random Pin Generation: took {sw.Elapsed.Milliseconds}ms");
                    TraceEx.PrintLog($"Total number of PINs is {hist.TotalPinCount}");
               }
               catch (Exception e) {
                    var errorMessage = $"Pin Random Generation failed {e.GetType()}: {e.Message}";
                    MessageBox.Show(errorMessage, "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    Trace.TraceError(errorMessage);
               }
          }

          private bool ValidPerson()
          {
               string errorMsg = string.Empty;
               errorMsg += CheckValidParamater("LastName", "Last Name");
               errorMsg += CheckValidParamater("FirstName", "First Name");
               errorMsg += CheckValidParamater("Company", "Company");
               errorMsg += CheckValidParamater("FobNumber", "Credential Number");
               errorMsg += CheckValidParamater("PinNumber", "PIN");
               errorMsg += CheckValidParamater("OrientationLevel", "Orientation Level");
               errorMsg += CheckValidParamater("OrientationTestedBy", "Tested By");

               if (!string.IsNullOrEmpty(errorMsg)) {
                    errorMsg += "\n";
                    errorMsg += "You will need to fix these issues before you are allowed to submit.";
                    ReportError(errorMsg);
               }

               return string.IsNullOrEmpty(errorMsg);
          }

          #endregion Methods
     }
}