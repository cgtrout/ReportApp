using ReportApp.Data;
using ReportApp.Model;
using ReportApp.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Speech.Synthesis;
using System.Text;
using System.Windows.Media;

namespace ReportApp.ViewModel
{
     /// <summary>
     /// Simply exposes CurrentPerson with INotifyProperties
     /// </summary>
     public class PersonViewModel : ViewModelBase, System.ComponentModel.IDataErrorInfo, CopyableObject
     {
          #region Fields

          private static readonly SpeechSynthesizer synth = new SpeechSynthesizer();

          public bool AutoChangeCredential = true;
          private bool _companyPopupIsOpen = false;
          private string _companyPopupText = string.Empty;
          private bool _orientationPopupIsOpen = false;
          private string _orientationPopupText = "Test popup";
          private bool _pinPopupIsOpen;
          private string _pinPopupText;
          private bool _fobPopupIsOpen;
          private string _fobPopupText;
          private Brush _pinPopupColor;
          private Brush _fobPopupColor;

          #endregion Fields

          #region Constructors

          public PersonViewModel(Person p)
          {
               InternalPerson = p.Clone();
          }

          #endregion Constructors

          #region Properties

          public bool IsEditing { get; set; } = false;

          public DateTime ActivationDate
          {
               get
               {
                    return InternalPerson.ActivationDate.GetValueOrDefault();
               }
               set
               {
                    InternalPerson.ActivationDate = value;
                    OnPropertyChanged(nameof(ActivationDate));
               }
          }

          public string AccessLevel
          {
               get 
               { 
                    return InternalPerson.GetValidAccessLevel(); 
               }
               set
               {
                    if (!Person.IsValidAccessLevel(value)) {
                         throw new ArgumentException("PersonVM:AccessLevel invalid type given");
                    } else if (value == null) {
                         InternalPerson.AccessLevel = Person.DEFAULT_ACCESS;
                    } else {
                         InternalPerson.AccessLevel = value;
                    }

                    OnPropertyChanged(nameof(AccessLevel));
                    OnPropertyChanged(nameof(CredentialActive));
                    OnPropertyChanged(nameof(CredentialGroupName));
                    OnPropertyChanged(nameof(CredentialColor)); 
                    OnPropertyChanged(nameof(CredentialBorderThickness));
                    OnPropertyChanged(nameof(CredentialBorderColor));
               }
          }


          //Credential active is based on access level
          public bool CredentialActive
          {
               get 
               {
                    if (AccessLevel == "All Access") {
                         return true;
                    } else if (AccessLevel == "No Access") {
                         return false;
                    } else { 
                         Trace.TraceWarning("Warning -- access not set (Person::CredentialActive)");
                         return false;
                    }
               }

               set
               {
                    if(value == true) {
                         AccessLevel = "All Access";
                    } else {
                         AccessLevel = "No Access";
                    }
               }
          }


          //use this to show extra note if credentials are not active
          public string CredentialGroupName 
          { 
               get
               {
                    string retVal = "Credentials";

                    if(CredentialActive == false) {
                         retVal += " - NOT ACTIVE";
                    } else {
                         retVal += " - are active";
                    }

                    return retVal;
               }
          }

          public Brush CredentialColor
          {
               get
               {
                    if (CredentialActive) {
                         return new SolidColorBrush(Color.FromRgb(0, 150, 0));
                    } else {
                         return new SolidColorBrush(Color.FromRgb(220, 0, 0));
                    }
               }
          }

          public Brush CredentialBorderColor
          {
               get
               {
                    if (CredentialActive) {
                         return new SolidColorBrush(Color.FromRgb(170, 170, 170));
                    } else {
                         return new SolidColorBrush(Color.FromRgb(220, 0, 0));
                    }
               }
          }

          public double CredentialBorderThickness
          {
               get
               {
                    if (CredentialActive) {
                         return 1;
                    } else {
                         return 3;
                    }
               }
          }


          public string OldCompContact
          {
               get
               {
                    return InternalPerson.OldCompContact;
               }
               set
               {
                    InternalPerson.OldCompContact = value;
                    OnPropertyChanged(nameof(OldCompContact));
               }
          }

