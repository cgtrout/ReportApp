using ReportApp.Utility;
using System;
using System.Data.Linq.Mapping;

namespace ReportApp.Model
{
     /// <summary>
     /// Description of ShiftEntry.
     /// </summary>
     [Table(Name = "shiftentry")]
     public class ShiftEntry
     {
          #region Fields

          public bool IsChanged = false;

          #endregion Fields

          #region Properties

          [Column(Name = "hoursworked")]
          public float Hours { get; set; }

          [Column(Name = "inlogid")]
          public long InLogId { get; set; }

          [Column(Name = "inTime")]
          public DateTime InTime { get; set; }

          [Column(Name = "outlogid")]
          public long OutLogId { get; set; }

          [Column(Name = "outTime")]
          public DateTime OutTime { get; set; }

          [Column(Name = "personid")]
          public string PersonId { get; set; }

          [Column(Name = "shiftEntryId", IsPrimaryKey = true)]
          public long ShiftEntryId { get; set; }

          #endregion Properties

          #region Methods

          public void CopyFromOther(object obj)
          {
               if (obj == null)
                    throw new ArgumentNullException(nameof(obj));
               var other = (ShiftEntry)obj;
               ShiftEntryId = other.ShiftEntryId;
               PersonId = ConvertUtility.NullStringCopy(other.PersonId);
               InTime = other.InTime;
               OutTime = other.OutTime;
               InLogId = other.InLogId;
               OutLogId = other.OutLogId;
               Hours = other.Hours;
          }

          public override string ToString() => $"ShiftEntryId={ShiftEntryId} InLogId={InLogId} InTime={InTime} OutLogId={OutLogId} OutTime={OutTime} PersonId={PersonId}";

          #endregion Methods
     }
}