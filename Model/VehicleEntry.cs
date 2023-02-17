using System;
using System.Data.Linq.Mapping;
using static ReportApp.Utility.ConvertUtility;

namespace ReportApp.Model
{
     /// <summary>
     /// Description of VehicleEntry.
     /// </summary>
     [Table(Name = "vehicleentry")]
     public class VehicleEntry : Utility.CopyableObject
     {
          #region Constructors

          public VehicleEntry()
          {
               Deleted = false;
          }

          #endregion Constructors

          #region Properties

          [Column(Name = "color")]
          public string Color { get; set; } = string.Empty;

          [Column(Name = "deleted")]
          public bool Deleted { get; set; }

          [Column(Name = "entryid", IsPrimaryKey = true)]
          public long EntryId { get; set; }

          [Column(Name = "inid")]
          public long InId { get; set; }

          [Column(Name = "intime")]
          public DateTime InTime { get; set; }

          [Column(Name = "licnum")]
          public string LicNum { get; set; } = string.Empty;

          [Column(Name = "make")]
          public string Make { get; set; } = string.Empty;

          [Column(Name = "model")]
          public string Model { get; set; } = string.Empty;

          [Column(Name = "outid")]
          public long OutId { get; set; }

          [Column(Name = "outtime")]
          public DateTime OutTime { get; set; }

          [Column(Name = "personid")]
          public string PersonId { get; set; }

          [Column(Name = "tagnum")]
          public string TagNum { get; set; }

          #endregion Properties

          #region Methods

          public object Copy()
          {
               throw new NotImplementedException();
          }

          public void CopyFromOther(object obj)
          {
               var other = obj as VehicleEntry;
               this.Color = NullStringCopy(other.Color);
               this.Deleted = other.Deleted;
               this.EntryId = other.EntryId;
               this.InId = other.InId;
               this.InTime = other.InTime;
               this.LicNum = NullStringCopy(other.LicNum);
               this.Make = NullStringCopy(other.Make);
               this.Model = NullStringCopy(other.Model);
               this.OutId = other.OutId;
               this.OutTime = other.OutTime;
               this.PersonId = other.PersonId;
               this.TagNum = NullStringCopy(other.TagNum);
          }

          public override string ToString()
          {
               return $"EntryId={this.EntryId} PersonId={this.PersonId} {Make} {Model}";
          }

          #endregion Methods
     }
}