          public string Company
          {
               get
               {
                    return InternalPerson.Company;
               }
               set
               {
                    InternalPerson.Company = value;

                    if (IsEditing) {
                         //do analysis
                         var results = StringStatistics.GetMatches(value, DataRepository.CompanyList);
                         var outText = "Warning: company name is similar to:";
                         if (results.Count > 0) {
                              CompanyPopupIsOpen = true;
                              foreach (var x in results) {
                                   outText += "\n";
                                   outText += x.Word;
                              }
                              CompanyPopupText = outText;
                         } else {
                              //check to see if a new company
                              if (string.IsNullOrEmpty(value) == false && DataRepository.CompanyList.Contains(value) == false) {
                                   CompanyPopupText = "Warning: new company name.";
                                   CompanyPopupIsOpen = true;
                              } else {
                                   CompanyPopupIsOpen = false;
                              }
                         }
                    }

                    OnPropertyChanged(nameof(Company));

                    //need to recheck these for validation (incase of OldComp)
                    OnPropertyChanged(nameof(OrientationLevel));
                    OnPropertyChanged(nameof(OrientationTestedBy));
               }
          }

          //controls if popup is showing
          public bool CompanyPopupIsOpen
          {
               get
               {
                    return _companyPopupIsOpen;
               }
               set
               {
                    _companyPopupIsOpen = value;
                    OnPropertyChanged(nameof(CompanyPopupIsOpen));
               }
          }

          //text in the popup
          public string CompanyPopupText
          {
               get
               {
                    return _companyPopupText;
               }
               set
               {
                    _companyPopupText = value;
                    OnPropertyChanged(nameof(CompanyPopupText));
               }
          }

          //how long until this person expired - formatted string
          public string ExpireTimeString
          {
               get
               {
                    var timespan = ExpirationDate - DateTime.Today;
                    var days = timespan.TotalDays;
                    if (timespan.Ticks < 0) {
                         return "Warning: Orientation is expired";
                    } else {
                         var day_s_char = (days <= 1 && days != 0) ? "" : "s";
                         var daysString = days.ToString("#");

                         if (days == 0) {
                              daysString = "0";
                         }
                         var outString = $"Warning: Orientation expires in {daysString} day{day_s_char}";
                         return outString;
                    }
               }
          }

          //controls if popup is showing
          public bool PinPopupIsOpen
          {
               get
               {
                    return _pinPopupIsOpen;
               }
               set
               {
                    _pinPopupIsOpen = value;
                    OnPropertyChanged(nameof(PinPopupIsOpen));
               }
          }

          //text in the popup
          public string PinPopupText
          {
               get
               {
                    return _pinPopupText;
               }
               set
               {
                    _pinPopupText = value;
                    OnPropertyChanged(nameof(PinPopupText));
               }
          }

          public Brush PinPopupColor
          {
               get
               {
                    return _pinPopupColor;
               }
               set
               {
                    _pinPopupColor = value;
                    OnPropertyChanged(nameof(PinPopupColor));
               }
          }

          //controls if popup is showing
          public bool FobPopupIsOpen
          {
               get
               {
                    return _fobPopupIsOpen;
               }
               set
               {
                    _fobPopupIsOpen = value;
                    OnPropertyChanged(nameof(FobPopupIsOpen));
               }
          }

          public Brush FobPopupColor
          {
               get
               {
                    return _fobPopupColor;
               }
               set
               {
                    _fobPopupColor = value;
                    OnPropertyChanged(nameof(FobPopupColor));
               }
          }

          //text in the popup
          public string FobPopupText
          {
               get
               {
                    return _fobPopupText;
               }
               set
               {
                    _fobPopupText = value;
                    OnPropertyChanged(nameof(FobPopupText));
               }
          }

          public bool Deleted
          {
               get
               {
                    return InternalPerson.Deleted;
               }
               set
               {
                    InternalPerson.Deleted = value;
                    OnPropertyChanged(nameof(Deleted));
               }
          }

