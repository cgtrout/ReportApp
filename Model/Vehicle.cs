using ReportApp.Utility;
using System.Data.Linq.Mapping;
using static ReportApp.Utility.ConvertUtility;

namespace ReportApp.Model
{
     /// <summary>
     /// Used to hold information for a single vehicle
     /// </summary>
     [Table(Name = "vehicle")]
     public class Vehicle : CopyableObject
     {
          #region Constructors

          public Vehicle()
          {
          }

          #endregion Constructors

          #region Properties

          [Column(Name = "color")]
          public string Color { get; set; }

          [Column(Name = "licnum")]
          public string LicNum { get; set; }

          [Column(Name = "make")]
          public string Make { get; set; }

          [Column(Name = "model")]
          public string Model { get; set; }

          [Column(Name = "personid")]
          public string PersonId { get; set; }

          public string TagNum { get; set; }

          [Column(Name = "vehicleid", IsPrimaryKey = true)]
          public int VehicleId { get; set; }

          #endregion Properties

          #region Methods

          public object Copy()
          {
               Vehicle v = new Vehicle();

               v.Color = NullStringCopy(Color);
               v.LicNum = NullStringCopy(LicNum);
               v.Make = NullStringCopy(Make);
               v.Model = NullStringCopy(Model);
               v.PersonId = NullStringCopy(PersonId);
               v.TagNum = NullStringCopy(TagNum);
               v.VehicleId = VehicleId;

               return v;
          }

          public void CopyFromOther(object obj)
          {
               var v = obj as Vehicle;

               Color = NullStringCopy(v.Color);
               LicNum = NullStringCopy(v.LicNum);
               Make = NullStringCopy(v.Make);
               Model = NullStringCopy(v.Model);
               PersonId = NullStringCopy(v.PersonId);
               TagNum = NullStringCopy(v.TagNum);
               VehicleId = v.VehicleId;
          }

          public override bool Equals(object obj)
          {
               if (obj == null || GetType() != obj.GetType()) {
                    return false;
               }
               var other = obj as Vehicle;
               return Color == other.Color &&
                      Make == other.Make &&
                      Model == other.Model &&
                      LicNum == other.LicNum;
          }

          public override int GetHashCode()
          {
               return base.GetHashCode();
          }

          public override string ToString()
          {
               return $"{LicNum} {Color} {Make} {Model}";
          }

          #endregion Methods
     }
}