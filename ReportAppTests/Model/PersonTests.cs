using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace ReportApp.Model.Tests
{
     [TestClass()]
     public class PersonTests
     {
          #region Methods

          [TestMethod()]
          public void EqualsTest()
          {
               Person p1 = new Person();
               Person p2 = new Person();

               p1.LastName = "Peter";
               p2.LastName = "Peter";

               List<Vehicle> v1 = new List<Vehicle>() {
                    new Vehicle { Model="Model" },
                    new Vehicle { LicNum="AAABBB" }
               };
               List<Vehicle> v2 = new List<Vehicle>() {
                    new Vehicle { Model="Model" },
                    new Vehicle { LicNum="AAABBBB" },
                    new Vehicle { LicNum="AAABBBB" }
               };

               p1.VehicleList.AddRange(v1);
               p2.VehicleList.AddRange(v2);

               bool equals = p1.Equals(p2);

               Assert.IsFalse(equals);
          }

          #endregion Methods
     }
}