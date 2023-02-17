using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ReportApp.Utility.Tests
{
     [TestClass()]
     public class FlaggedListTests
     {
          #region Methods

          [TestMethod()]
          public void SetFlagTest()
          {
               FlaggedList fl = new FlaggedList();

               fl.Add(1000);
               fl.Add(1003);
               fl.Add(1004);

               fl.Add(1010);

               fl.Add(2001);

               Assert.IsTrue(fl.Get(1000) == true, "1000");
               Assert.IsTrue(fl.Get(1001) == false, "1001");
               Assert.IsTrue(fl.Get(1002) == false, "1002");
               Assert.IsTrue(fl.Get(1003) == true, "1003");
               Assert.IsTrue(fl.Get(1004) == true, "1004");
               Assert.IsTrue(fl.Get(1005) == false, "1005");
               Assert.IsTrue(fl.Get(1006) == false, "1006");
               Assert.IsTrue(fl.Get(1010) == true, "1010");
               Assert.IsTrue(fl.Get(1011) == false, "1011");

               var res2 = fl.Get(105);
               Assert.IsFalse(res2, "Checking number that does not exist");

               var res3 = fl.Get(9);
               Assert.IsFalse(res3, "Checking number that does not exist");

               //now check rebuild
               fl.Add(15);

               Assert.IsTrue(fl.Get(15) == true, "15");
               Assert.IsTrue(fl.Get(14) == false, "14");
               Assert.IsTrue(fl.Get(16) == false, "16");
               Assert.IsTrue(fl.Get(1000) == true, "1000");
               Assert.IsTrue(fl.Get(1001) == false, "1000");
               Assert.IsTrue(fl.Get(1002) == false, "1002");
               Assert.IsTrue(fl.Get(1003) == true, "1003");
               Assert.IsTrue(fl.Get(1004) == true, "1004");
               Assert.IsTrue(fl.Get(1005) == false, "1005");
               Assert.IsTrue(fl.Get(1006) == false, "1006");

               fl.Add(14);

               Assert.IsTrue(fl.Get(15) == true, "15");
               Assert.IsTrue(fl.Get(14) == true, "14");
               Assert.IsTrue(fl.Get(13) == false, "13");
               Assert.IsTrue(fl.Get(16) == false, "16");
               Assert.IsTrue(fl.Get(1000) == true, "1000");
               Assert.IsTrue(fl.Get(1001) == false, "1000");
               Assert.IsTrue(fl.Get(1002) == false, "1002");
               Assert.IsTrue(fl.Get(1003) == true, "1003");
               Assert.IsTrue(fl.Get(1004) == true, "1004");
               Assert.IsTrue(fl.Get(1005) == false, "1005");
               Assert.IsTrue(fl.Get(1006) == false, "1006");

               fl.Add(13);

               Assert.IsTrue(fl.Get(15) == true, "15");
               Assert.IsTrue(fl.Get(14) == true, "14");
               Assert.IsTrue(fl.Get(13) == true, "13");
               Assert.IsTrue(fl.Get(12) == false, "12");
               Assert.IsTrue(fl.Get(16) == false, "16");
               Assert.IsTrue(fl.Get(1000) == true, "1000");
               Assert.IsTrue(fl.Get(1001) == false, "1000");
               Assert.IsTrue(fl.Get(1002) == false, "1002");
               Assert.IsTrue(fl.Get(1003) == true, "1003");
               Assert.IsTrue(fl.Get(1004) == true, "1004");
               Assert.IsTrue(fl.Get(1005) == false, "1005");
               Assert.IsTrue(fl.Get(1006) == false, "1006");

               Assert.IsTrue(fl.Get(1010) == true, "1010");
               Assert.IsTrue(fl.Get(1011) == false, "1011");

               Assert.IsTrue(fl.Get(1500) == false, "1500");
               Assert.IsTrue(fl.Get(2000) == false, "2000");
               Assert.IsTrue(fl.Get(2001) == true, "2001");

               //Assert.Fail();
          }

          #endregion Methods
     }
}