using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReportApp.Data;
using ReportApp.Model;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ReportApp.API.Tests
{
     [TestClass()]
     public class API_LoaderTests
     {
          #region Methods

          ////only partially tests reprocess.  Does not emulate full step.  Basically a full API emulator
          ////would be needed to test this thouroughly
          //[TestMethod()]
          //public async Task TestReprocess()
          //{
          //     //create copy of current database -- use it
          //     CopyDatabase();
          //     DateTime compareTime;
          //     long newId = 0;

          //     //do access cycle - create fake entry
          //     API_Loader apiLoader = new API_Loader();
          //     using (NetboxDatabase db = new NetboxDatabase("c:\\CTApp\\DB\\TestAPI_Loader\\DB\\data.sqlite")) {
          //          newId = db.GetContext().AccessEntries.Max(x => x.LogId) + 1;
          //          var entry = new AccessEntry() {
          //               DtTm = DateTime.Now.AddMinutes(0.1),
          //               LogId = newId,
          //               PersonId = null,
          //               Reader = "ADMIN IN",
          //               Reason = 0,
          //               Type = Model.TypeCode.ValidAccess
          //          };
          //          compareTime = entry.DtTm;
          //          var dbContext = db.GetContext();
          //          dbContext.AccessEntries.InsertOnSubmit(entry);
          //          dbContext.SubmitChanges();
          //     };
          //     await apiLoader.UpdateAccess("c:\\CTApp\\DB\\TestAPI_Loader\\DB\\data.sqlite");

          //     using (NetboxDatabase db = new NetboxDatabase("c:\\CTApp\\DB\\TestAPI_Loader\\DB\\data.sqlite")) {
          //          //emulate person update, (this assigns id)
          //          var entry = new AccessEntry() {
          //               DtTm = compareTime,
          //               LogId = newId,
          //               PersonId = "1",
          //               Reader = "ADMIN IN",
          //               Reason = 0,
          //               Type = Model.TypeCode.ValidAccess
          //          };
          //          var dbContext = db.GetContext();
          //          dbContext.AccessEntries.InsertOnSubmit(entry);
          //          dbContext.SubmitChanges();
          //     };

          //     //do acccess cycle with reprocess
          //     await apiLoader.UpdateAccess("c:\\CTApp\\DB\\TestAPI_Loader\\DB\\data.sqlite");

          //     using (NetboxDatabase db = new NetboxDatabase("c:\\CTApp\\DB\\TestAPI_Loader\\DB\\data.sqlite")) {
          //          var dbContext = db.GetContext();
          //          //reprocessed entry should now be added
          //          var query = dbContext.AccessEntries.Where(x => x.LogId == newId);
          //          var checkEntry = query.ToList().First();
          //          Assert.IsTrue(checkEntry.DtTm == compareTime);
          //          Assert.IsTrue(checkEntry.PersonId == "1");
          //     }
          //}

          private void CopyDatabase()
          {
               if (!Directory.Exists("c:\\CTApp\\DB\\TestAPI_Loader\\DB\\")) {
                    Directory.CreateDirectory("c:\\CTApp\\DB\\TestAPI_Loader\\DB\\");
               }
               else {
                    File.Delete("c:\\CTApp\\DB\\TestAPI_Loader\\DB\\data.sqlite");
               }

               try {
                    File.Copy(@"c:\CTApp\DB\Data.sqlite", $"c:\\CTApp\\DB\\TestAPI_Loader\\DB\\data.sqlite");
               }
               catch (Exception) {
                    System.Console.WriteLine("Problem backing up database");
               }
          }

          #endregion Methods
     }
}