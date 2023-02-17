using ReportApp.Utility;
using System;
using System.Collections.Generic;
using System.Data.Linq.Mapping;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace ReportApp.Model
{
     /// <summary>
     /// Description of Person.
     /// </summary>
     [Table(Name = "person")]
     public class Person
     {
          private string _accessLevel;

          public const string DEFAULT_ACCESS = "All Access";

          #region Constructors

          public Person()
          {
               OrientationDate = DateTime.Now.Date;
               ExpirationDate = DateTime.MinValue;
               Deleted = false;
               FirstName = "";
               LastName = "";
               PersonId = "";
               Company = "";
               OrientationLevel = "";
               OrientationTestedBy = "";
               VehicleList = new List<Vehicle>();
               LastModified = new DateTime();
               OldCompContact = "";
               VehiclesActivated = true;
               IsNetbox = false;
               AccessLevel = DEFAULT_ACCESS;
          }

          #endregion Constructors

          #region Properties

          [Column(Name = "activationdate")]
          public DateTime? ActivationDate { get; set; }

          [Column(Name = "OldCompcontact")]
          public string OldCompContact { get; set; }

          [Column(Name = "company")]
          public string Company { get; set; }

          [Column(Name = "deleted")]
          public bool Deleted { get; set; }

          [Column(Name = "expirationdate")]
          public DateTime? ExpirationDate { get; set; }

          [Column(Name = "firstname")]
          public string FirstName { get; set; }

          [Column(Name = "fobnumber")]
          public long? FobNumber { get; set; } = 0;

          [Column(Name = "fobcredential")]
          public string FobCredential { get; set; }

          //is this a netbox entry
          [Column(Name = "isnetbox")]
          public bool? IsNetbox
          {
               get;
               set;
          }

          [Column(Name = "lastmodified")]
          public DateTime? LastModified { get; set; }

          [Column(Name = "lastname")]
          public string LastName { get; set; }

          [Column(Name = "orientationdate")]
          public DateTime OrientationDate { get; set; }

          [Column(Name = "orientationlevel")]
          public string OrientationLevel { get; set; }

          [Column(Name = "orientationnumber")]
          public long OrientationNumber { get; set; }

          [Column(Name = "orientationtestedby")]
          public string OrientationTestedBy { get; set; }

          [Column(Name = "personid", DbType = "varchar", IsPrimaryKey = true)]
          public string PersonId { get; set; }

          [Column(Name = "pinnumber")]
          public long? PinNumber { get; set; } = 0;

          public List<Vehicle> VehicleList { get; set; }

          [Column(Name = "vehiclesactivated")]
          public bool? VehiclesActivated { get; set; }

          [Column(Name = "employeeCategory")]
          public string EmployeeCategory { get; set; }

          [Column(Name = "vehicleReader")]
          public int? VehicleReader { get; set; }

          public bool HasCredentials { get; set; } = false;

          private static string[] validAccessLevels = { "All Access", "No Access" };

          public static bool IsValidAccessLevel(string val)
          {
               bool isValid = false;
               foreach(var v in validAccessLevels) {
                    if(v == val) {
                         isValid = true;
                         break;
                    }
               }
               return isValid;
          }

          [Column(Name = "accesslevel")]
          public string AccessLevel
          {
               get
               {
                    return _accessLevel;
               }

               set
               {
                    _accessLevel = value;
               }
          }

          //ensure returned value is not null, must do it this way (not in get property) or it will cause desync issues
          //with DB system
          public string GetValidAccessLevel()
          {
               if (AccessLevel == null) {
                    return DEFAULT_ACCESS;
               } else {
                    return AccessLevel;
               }
          }

          #endregion Properties

          #region Methods

          //get deep copy
          public Person Clone()
          {
               var p = new Person();
               p.FirstName = ConvertUtility.NullStringCopy(FirstName);
               p.LastName = ConvertUtility.NullStringCopy(LastName);
               p.PersonId = ConvertUtility.NullStringCopy(PersonId);
               p.Company = ConvertUtility.NullStringCopy(Company);
               p.Deleted = Deleted;
               p.OrientationNumber = OrientationNumber;
               p.OrientationDate = OrientationDate;
               p.OrientationLevel = ConvertUtility.NullStringCopy(OrientationLevel);
               p.OrientationTestedBy = ConvertUtility.NullStringCopy(OrientationTestedBy);

               p.OldCompContact = string.Copy(ConvertUtility.ConvertStrHandleNull(OldCompContact));

               p.VehiclesActivated = VehiclesActivated;

               p.ActivationDate = ActivationDate;
               p.ExpirationDate = ExpirationDate;
               p.FobNumber = FobNumber;
               p.FobCredential = string.Copy(ConvertUtility.ConvertStrHandleNull(FobCredential));
               p.PinNumber = PinNumber;
               p.IsNetbox = IsNetbox;

               p.EmployeeCategory = EmployeeCategory;
               p.VehicleReader = VehicleReader;

               p.LastModified = LastModified;

               //TODO may need to deep copy this as well
               p.VehicleList = VehicleList;

               p.AccessLevel = AccessLevel;

               return p;
          }

          public string CompareAndPrint(Person other, string thisStr, string otherStr)
          {
               StringBuilder sb = new StringBuilder();

               sb.Append(CompareAndPrint(this.LastName, other.LastName, thisStr, otherStr));
               sb.Append(CompareAndPrint(this.FirstName, other.FirstName, thisStr, otherStr));
               sb.Append(CompareAndPrint(this.Company, other.Company, thisStr, otherStr));
               sb.Append(CompareAndPrint(this.OrientationNumber, other.OrientationNumber, thisStr, otherStr));
               sb.Append(CompareAndPrint(this.OrientationLevel, other.OrientationLevel, thisStr, otherStr));
               sb.Append(CompareAndPrint(this.OrientationTestedBy, other.OrientationTestedBy, thisStr, otherStr));
               sb.Append(CompareAndPrint(this.FobNumber, other.FobNumber, thisStr, otherStr));
               sb.Append(CompareAndPrint(this.PinNumber, other.PinNumber, thisStr, otherStr));
               sb.Append(CompareAndPrint(this.VehicleReader, other.VehicleReader, thisStr, otherStr));

               /*
               sb.AppendLine($"This Vehicles Count={this.VehicleList.Count()}");
               foreach(var v in this.VehicleList) {
                    sb.AppendLine(v.ToString());
               }
               sb.AppendLine();
               sb.AppendLine($"Other Vehicles Count={this.VehicleList.Count()}");
               foreach (var v in other.VehicleList) {
                    sb.AppendLine(v.ToString());
               }*/

               return sb.ToString();
          }

          //copy values from other person into THIS instance
          public void CopyFromOther(Person p)
          {
               FirstName = ConvertUtility.NullStringCopy(p.FirstName);
               LastName = ConvertUtility.NullStringCopy(p.LastName);
               PersonId = ConvertUtility.NullStringCopy(p.PersonId);
               Company = ConvertUtility.NullStringCopy(p.Company);
               Deleted = p.Deleted;
               OrientationNumber = p.OrientationNumber;
               OrientationDate = p.OrientationDate;
               OrientationLevel = ConvertUtility.NullStringCopy(p.OrientationLevel);
               OrientationTestedBy = ConvertUtility.NullStringCopy(p.OrientationTestedBy);
               OldCompContact = ConvertUtility.NullStringCopy(p.OldCompContact);
               VehiclesActivated = p.VehiclesActivated;
               ActivationDate = p.ActivationDate;
               ExpirationDate = p.ExpirationDate;
               FobNumber = p.FobNumber;
               PinNumber = p.PinNumber;
               FobCredential = string.Copy(Utility.ConvertUtility.ConvertStrHandleNull(p.FobCredential));
               IsNetbox = p.IsNetbox;
               VehicleList = p.VehicleList;
               LastModified = p.LastModified;
               EmployeeCategory = ConvertUtility.NullStringCopy(p.EmployeeCategory);
               VehicleReader = p.VehicleReader;
               AccessLevel = p.AccessLevel;
          }

          public override bool Equals(Object obj)
          {
               if (obj == null || GetType() != obj.GetType()) {
                    return false;
               }
               Person p = (Person)obj;
               return this.LastName == p.LastName &&
                         this.FirstName == p.FirstName &&
                         this.Company == p.Company &&
                         this.OrientationNumber == p.OrientationNumber &&
                         //this.OrientationDate == p.OrientationDate &&
                         this.OrientationLevel == p.OrientationLevel &&
                         this.OrientationTestedBy == p.OrientationTestedBy &&
                         this.FobNumber == p.FobNumber &&
                         this.PinNumber == p.PinNumber &&
                         this.FobCredential == p.FobCredential &&
                         this.VehicleList.SequenceEqual(p.VehicleList);
          }

          public bool EqualsIgnoreVehicle(Person p)
          {
               return this.LastName == p.LastName &&
                         this.FirstName == p.FirstName &&
                         this.Company == p.Company &&
                         this.OrientationNumber == p.OrientationNumber &&
                         this.OrientationLevel == p.OrientationLevel &&
                         this.OrientationTestedBy == p.OrientationTestedBy &&
                         this.FobNumber == p.FobNumber &&
                         this.FobCredential == p.FobCredential &&
                         this.PinNumber == p.PinNumber;
          }

          public override int GetHashCode()
          {
               return base.GetHashCode();
          }

          public override string ToString()
          {
               return $"FullName={LastName}, {FirstName} id={PersonId} isNetbox={IsNetbox} deleted={Deleted} fob={FobNumber} pin={PinNumber}";
          }

          private string CompareAndPrint<T>(T val1, T val2, string name1, string name2)
          {
               if (val1?.Equals(val2) == false) {
                    return $"{name1}:{val1} is not equal to {name2}:{val2}\n";
               }
               return string.Empty;
          }

          #endregion Methods
     }
}