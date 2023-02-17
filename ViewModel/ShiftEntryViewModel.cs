using ReportApp.Data;
using ReportApp.Model;
using ReportApp.Utility;
using System;

namespace ReportApp.ViewModel
{
     public class ShiftEntryViewModel : WorkspaceViewModel, CopyableObject
     {
          #region Constructors

          public ShiftEntryViewModel(ShiftEntry shiftEntry)
          {
               Entry = shiftEntry;
          }

          #endregion Constructors

          #region Properties

          public ShiftEntry Entry { get; private set; }

          public float Hours
          {
               get { return Entry.Hours; }
               set
               {
                    Entry.Hours = value;
                    OnPropertyChanged(nameof(Hours));
                    OnPropertyChanged(nameof(HoursText));
               }
          }

          public string HoursText
          {
               get
               {
                    return Hours.ToString("0.00");
               }
          }

          public long InLogId
          {
               get { return Entry.InLogId; }
               set
               {
                    Entry.InLogId = value;
                    OnPropertyChanged(nameof(InLogId));
               }
          }

          public DateTime InTime
          {
               get { return Entry.InTime; }
               set
               {
                    Entry.InTime = value;
                    OnPropertyChanged(nameof(InTime));
                    OnPropertyChanged(nameof(HoursText));
               }
          }

          public PersonViewModel LinkedPerson
          {
               get
               {
                    return DataRepository.PersonDict[PersonId];
               }
          }

          public long OutLogId
          {
               get { return Entry.OutLogId; }
               set
               {
                    Entry.OutLogId = value;
                    OnPropertyChanged(nameof(OutLogId));
               }
          }

          public DateTime OutTime
          {
               get { return Entry.OutTime; }
               set
               {
                    Entry.OutTime = value;
                    OnPropertyChanged(nameof(OutTime));
                    OnPropertyChanged(nameof(HoursText));
               }
          }

          public string PersonId
          {
               get { return Entry.PersonId; }
               set
               {
                    Entry.PersonId = value;
                    OnPropertyChanged(nameof(PersonId));
               }
          }

          public long ShiftEntryId
          {
               get { return Entry.ShiftEntryId; }
               set
               {
                    Entry.ShiftEntryId = value;
                    OnPropertyChanged(nameof(ShiftEntryId));
               }
          }

          #endregion Properties

          #region Methods

          public void CalculateHours()
          {
               Hours = (float)(OutTime.ToUniversalTime() - InTime.ToUniversalTime()).TotalHours;
               OnPropertyChanged(nameof(Hours));
               OnPropertyChanged(nameof(HoursText));
          }

          public object Copy()
          {
               ShiftEntryViewModel obj = new ShiftEntryViewModel(new ShiftEntry());

               obj.ShiftEntryId = ShiftEntryId;
               obj.PersonId = PersonId;
               obj.InTime = InTime;
               obj.OutTime = OutTime;
               obj.OutLogId = OutLogId;
               obj.Hours = Hours;

               return obj;
          }

          public void CopyFromOther(object obj)
          {
               var other = (ShiftEntryViewModel)obj;
               ShiftEntryId = other.ShiftEntryId;
               PersonId = string.Copy(other.PersonId);
               InTime = other.InTime;
               OutTime = other.OutTime;
               InLogId = other.InLogId;
               OutLogId = other.OutLogId;
               Hours = other.Hours;
          }

          //only compares on id which is primary key
          public override bool Equals(object obj)
          {
               if (obj == null || GetType() != obj.GetType()) {
                    return false;
               }
               var otherValue = (ShiftEntryViewModel)obj;
               return otherValue.ShiftEntryId == ShiftEntryId;
          }

          public override int GetHashCode()
          {
               return base.GetHashCode();
          }

          #endregion Methods
     }
}