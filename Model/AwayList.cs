using System;
using System.Data.Linq.Mapping;

namespace ReportApp.Model
{
     //AwayList - contains list of people that are currently away
     [Table(Name = "AwayList")]
     public class AwayList
     {
          #region Properties

          [Column(Name = "AwayListId", IsPrimaryKey = true)]
          public long AwayListId { get; set; }

          [Column(Name = "PersonId")]
          public string PersonId { get; set; }

          [Column(Name = "ReturnDate")]
          public DateTime ReturnDate { get; set; }

          [Column(Name = "Notes")]
          public string Notes { get; set; }

          #endregion Properties
     }
}