          public string EmployeeCategory
          {
               get
               {
                    return InternalPerson.EmployeeCategory;
               }

               set
               {
                    InternalPerson.EmployeeCategory = value;
                    OnPropertyChanged(nameof(EmployeeCategory));
               }
          }

          public string Error
          {
               get { return null; }
          }

          public DateTime ExpirationDate
          {
               get
               {
                    return InternalPerson.ExpirationDate.GetValueOrDefault();
               }
               set
               {
                    InternalPerson.ExpirationDate = value;
                    OnPropertyChanged(nameof(ExpirationDate));
                    OnPropertyChanged(nameof(ExpirationDateString));
                    OnPropertyChanged(nameof(IsExpired));
                    OnPropertyChanged(nameof(IsAboutToExpire));

                    OnPropertyChanged(nameof(OrientationDateString));
                    OnPropertyChanged(nameof(OrientationColorBorder));
                    OnPropertyChanged(nameof(OrientationColorBackground));
                    OnPropertyChanged(nameof(OrientationColorText));
                    OnPropertyChanged(nameof(OrientationDays));
                    OnPropertyChanged(nameof(DaysAway));
               }
          }

          public string ExpirationDateString
          {
               get
               {
                    //must use tostring or they won't equate (since tick values differ slightly)
                    if (InternalPerson.ExpirationDate.HasValue == false || InternalPerson.ExpirationDate.Value.Date.ToString() == DateTime.MinValue.Date.ToString()) {
                         return String.Empty;
                    } else {
                         return InternalPerson.ExpirationDate.Value.ToString("yyyy-MM-dd");
                    }
               }
               set
               {
                    ExpirationDate = DateTime.Parse(value);
               }
          }

          public string FirstName
          {
               get
               {
                    return InternalPerson.FirstName;
               }
               set
               {
                    InternalPerson.FirstName = value;
                    OnPropertyChanged(nameof(FirstName));
                    OnPropertyChanged(nameof(FullName));
               }
          }

          public string FobNumberString
          {
               get
               {
                    return FobNumber.ToString();
               }

               set
               {
                    long longVal;
                    if(Int64.TryParse(value, out longVal)) {
                         FobNumber = longVal;
                    } else {
                         FobNumber = 0;
                    }
               }
          }

          public long FobNumber
          {
               get
               {
                    if (InternalPerson.FobNumber.HasValue) {
                         return InternalPerson.FobNumber.Value;
                    } else {
                         return 0;
                    }
               }
               set
               {
                    if (AutoChangeCredential) {
                         if (value > 0 && value <= 10000) {
                              FobCredential = "Facility Code 210";
                         } else if (value >= 11005 && value <= 11104) {
                              FobCredential = "Facility Code 66";
                         } else if (value >= 19326 && value <= 19925) {
                              FobCredential = "Facility Code 209";
                         } else if (value == 0) {
                              FobCredential = "";
                         } else {
                              FobCredential = "Facility Code 193";
                         }
                    }

                    //check to see if exists
                    var fobs = DataRepository.FobList;
                    string key = FobCredential + value.ToString();
                    if (value != 0 && fobs.ContainsKey(key) && fobs[key].PersonId != PersonId) {
                         if (fobs.ContainsKey(key)) {
                              FobPopupText = $"'{fobs[key].FobCredential}' is in use by {fobs[key].FullName} of {fobs[key].Company}!";
                         } else {
                              FobPopupText = "FOB is in use by unknown!";
                         }

                         FobPopupIsOpen = true;
                    } else {
                         FobPopupText = string.Empty;
                         FobPopupIsOpen = false;
                    }

                    //extract 4 numbers and place in Pin field if it is not set
                    if (IsEditing) {
                         TraceEx.PrintLog("Extracting pin");
                         string keyFobStr = value.ToString();
                         if (keyFobStr.Length >= 4) {
                              PinNumber = long.Parse(keyFobStr.Substring(keyFobStr.Length - 4, 4));
                         } else {
                              PinNumber = long.Parse(keyFobStr);
                         }
                    }

                    InternalPerson.FobNumber = value;
                    OnPropertyChanged(nameof(FobNumber));
                    OnPropertyChanged(nameof(FobNumberString));
                    OnPropertyChanged(nameof(FobCredential));

                    ChangeColors();
               }
          }

