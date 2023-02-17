using ReportApp.Data;
using ReportApp.Model;
using ReportApp.Utility;
using System;
using System.Windows.Media;

namespace ReportApp.ViewModel
{
     /// <summary>
     /// Description of VehicleEntryViewModel.
     /// </summary>
     public class VehicleEntryViewModel : WorkspaceViewModel
     {
          #region Constructors

          public VehicleEntryViewModel(VehicleEntry entry)
          {
               this.Entry = entry;

               //force trigger of person update
               this.PersonId = PersonId;
          }

          #endregion Constructors

          #region Properties

          public string Color
          {
               get
               {
                    return Entry.Color;
               }
               set
               {
                    Entry.Color = value;
                    OnPropertyChanged(nameof(Color));
               }
          }

          public string Company
          {
               get
               {
                    return Person.Company;
               }
          }

          public bool Deleted
          {
               get
               {
                    return Entry.Deleted;
               }
               set
               {
                    Entry.Deleted = value;
                    OnPropertyChanged(nameof(Deleted));
               }
          }

          //private PersonViewModel person;
          public VehicleEntry Entry { get; set; }

          public long EntryId
          {
               get
               {
                    return Entry.EntryId;
               }
               set
               {
                    Entry.EntryId = value;
                    OnPropertyChanged(nameof(EntryId));
               }
          }

          public string FullName
          {
               get { return Person.FullName; }
          }

          public string LastName => Person.LastName;
          public string FirstName => Person.FirstName;

          public bool HasNoOutTime
          {
               get
               {
                    return Entry.OutTime == DateTime.MinValue;
               }
          }

          public string HoursIn
          {
               get
               {
                    var val = (OutTime - InTime).TotalHours;

                    //if val is less than 0 it means outtime has not been set
                    if (val < 0) {
                         val = (DateTime.Now - InTime).TotalHours;
                    }

                    return val.ToString("0.#");
               }
          }

          public long InId
          {
               get
               {
                    return Entry.InId;
               }
               set
               {
                    Entry.InId = value;
                    OnPropertyChanged(nameof(InId));
               }
          }

          public DateTime InTime
          {
               get
               {
                    return Entry.InTime;
               }
               set
               {
                    Entry.InTime = value;
                    OnPropertyChanged(nameof(InTime));
                    OnPropertyChanged(nameof(InTimeString));
                    OnPropertyChanged(nameof(HoursIn));
               }
          }

          public string InTimeString
          {
               get
               {
                    return Entry.InTime.ToString("HH:mm");
               }
          }

          public bool IsPassEmpty
          {
               get
               {
                    return string.IsNullOrEmpty(TagNum);
               }
          }

          public string LicNumber
          {
               get
               {
                    return Entry.LicNum;
               }
               set
               {
                    Entry.LicNum = value;
                    OnPropertyChanged(nameof(LicNumber));
                    OnPropertyChanged(nameof(IsLicNumEmpty));
               }
          }

          public bool IsLicNumEmpty
          {
               get
               {
                    return string.IsNullOrEmpty(LicNumber);
               }
          }

          public string Make
          {
               get
               {
                    return Entry.Make;
               }
               set
               {
                    Entry.Make = value;
                    OnPropertyChanged(nameof(Make));
               }
          }

          public string Model
          {
               get
               {
                    return Entry.Model;
               }
               set
               {
                    Entry.Model = value;
                    OnPropertyChanged(nameof(Model));
               }
          }

          public long OutId
          {
               get
               {
                    return Entry.OutId;
               }
               set
               {
                    Entry.OutId = value;
                    OnPropertyChanged(nameof(OutId));
               }
          }

          public DateTime OutTime
          {
               get
               {
                    return Entry.OutTime;
               }
               set
               {
                    Entry.OutTime = value;
                    OnPropertyChanged(nameof(OutTime));
                    OnPropertyChanged(nameof(HoursIn));
                    OnPropertyChanged(nameof(OutTimeString));
                    OnPropertyChanged(nameof(HasNoOutTime));
               }
          }

          public string OutTimeString
          {
               get
               {
                    if (Entry.OutTime == DateTime.MinValue) {
                         return string.Empty;
                    }
                    return Entry.OutTime.ToString("HH:mm");
               }
          }

          public Brush PassSlotColor
          {
               get
               {
                    Color red_color = (Color)System.Windows.Media.ColorConverter.ConvertFromString("#DB3535");
                    Color blue_color = Colors.Blue;

                    Color selectedColor = Colors.Black;

                    if (Person.VehicleReader == 0) {
                         selectedColor = red_color;
                    } else {
                         selectedColor = blue_color;
                    }

                    var brush = new SolidColorBrush(selectedColor);
                    return brush;
               }
          }

          public PersonViewModel Person
          {
               get
               {
                    return DataRepository.PersonDict[Entry.PersonId];
               }
          }

          public string PersonId
          {
               get
               {
                    return Entry.PersonId;
               }
               set
               {
                    Entry.PersonId = value;
                    OnPropertyChanged(nameof(PersonId));
                    OnPropertyChanged(nameof(FullName));
                    OnPropertyChanged(nameof(Company));
               }
          }

          public string TagNum
          {
               get
               {
                    return Entry.TagNum;
               }
               set
               {
                    Entry.TagNum = value;
                    OnPropertyChanged(nameof(TagNum));
                    OnPropertyChanged(nameof(IsPassEmpty));
               }
          }

          #endregion Properties

          #region Methods

          public void CopyFrom(VehicleEntryViewModel vm)
          {
               EntryId = vm.EntryId;
               InTime = vm.InTime;
               OutTime = vm.OutTime;
               InId = vm.InId;
               OutId = vm.OutId;
               PersonId = ConvertUtility.NullStringCopy(vm.PersonId);
               TagNum = ConvertUtility.NullStringCopy(vm.TagNum);
               LicNumber = ConvertUtility.NullStringCopy(vm.LicNumber);
               Make = ConvertUtility.NullStringCopy(vm.Make);
               Model = ConvertUtility.NullStringCopy(vm.Model);
               Color = ConvertUtility.NullStringCopy(vm.Color);
               Deleted = vm.Deleted;
          }

          public void RefreshHoursIn()
          {
               OnPropertyChanged(nameof(HoursIn));
          }

          public override string ToString()
          {
               return $"EntryId={this.EntryId} inid={this.InId} {FullName} {LicNumber} {Make} {Model}";
          }

          #endregion Methods
     }
}