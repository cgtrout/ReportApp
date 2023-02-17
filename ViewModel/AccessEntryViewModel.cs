using ReportApp.Model;
using System;
using System.Windows.Input;

namespace ReportApp.ViewModel
{
     /// <summary>
     /// Single Access Entry
     /// </summary>
     public class AccessEntryViewModel
     {
          #region Fields

          private readonly AccessEntry entry;

          private PersonViewModel person;
          private ICommand _openLinkCommand;

          #endregion Fields

          #region Constructors

          public AccessEntryViewModel(PersonViewModel p, AccessEntry e)
          {
               person = p;
               entry = e;

               if (person == null) {
                    person = new PersonViewModel(new Person());
                    person.PersonId = null;
               }
          }

          #endregion Constructors

          #region Properties

          public PersonViewModel CurrentPerson => person;

          public DateTime DtTm
          {
               get { return entry.DtTm; }
          }

          public string InOut
          {
               get
               {
                    if (IsInEntry) {
                         return "IN";
                    } else {
                         return "OUT";
                    }
               }
          }

          public bool IsExpiredOrInvalid
          {
               get { return entry.Type == ReportApp.Model.TypeCode.InvalidAccess || entry.Type == ReportApp.Model.TypeCode.AccessNotCompleted || entry.Reason == ReasonCode.CardExpired; }
          }

          public bool IsInEntry => entry.ReaderKey == ReaderKeyEnum.AdminIn
                                || entry.ReaderKey == ReaderKeyEnum.CPIn
                                || entry.ReaderKey == ReaderKeyEnum.ControlIn
                                || entry.ReaderKey == ReaderKeyEnum.TestIn;

          public bool IsOutEntry => !IsInEntry;

          public bool IsPassback
          {
               get { return entry.Reason == ReasonCode.AntiPassbackViolation; }
          }

          //OrientationBorderMargin
          public int OrientationBorderMargin
          {
               get
               {
                    if (IsOutEntry) {
                         return 1;
                    } else {
                         return 0;
                    }
               }
          }

          public long LogId => entry.LogId;

          public string Name
          {
               get
               {
                    if (person == null) {
                         return string.Empty;
                    } else {
                         return person?.LastName + ", " + person?.FirstName;
                    }
               }
          }

          public string Reader
          {
               get
               {
                    switch (entry.ReaderKey) {
                         case ReaderKeyEnum.AdminIn:
                              return "Admin";

                         case ReaderKeyEnum.AdminOut:
                              return "Admin";

                         case ReaderKeyEnum.CPIn:
                              return "CP";

                         case ReaderKeyEnum.CPOut:
                              return "CP";

                         case ReaderKeyEnum.ControlIn:
                              return "Control Building";

                         case ReaderKeyEnum.ControlOut:
                              return "Control Building";

                         case ReaderKeyEnum.TestIn:
                              return "Test";

                         case ReaderKeyEnum.TestOut:
                              return "Test";
                    }
                    return "UNKNOWN_READER_ERROR";
               }
          }

          public string Reason
          {
               get
               {
                    string outString;

                    if (entry.Reason == 0) {
                         outString = string.Empty;
                    } else {
                         outString = entry.Reason.ToString();
                    }

                    return outString;
               }
          }

          public string Type
          {
               get { return entry.Type.ToString(); }
          }

          public ICommand OpenLinkCommand
          {
               get
               {
                    if (_openLinkCommand == null) {
                         _openLinkCommand = new RelayCommand((x) => {
                              if (entry.Reason == ReasonCode.CardNotInLocalDatabase || entry.Reason == ReasonCode.CardNotInS2NCDatabase) {
                                   WebBrowserViewModel vm = WebBrowserViewModel.Instance;
                                   vm.Param = this.LogId.ToString();
                                   vm.Param2 = this.DtTm.ToString("yyyy-MMM-dd HH:mm");
                                   vm.Initialize(WebBrowserViewModel.BrowserPageMode.AccessLog);

                                   MainWindowViewModel.MainWindowInstance.ShowView(vm);
                              }
                         });
                    }
                    return _openLinkCommand;
               }
          }

          #endregion Properties

          //public bool IsAboutToExpire => CurrentPerson == null ? false : CurrentPerson.IsAboutToExpire;
     }
}