          public string FobCredential
          {
               get
               {
                    return InternalPerson.FobCredential;
               }
               set
               {
                    InternalPerson.FobCredential = value;
                    OnPropertyChanged(nameof(FobCredential));
               }
          }

          public string FullName
          {
               get
               {
                    var p = InternalPerson;
                    return $"{p.LastName}, {p.FirstName}";
               }
          }

          public Person InternalPerson { get; set; }
          public bool IsAboutToExpire => IsExpiredOnDate(DateTime.Today.AddDays(7));

          public bool IsEdit { get; set; } = false;

          public bool IsExpired => IsExpiredOnDate(DateTime.Today);

          public bool IsNetbox
          {
               get
               {
                    if (InternalPerson.IsNetbox.HasValue) {
                         return InternalPerson.IsNetbox.Value;
                    } else {
                         return false;
                    }
               }
               set
               {
                    InternalPerson.IsNetbox = value;
                    OnPropertyChanged(nameof(IsNetbox));
               }
          }

          public string LastName
          {
               get
               {
                    return InternalPerson.LastName;
               }
               set
               {
                    InternalPerson.LastName = value;
                    OnPropertyChanged(nameof(LastName));
                    OnPropertyChanged(nameof(FullName));
               }
          }

          public DateTime OrientationDate
          {
               get
               {
                    return InternalPerson.OrientationDate;
               }
               set
               {
                    InternalPerson.OrientationDate = value;
                    ChangeExpiration();
                    OnPropertyChanged(nameof(OrientationDate));
                    OnPropertyChanged(nameof(OrientationDateString));
                    OnPropertyChanged(nameof(OrientationColorBorder));
                    OnPropertyChanged(nameof(OrientationColorBackground));
                    OnPropertyChanged(nameof(OrientationColorText));
                    OnPropertyChanged(nameof(OrientationDays));
               }
          }

          public string OrientationDateString
          {
               get
               {
                    if (InternalPerson.OrientationDate == DateTime.MinValue) {
                         return String.Empty;
                    } else {
                         return InternalPerson.OrientationDate.ToString("yyyy-MM-dd");
                    }
               }
          }

          public string OrientationLevel
          {
               get
               {
                    return InternalPerson.OrientationLevel;
               }
               set
               {
                    InternalPerson.OrientationLevel = value;
                    ChangeExpiration();
                    OnPropertyChanged(nameof(OrientationLevel));
               }
          }

          public long OrientationNumber
          {
               get
               {
                    return InternalPerson.OrientationNumber;
               }
               set
               {
                    InternalPerson.OrientationNumber = value;

                    if (IsEditing) {
                         if (OrientationNumber != 0) {
                              if (CheckDuplicateOrientation() && !(IsEdit && OrientationNumber == OriginalPerson.OrientationNumber)) {
                                   OrientationPopupIsOpen = true;
                                   OrientationPopupText = "Orientation already exists!";
                              } else if (WrongDigitCount()) {
                                   OrientationPopupIsOpen = true;
                                   OrientationPopupText = "Wrong number of digits!";
                              } else {
                                   OrientationPopupIsOpen = false;
                              }
                         } else {
                              OrientationPopupIsOpen = false;
                              
                              //if set to zero, reset error checking on these fields since they don't need to be filled in
                              OnPropertyChanged(nameof(OrientationTestedBy));
                              OnPropertyChanged(nameof(OrientationLevel));
                         }
                    }

                    OnPropertyChanged(nameof(OrientationNumber));
               }
          }

          //controls if popup is showing
          public bool OrientationPopupIsOpen
          {
               get
               {
                    return _orientationPopupIsOpen;
               }
               set
               {
                    _orientationPopupIsOpen = value;
                    OnPropertyChanged(nameof(OrientationPopupIsOpen));
               }
          }

          //text in the popup
          public string OrientationPopupText
          {
               get
               {
                    return _orientationPopupText;
               }
               set
               {
                    _orientationPopupText = value;
                    OnPropertyChanged(nameof(OrientationPopupText));
               }
          }

