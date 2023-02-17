using System;
using System.Data.Linq.Mapping;

namespace ReportApp.Model
{
     [Table(Name = "PhoneInfo")]
     public class PhoneInfo
     {
          #region Properties

          [Column(Name = "CellNumber")]
          public string CellNumber { get; set; }

          //so we can easily sort without having to join later
          [Column(Name = "FullName")]
          public string FullName { get; set; }

          [Column(Name = "HomeNumber")]
          public string HomeNumber { get; set; }

          [Column(Name = "importedname")]
          public string ImportedName { get; set; }

          [Column(Name = "Pager")]
          public string Pager { get; set; }

          [Column(Name = "personid")]
          public string PersonId { get; set; }

          [Column(Name = "phoneinfoid", IsPrimaryKey = true)]
          public Int64 PhoneInfoId { get; set; }

          [Column(Name = "WorkNumber")]
          public string WorkNumber { get; set; }

          #endregion Properties
     }
}