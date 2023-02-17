using ReportApp.Data;
using ReportApp.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace ReportApp.RuntimeTestSystem
{
     public static class AccessEntryTest
     {
          private const string personId = "_1068";

          #region Methods

          public static TestSuite GetSuite()
          {
               //initialize suite
               var suite = new TestSuite();
               suite.Name = "access";

               var test4 = new Test();
               test4.Name = "Test Access Log";
               test4.TestMethod = new Func<Task<bool>>(Test4);
               suite.AddTest(test4);

               suite.CleanupFunction = new Action(Cleanup);

               return suite;
          }

          private static void Cleanup()
          {
               using (var db = NetboxDatabase.GetWriteInstance()) {
                    //delete access data
                    db.DeleteAccessEntryTestData();

                    //delete shift entry data
                    db.DeleteShiftEntryTestData(personId);
               }

               using (var db = VehicleDatabase.GetWriteInstance()) {
                    db.DeleteEntriesFromPerson(personId);
               }
          }

          private static async Task<bool> Test4()
          {
               //reinitialize test data
               DataRepository.AccessTestData = new System.Collections.Concurrent.ConcurrentQueue<AccessEntry>();

               //set time used to set testing data
               var time = DateTime.Now + new TimeSpan(0, 0, 5);

               var id = DataRepository.MaxAccessId + 500;

               var createdList = new List<AccessEntry>();

               //add data
               for (int i = 0; i < 30; i++) {
                    AccessEntry a = new AccessEntry();
                    a.DtTm = time + new TimeSpan(0, 0, i);
                    a.LogId = id++;
                    a.PersonId = personId;

                    if (i % 2 == 0) {
                         a.Reader = "Test IN";
                         a.ReaderKey = ReaderKeyEnum.TestIn;
                    } else {
                         a.Reader = "Test OUT";
                         a.ReaderKey = ReaderKeyEnum.TestOut;
                    }

                    a.Reason = 0;
                    a.Type = Model.TypeCode.ValidAccess;

                    createdList.Add(a);
                    DataRepository.AccessTestData.Enqueue(a);
               }

               //wait until all test data is removed from injection queue
               do {
                    await Task.Delay(5000);
               } while (!DataRepository.AccessTestData.IsEmpty);
               await Task.Delay(10000);

               try {
                    using (var db = NetboxDatabase.GetWriteInstance()) {
                         //ensure all out times are completed
                         var shiftEntryQuery = db.GetContext().ShiftEntries.Where(x => x.PersonId == personId);
                         foreach (var v in shiftEntryQuery) {
                              //if outlogid is 0 it means outtime has not been assigned
                              if (v.OutLogId == 0) {
                                   throw new RuntimeTestSystem.TestException("Out time not completed");
                              }
                         }

                         //delete access data
                         db.DeleteAccessEntryTestData();

                         //delete shift entry data
                         db.DeleteShiftEntryTestData(personId);
                    }
               }
               catch (Exception e) {
                    Trace.TraceError($"AccessEntry Test: Exception {e.GetType().Name} Caught: {e.Message} ");
               }

               return true;
          }

          #endregion Methods
     }
}