          public string OrientationTestedBy
          {
               get
               {
                    return InternalPerson.OrientationTestedBy;
               }
               set
               {
                    InternalPerson.OrientationTestedBy = value;
                    OnPropertyChanged(nameof(OrientationTestedBy));
               }
          }

          public double DaysAway
          {
               get
               {
                    //if expiration date is not set then we want them to be sorted with eachother
                    if (ExpirationDate.Year == 1 && ExpirationDate.Month == 1 && ExpirationDate.Day == 1) {
                         //return large number so that users that don't expire show last in sort order
                         return 100000;
                    }
                    var daysAway = (ExpirationDate - DateTime.Today).TotalDays;
                    if (daysAway < 0) {
                         daysAway = 0;
                    }
                    return daysAway;
               }
          }

          //OrientationColor
          public Brush OrientationColorBorder
          {
               get
               {
                    var c = CalcOrientationColor();
                    if (ExpirationDate == DateTime.MinValue || DaysAway > 60) {
                         return new SolidColorBrush(Colors.Transparent);
                    }

                    byte darknessOffset = 75;
                    c.R = c.R - darknessOffset < 0 ? (byte)0 : (byte)(c.R - darknessOffset);
                    c.G = c.G - darknessOffset < 0 ? (byte)0 : (byte)(c.G - darknessOffset);
                    c.B = c.B - darknessOffset < 0 ? (byte)0 : (byte)(c.B - darknessOffset);
                    return new SolidColorBrush(c);
               }
          }

          public Brush OrientationColorBackground
          {
               get
               {
                    if (ExpirationDate == DateTime.MinValue || DaysAway > 60) {
                         return new SolidColorBrush(Colors.Transparent);
                    }
                    var c = CalcOrientationColor();
                    var endColor = c;
                    endColor.A = 150;
                    var brush = new RadialGradientBrush(endColor, c);
                    brush.RadiusX *= 1.5;
                    brush.RadiusY *= 1.5;
                    return brush;
               }
          }

          public Brush OrientationColorText
          {
               get
               {
                    var c = CalcOrientationColor();
                    byte darknessOffset = 150;
                    c.R = c.R - darknessOffset < 0 ? (byte)0 : (byte)(c.R - darknessOffset);
                    c.G = c.G - darknessOffset < 0 ? (byte)0 : (byte)(c.G - darknessOffset);
                    c.B = c.B - darknessOffset < 0 ? (byte)0 : (byte)(c.B - darknessOffset);
                    return new SolidColorBrush(c);
               }
          }

          /// <summary>
          /// Days away until exp
          /// </summary>
          public string OrientationDays
          {
               get
               {
                    string retString = string.Empty;
                    var daysAway = DaysAway;
                    if (ExpirationDate != DateTime.MinValue && daysAway <= 60 && daysAway != 0) {
                         retString = daysAway.ToString();
                    } else if (ExpirationDate != DateTime.MinValue && daysAway == 0) {
                         retString = "Ex";
                    }

                    return retString;
               }
          }

          //link to original person (if editing)
          public PersonViewModel OriginalPerson { get; set; }

          public string PersonId
          {
               get
               {
                    return InternalPerson.PersonId;
               }
               set
               {
                    InternalPerson.PersonId = value;
                    OnPropertyChanged(nameof(PersonId));
               }
          }

          public string PhoneInfo
          {
               get
               {
                    PhoneDatabase db = new PhoneDatabase(PathSettings.Default.PhoneDatabasePath); ;
                    PhoneInfo info = db.GetPhone(PersonId);

                    if (info == null) {
                         return string.Empty;
                    } else {
                         return GeneratePhoneString(info);
                    }
               }
          }

          public string PinNumberString
          {
               get
               {
                    return PinNumber.ToString();
               }

               set
               {
                    long longVal;
                    if (Int64.TryParse(value, out longVal)) {
                         PinNumber = longVal;
                    } else {
                         PinNumber = 0;
                    }
               }
          }

