using System;
using System.Data.Linq.Mapping;

namespace ReportApp.Model
{
     /// <summary>
     /// Description of RollCall.
     /// </summary>
     [Table(Name = "rollcall")]
     public class RollCall
     {
          #region Properties

          [Column(Name = "dttm")]
          public DateTime DtTm { get; set; }

          [Column(Name = "personid")]
          public string PersonId { get; set; }

          [Column(Name = "reader")]
          public string Reader { get; set; }

          [Column(Name = "rollcallid", IsPrimaryKey = true)]
          public long RollCallId { get; set; }

          [Column(Name = "timein")]
          public double TimeIn { get; set; }

          #endregion Properties
     }
}