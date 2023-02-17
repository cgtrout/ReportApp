using API_Interface;
using ReportApp.Data;
using ReportApp.Utility;
using ReportApp.ViewModel;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ReportApp.RuntimeTestSystem
{
     public static class ChangeDataTest
     {
          #region Methods

          public static TestSuite GetSuite()
          {
               //initialize suite
               var suite = new TestSuite();
               suite.Name = "datasync";

               //Test 1
               var test = new Test();
               test.Name = "Multiple Updates";
               test.TestMethod = new Func<Task<bool>>(Test1);
               suite.AddTest(test);

               //Test 2
               var test2 = new Test();
               test2.Name = "Cred change test";
               test2.TestMethod = new Func<Task<bool>>(Test2);
               suite.AddTest(test2);

               //Test 3
               var test3 = new Test();
               test3.Name = "Test that vehicles are loaded";
               test3.TestMethod = new Func<Task<bool>>(Test3);
               suite.AddTest(test3);

               return suite;
          }

          public static async Task<bool> Test1()
          {
               var p = DataRepository.PersonDict["_492"];
               AddPersonViewModel personvm = AddPersonViewModel.Create(p, true);

               for (int i = 0; i < 10; i++) {
                    TraceEx.PrintLog($"{i}");
                    var istring = i.ToString();
                    personvm.CurrentPerson.FirstName = istring;

                    await personvm.ModifyPerson(personvm.CurrentPerson, true);

                    await Task.Delay(1000);

                    if (DataRepository.PersonDict["_492"].FirstName.ToString() != istring) {
                         TestSuite.WriteLine($"Test 1 - not equal on iteration {i}");
                         return false;
                    }
               }

               return true;
          }

          public static async Task<bool> Test2()
          {
               long fobNumber = 44654;
               long pinNumber = 9999;

               //delete names to ensure data is clean
               API_Interaction.RemovePerson("_1044");
               API_Interaction.RemovePerson("_1068");

               var p1 = new PersonViewModel(AsyncHelper.RunSync<Model.Person>(() => API_Interaction.LoadSinglePerson("_1044")));
               var p2 = new PersonViewModel(AsyncHelper.RunSync<Model.Person>(() => API_Interaction.LoadSinglePerson("_1068")));
               DataRepository.AddPerson(p1);
               DataRepository.AddPerson(p2);

               AddPersonViewModel vm1 = AddPersonViewModel.Create(p1, true);
               AddPersonViewModel vm2 = AddPersonViewModel.Create(p2, true);

               //add fake fob to vm1
               vm1.CurrentPerson.FobNumber = fobNumber;
               vm1.CurrentPerson.PinNumber = pinNumber;
               vm1.CurrentPerson.OrientationTestedBy = "TEST";
               vm1.CurrentPerson.OrientationLevel = "2";
               vm2.CurrentPerson.OrientationTestedBy = "TEST";
               vm2.CurrentPerson.OrientationLevel = "2";
               await vm1.Save(testing: true);

               //check vm1 information exists on vm1
               var checkPerson = await API_Interaction.LoadSinglePerson("_1044");
               Assert.IsTrue(checkPerson.FobNumber == fobNumber);
               Assert.IsTrue(checkPerson.PinNumber == pinNumber);

               //add same fob to vm2
               vm2.CurrentPerson.FobNumber = fobNumber;
               vm2.CurrentPerson.PinNumber = pinNumber;
               await vm2.Save(testing: true);

               //check fake fob exists on vm
               var checkPerson2 = await API_Interaction.LoadSinglePerson("_1068");
               Assert.IsTrue(checkPerson2.FobNumber == fobNumber);
               Assert.IsTrue(checkPerson2.PinNumber == pinNumber);

               //check fob does not exist on vm1
               var checkPerson3 = await API_Interaction.LoadSinglePerson("_1044");
               Assert.IsTrue(checkPerson3.FobNumber == 0);
               Assert.IsTrue(checkPerson3.PinNumber == 0);

               //delete names (clean up)
               //API_Interaction.RemovePerson("_1044");
               //API_Interaction.RemovePerson("_1068");

               return true;
          }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

          public static async Task<bool> Test3()

          {
               foreach (var p in DataRepository.PersonDict.Values) {
                    if (p.VehicleList.Any()) {
                         return true;
                    }
               }
               return false;
          }

#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously

          #endregion Methods
     }
}