          public long PinNumber
          {
               get
               {
                    if (InternalPerson.PinNumber.HasValue) {
                         return InternalPerson.PinNumber.Value;
                    } else {
                         return 0;
                    }
               }
               set
               {
                    //check to see if exists
                    var pins = DataRepository.PinList;
                    if (value != 0 && pins.ContainsKey(value) && pins[value].PersonId != PersonId) {
                         if (pins.ContainsKey(value)) {
                              PinPopupText = $"PIN is in use by {pins[value].FullName} of {pins[value].Company}!";
                         } else {
                              PinPopupText = "PIN is in use by unknown!";
                         }

                         PinPopupIsOpen = true;
                    } else {
                         PinPopupText = string.Empty;
                         PinPopupIsOpen = false;
                    }

                    InternalPerson.PinNumber = value;
                    OnPropertyChanged(nameof(PinNumber));
                    OnPropertyChanged(nameof(PinNumberString));

                    ChangeColors();
               }
          }

          public List<Vehicle> VehicleList
          {
               get
               {
                    return InternalPerson.VehicleList;
               }
               set
               {
                    InternalPerson.VehicleList = value;
                    OnPropertyChanged(nameof(VehicleList));
               }
          }

          public bool VehiclesActivated
          {
               get
               {
                    if (InternalPerson.VehiclesActivated.HasValue) {
                         return InternalPerson.VehiclesActivated.Value;
                    } else {
                         return false;
                    }
               }
               set
               {
                    InternalPerson.VehiclesActivated = value;
                    OnPropertyChanged(nameof(VehiclesActivated));
               }
          }

          public int VehicleReader
          {
               get
               {
                    if (InternalPerson.VehicleReader.HasValue) {
                         return InternalPerson.VehicleReader.Value;
                    } else {
                         return 0;
                    }
               }

               set
               {
                    InternalPerson.VehicleReader = value;
                    OnPropertyChanged(nameof(VehicleReader));
               }
          }

          #endregion Properties

          #region Indexers

          public string this[string columnName]
          {
               get
               {
                    switch (columnName) {
                         case "LastName":
                              if (string.IsNullOrWhiteSpace(LastName)) {
                                   return "Must not be empty.";
                              }
                              break;

                         case "FirstName":
                              if (string.IsNullOrWhiteSpace(FirstName)) {
                                   return "Must not be empty.";
                              }
                              break;

                         case "Company":
                              if (string.IsNullOrWhiteSpace(Company)) {
                                   return "Must not be empty.";
                              } else if (Company.Any(IsPunc)) {
                                   return "Can't contain any puctuation (< > : \" | ? *)";
                              }
                              break;

                         case "FobNumber":
                              if (FobNumber < 0) {
                                   return "Must be 0 or greater.  Set to 0 if you don't want to add a Fob Key";
                              }
                              break;

                         case "PinNumber":
                              if (PinNumber < 0) {
                                   return "Must be 0 or greater.  Set to 0 if you don't want to add a Pin.";
                              }
                              break;

                         case "OrientationLevel":
                              if (OrientationNumber != 0 && string.IsNullOrWhiteSpace(OrientationLevel)) {
                                   return "Must not be empty.";
                              }
                              break;

                         case "OrientationTestedBy":
                              if (OrientationNumber != 0 && string.IsNullOrWhiteSpace(OrientationTestedBy)) {
                                   return "Must not be empty.";
                              }
                              break;
                    }
                    return string.Empty;
               }
          }

          #endregion Indexers

          #region Methods

          /// <summary>
          /// ConvertList - creates list of PersonViewModel from List of Person
          /// </summary>
          /// <param name="personList"></param>
          /// <returns>List of PersonViewModel</returns>
          public static List<PersonViewModel> ConvertList(List<Person> personList)
          {
               var returnList = new List<PersonViewModel>();
               foreach (var p in personList) {
                    var pvm = new PersonViewModel(p);
                    returnList.Add(pvm);
               }
               return returnList;
          }

