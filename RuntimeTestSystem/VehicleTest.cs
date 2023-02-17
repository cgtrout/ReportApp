using API_Interface;
using ReportApp.Console;
using ReportApp.Data;
using ReportApp.Model;
using ReportApp.Utility;
using ReportApp.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WindowsInput;
using WindowsInput.Native;

namespace ReportApp.RuntimeTestSystem
{
     public static class VehicleTest
     {
          #region Methods

          public static TestSuite GetSuite()
          {
               //initialize suite
               var suite = new TestSuite();
               suite.Name = "vehicle";

               //Test 1
               var test = new Test();
               test.Name = "Multiple Vehicle Test";
               test.TestMethod = new Func<Task<bool>>(Test1);
               suite.AddTest(test);

               //Cleanup
               suite.CleanupFunction = new Action(Cleanup);

               return suite;
          }

          public static async Task<bool> Test1()
          {
               var console = ConsoleSystem.ConsoleSystemInstance;

               //add multiple passes to vehicle system
               DispatcherHelper.GetDispatcher().Invoke(() => {
                    var passlist = View.VehicleEntryView.PassList;
                    passlist.Clear();
                    for (int i = 0; i < 20; i++) {
                         passlist.Add(new View.Pass(i.ToString()));
                    }
               });

               var personArray = (from p in DataRepository.PersonDict.Values
                                  where p.FirstName.Contains("VehicleTest")
                                  select p).ToArray();

               var personList = new List<PersonViewModel>();

               //create 20 people with vehicle data
               for (int i = 0; i < 20; i++) {
                    //find person
                    PersonViewModel p = null;

                    if (personArray.Length > 0) {
                         p = personArray.First(x => x.FirstName == $"VehicleTest{i}");
                    }

                    if (p == null) {
                         p = CreateTestPerson(i);
                         await API_Interaction.AddPerson(p.InternalPerson, false);
                    }
                    personList.Add(p);
                    using (var db = VehicleDatabase.GetWriteInstance()) {
                         db.DeleteEntriesFromPerson(p.PersonId);
                    }
               }

               //set time used to set testing data
               var time = DateTime.Now + new TimeSpan(0, 0, 3);

               var id = DataRepository.MaxAccessId + 600;

               //input simulator - used for input simulation
               InputSimulator input = new InputSimulator();

               //create multiple sign ins
               for (int i = 0; i < 20; i++) {
                    //sign in

                    //use test queue
                    DataRepository.AccessTestData.Enqueue(new AccessEntry() {
                         PersonId = personList[i].PersonId,
                         DtTm = time,
                         LogId = id++,
                         Reader = "Test IN",
                         ReaderKey = ReaderKeyEnum.TestIn,
                         Reason = 0,
                         Type = Model.TypeCode.ValidAccess,
                    });

                    time += new TimeSpan(0, 0, 0, 0, 100);
                    TraceEx.PrintLog($"Adding in entry {time}");
               }

               //wait long enough for input vehicles to all get set
               time += new TimeSpan(0, 0, 5);
               do {
                    await Task.Delay(1000);
               } while (DateTime.Now < time);

               //press keyboard shortcut for "add pass"
               for (int i = 0; i < 20; i++) {
                    TraceEx.PrintLog("Sending keystroke");
                    input.Keyboard.ModifiedKeyStroke(new[] { VirtualKeyCode.RCONTROL, VirtualKeyCode.RSHIFT },
                                                      new[] { VirtualKeyCode.UP });
                    await Task.Delay(200);
               }

               for (int i = 0; i < 20; i++) {
                    //sign out

                    //use test queue
                    DataRepository.AccessTestData.Enqueue(new AccessEntry() {
                         PersonId = personList[i].PersonId,
                         DtTm = time,
                         LogId = id++,
                         Reader = "Test OUT",
                         ReaderKey = ReaderKeyEnum.TestOut,
                         Reason = 0,
                         Type = Model.TypeCode.ValidAccess,
                    });

                    time += new TimeSpan(0, 0, 0, 0, 200);
                    TraceEx.PrintLog($"Adding out entry {time}");
               }

               //wait long enough for vehicles outtime to all get set
               time += new TimeSpan(0, 0, 5);
               do {
                    await Task.Delay(1000);
               } while (DateTime.Now < time);

               //delete data
               foreach (var p in personList) {
                    using (var db = VehicleDatabase.GetWriteInstance()) {
                         db.DeleteEntriesFromPerson(p.PersonId);
                    }
               }

               //TODO delete access log test data

               return true;
          }

          private static void Cleanup()
          {
               var personArray = (from p in DataRepository.PersonDict.Values
                                  where p.FirstName.Contains("VehicleTest")
                                  select p).ToArray();
               foreach (var person in personArray) {
                    using (var vehicleDB = VehicleDatabase.GetWriteInstance()) {
                         vehicleDB.DeleteEntriesFromPerson(person.PersonId);
                    }
               }
               using (var db = NetboxDatabase.GetWriteInstance()) {
                    db.DeleteAccessEntryTestData();

                    foreach (var person in personArray) {
                         db.DeleteShiftEntryTestData(person.PersonId);
                    }
               }
          }

          private static PersonViewModel CreateTestPerson(int i)
          {
               Model.Person p = new Model.Person();
               p.Company = "TEST";
               p.LastName = "TEST";
               p.FirstName = $"VehicleTest{i}";
               p.VehicleList.Add(new Model.Vehicle() { LicNum = $"AAA {i}" });
               return new PersonViewModel(p);
          }

          #endregion Methods
     }
}