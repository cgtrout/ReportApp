using ReportApp.Data;
using ReportApp.Model;

namespace ReportApp.ViewModel
{
     public class PhoneItemViewModel : WorkspaceViewModel
     {
          #region Fields

          private PhoneInfo info;

          #endregion Fields

          #region Constructors

          public PhoneItemViewModel(PhoneInfo i)
          {
               info = i;
          }

          #endregion Constructors

          #region Properties

          public string CellNumber
          {
               get { return info.CellNumber; }
               set
               {
                    info.CellNumber = value;
                    OnPropertyChanged(nameof(CellNumber));
               }
          }

          public string FullName
          {
               get { return info.FullName; }
               set
               {
                    info.FullName = value;
                    OnPropertyChanged(nameof(FullName));
               }
          }

          public string HomeNumber
          {
               get { return info.HomeNumber; }
               set
               {
                    info.HomeNumber = value;
                    OnPropertyChanged(nameof(HomeNumber));
               }
          }

          public string ImportedName
          {
               get { return info.ImportedName; }
               set
               {
                    info.ImportedName = value;
                    OnPropertyChanged(nameof(ImportedName));
               }
          }

          public PersonViewModel LinkedPerson
          {
               get
               {
                    var dict = DataRepository.PersonDict;
                    if (dict.ContainsKey(PersonId)) {
                         return dict[PersonId];
                    } else {
                         return null;
                    }
               }
          }

          public string Pager
          {
               get { return info.Pager; }
               set
               {
                    info.Pager = value;
                    OnPropertyChanged(nameof(Pager));
               }
          }

          public string PersonId
          {
               get { return info.PersonId; }
               set
               {
                    info.PersonId = value;
                    info.FullName = LinkedPerson.FullName;
                    OnPropertyChanged(nameof(PersonId));
               }
          }

          public PhoneInfo UnderlyingItem
          {
               get { return info; }
          }

          public string WorkNumber
          {
               get { return info.WorkNumber; }
               set
               {
                    info.WorkNumber = value;
                    OnPropertyChanged(nameof(WorkNumber));
               }
          }

          #endregion Properties
     }
}