          //get deep copy
          public object Copy()
          {
               var p = new PersonViewModel(new Person());
               p.FirstName = ConvertUtility.NullStringCopy(FirstName);
               p.LastName = ConvertUtility.NullStringCopy(LastName);
               p.PersonId = ConvertUtility.NullStringCopy(PersonId);
               p.Company = ConvertUtility.NullStringCopy(Company);
               p.Deleted = Deleted;
               p.OrientationNumber = OrientationNumber;
               p.OrientationDate = OrientationDate;
               p.OrientationLevel = ConvertUtility.NullStringCopy(OrientationLevel);
               p.OrientationTestedBy = ConvertUtility.NullStringCopy(OrientationTestedBy);
               p.ActivationDate = ActivationDate;
               p.ExpirationDate = ExpirationDate;
               p.OldCompContact = ConvertUtility.NullStringCopy(OldCompContact);
               p.VehiclesActivated = VehiclesActivated;
               p.FobNumber = FobNumber;
               p.FobCredential = ConvertUtility.NullStringCopy(FobCredential);
               p.PinNumber = PinNumber;
               p.IsNetbox = IsNetbox;

               p.EmployeeCategory = EmployeeCategory;
               p.VehicleReader = VehicleReader;

               //copy vehicles
               foreach (var v in VehicleList) {
                    p.VehicleList.Add(v.Copy() as Vehicle);
               }

               p.AccessLevel = AccessLevel;

               return p;
          }

          //copy values from other person into THIS instance
          public void CopyFromOther(object obj)
          {
               var p = obj as PersonViewModel;

               TraceEx.PrintLog($"CopyFromOther PersonViewModel this={this} other={p}");

               FirstName = ConvertUtility.NullStringCopy(p.FirstName);
               LastName = ConvertUtility.NullStringCopy(p.LastName);
               PersonId = ConvertUtility.NullStringCopy(p.PersonId);
               Company = ConvertUtility.NullStringCopy(p.Company);
               Deleted = p.Deleted;
               OrientationNumber = p.OrientationNumber;
               OrientationDate = p.OrientationDate;
               OrientationLevel = ConvertUtility.NullStringCopy(p.OrientationLevel);
               OrientationTestedBy = ConvertUtility.NullStringCopy(p.OrientationTestedBy);
               ActivationDate = p.ActivationDate;
               ExpirationDate = p.ExpirationDate;
               InternalPerson.LastModified = p.InternalPerson.LastModified;

               OldCompContact = p.OldCompContact;
               VehiclesActivated = p.VehiclesActivated;
               FobNumber = p.FobNumber;
               FobCredential = ConvertUtility.NullStringCopy(ConvertUtility.ConvertStrHandleNull(p.FobCredential));
               PinNumber = p.PinNumber;
               IsNetbox = p.IsNetbox;

               EmployeeCategory = p.EmployeeCategory;
               VehicleReader = p.VehicleReader;

               VehicleList.Clear();
               foreach (var v in p.VehicleList) {
                    VehicleList.Add(v.Copy() as Vehicle);
               }
          }

          public override bool Equals(Object obj)
          {
               if (obj == null || GetType() != obj.GetType()) {
                    return false;
               }
               return InternalPerson.Equals((obj as PersonViewModel).InternalPerson);
          }

          public override int GetHashCode()
          {
               return base.GetHashCode();
          }

          public override string ToString() => $"{PersonId} {FullName} {Company} {FobNumber} {PinNumber}";

          public string ToStringLong()
          {
               StringBuilder sb = new StringBuilder();
               sb.AppendLine(GetParamLine("LastName", LastName));
               sb.AppendLine(GetParamLine("FirstName", FirstName));
               sb.AppendLine(GetParamLine("Company", LastName));
               sb.AppendLine(GetParamLine("OrientationNumber", OrientationNumber));
               sb.AppendLine(GetParamLine("VehActivated", VehiclesActivated));
               sb.AppendLine("VEHICLES");
               sb.AppendLine("----------");
               foreach (var v in VehicleList) {
                    sb.AppendLine(GetParamLine("LicNum", v.LicNum));
               }
               return sb.ToString();
          }

