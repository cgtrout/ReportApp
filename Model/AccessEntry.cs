using ReportApp.Utility;
using System;
using System.Data.Linq.Mapping;

namespace ReportApp.Model
{
     public enum ReaderKeyEnum
     {
          AdminIn = 1,
          AdminOut = 2,
          ControlIn = 3,
          ControlOut = 4,
          CPOut = 5,
          CPIn = 6,
          TestIn = 7,
          TestOut = 8
     }

     public enum ReasonCode
     {
          CardNotInLocalDatabase = 1,
          CardNotInS2NCDatabase = 2,
          WrongTime = 3,
          WrongLocation = 4,
          CardMisread = 5,
          TailgateViolation = 6,
          AntiPassbackViolation = 7,
          CardExpired = 14,
          CardBitLengthMismatch = 15,
          WrongDay = 16,
          ThreatLevel = 17,
          InvalidPassback = 18 //CT code to represent netbox passback later calculated to be a invalid passback
     };

     public enum TypeCode
     {
          ValidAccess = 1,
          InvalidAccess = 2,
          ElevatorValidAccess = 37,
          ElevatorInvalidAccess = 38,
          AccessNotCompleted = 64
     };

     /// <summary>
     /// Description of AccessEntry.
     /// </summary>
     [Table(Name = "AccessEntry")]
     public class AccessEntry : Utility.CopyableObject
     {
          #region Fields

          private bool _shiftEntryProcessed = false;

          #endregion Fields

          #region Properties

          [Column(Name = "dttm")]
          public DateTime DtTm { get; set; }

          [Column(Name = "accessentryid", IsPrimaryKey = true)]
          public long LogId { get; set; }

          [Column(Name = "personid")]
          public string PersonId { get; set; }

          [Column(Name = "portalkey")]
          public long PortalKey { get; set; }

          [Column(Name = "reader")]
          public string Reader { get; set; }

          [Column(Name = "readerkey")]
          public ReaderKeyEnum ReaderKey { get; set; }

          [Column(Name = "reason")]
          public ReasonCode Reason { get; set; }

          [Column(Name = "shiftentryprocessed")]
          public bool? ShiftEntryProcessed
          {
               get { return _shiftEntryProcessed; }
               set
               {
                    if (value.HasValue) {
                         _shiftEntryProcessed = value.Value;
                    } else {
                         _shiftEntryProcessed = true;
                    }
               }
          }

          [Column(Name = "type")]
          public TypeCode Type { get; set; }

          #endregion Properties

          #region Methods

          public object Copy()
          {
               var newObj = new AccessEntry {
                    DtTm = this.DtTm,
                    LogId = this.LogId,
                    PersonId = ConvertUtility.NullStringCopy(this.PersonId),
                    PortalKey = this.PortalKey,
                    Reader = ConvertUtility.NullStringCopy(this.Reader),
                    ReaderKey = this.ReaderKey,
                    Reason = this.Reason,
                    ShiftEntryProcessed = this.ShiftEntryProcessed,
                    Type = this.Type
               };

               return newObj;
          }

          public void CopyFromOther(object obj)
          {
               throw new NotImplementedException();
          }

          public override string ToString()
          {
               return $"LogId={LogId} PersonId={PersonId} reader={Reader} dttm={DtTm} reason={Reason}";
          }

          #endregion Methods
     }
}