          private static string ExtractPhoneElement(string element, string outstr, string name)
          {
               if (!string.IsNullOrEmpty(element)) {
                    outstr += $"{name.ToUpper()}: {element} ";
               }

               return outstr;
          }

          private Color CalcOrientationColor()
          {
               byte r = 255, g = 255, b = 255, a = 255;
               var daysAway = DaysAway;
               if (ExpirationDate != DateTime.MinValue && daysAway <= 60) {
                    Color red = Colors.Red;
                    Color yellow = Colors.Yellow;

                    var fraction = ColorCalc.CalculateFraction(daysAway, 0, 60);
                    ColorCalc.CalculateColors(red, yellow, 1.0f, 1.0f, fraction, out r, out b, out g, out a);
               }
               return new Color() { R = r, G = g, B = b, A = 255 };
          }

          //change colors of popup boxes
          private void ChangeColors()
          {
               string fobKey = FobCredential + FobNumber.ToString();

               bool namesAreSame = (PinNumber != 0 && FobNumber != 0) && (DataRepository.PinList.ContainsKey(PinNumber) && DataRepository.FobList.ContainsKey(fobKey))
                    && (DataRepository.PinList[PinNumber].PersonId == DataRepository.FobList[fobKey].PersonId);
               bool onlyPin = (FobPopupIsOpen == false && PinPopupIsOpen == true);
               bool onlyFob = (FobPopupIsOpen == true && PinPopupIsOpen == false);

               if (namesAreSame) {
                    PinPopupColor = Brushes.Yellow;
                    FobPopupColor = Brushes.Yellow;
               } else if (onlyPin) {
                    PinPopupColor = Brushes.Red;

                    TraceEx.PrintLog("Showing user RED warning");

                    if (synth.State != SynthesizerState.Speaking) {
                         synth.SpeakAsync("Warning.. PIN Conflict");
                    }
               } else if (onlyFob) {
                    FobPopupColor = Brushes.Yellow;
               } else if (!namesAreSame) {
                    PinPopupColor = Brushes.Red;
                    FobPopupColor = Brushes.Red;
               }
          }

          private bool IsPunc(char arg)
          {
               if (arg == '<'
               || arg == '>'
               || arg == ':'
               || arg == '|'
               || arg == '?'
               || arg == '*'
               || arg == '"') {
                    return true;
               } else {
                    return false;
               }
          }

          private void ChangeExpiration()
          {
               if (IsEditing && !string.IsNullOrEmpty(OrientationLevel)) {
                    if(OrientationDate == DateTime.MinValue) {
                         return;
                    }
                    if (OrientationLevel.Contains("1")) {
                         ExpirationDate = OrientationDate.AddMonths(6);
                    } else if (OrientationLevel.Contains("2")) {
                         ExpirationDate = OrientationDate.AddYears(1);
                    }
               }
          }

          private bool CheckDuplicateOrientation()
          {
               return DataRepository.SearchOrientationNumber(OrientationNumber);
          }

          private string GeneratePhoneString(PhoneInfo info)
          {
               string outstr = string.Empty;

               outstr = ExtractPhoneElement(info.WorkNumber, outstr, "Work");
               outstr = ExtractPhoneElement(info.HomeNumber, outstr, "Home");
               outstr = ExtractPhoneElement(info.CellNumber, outstr, "Cell");
               outstr = ExtractPhoneElement(info.Pager, outstr, "Pager");

               return outstr;
          }

          private string GetParamLine<T>(string paramName, T val) => $"{paramName}: {val}";

          private bool IsExpiredOnDate(DateTime comparison)
          {
               //if exp date not set then return false
               if (ExpirationDate == DateTime.MinValue || OrientationNumber == 0) {
                    return false;
               }
               if (comparison >= ExpirationDate) {
                    return true;
               }
               return false;
          }

          private bool WrongDigitCount()
          {
               var largestOrientation = DataRepository.GetLastOrientationNumber().ToString();
               var orientationNumber = OrientationNumber.ToString();

               if (orientationNumber.Length != largestOrientation.Length) {
                    return true;
               } else {
                    return false;
               }
          }

          #endregion Methods
     }
}