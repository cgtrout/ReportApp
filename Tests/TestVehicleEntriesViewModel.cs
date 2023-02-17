/*
 * Created by SharpDevelop.
 * User: Admin Security
 * Date: 8/12/15
 * Time: 2:16 AM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using NUnit.Framework;
using ReportApp.Data;
using ReportApp.Model;
using ReportApp.ViewModel;

namespace ReportApp.Tests
{
	[TestFixture]
	public class TestVehicleEntriesViewModel
	{
		VehicleEntriesViewModel entriesvm = null;
		
		//Person with vehicle signs in and out
		[Test]
		public void Test1()
		{
			var accessList = new AccessEntry[] {
				new AccessEntry() {
					PersonId = "_1",
					Reader = "Admin IN",
					DtTm = new DateTime(1, 1, 1, 1, 1, 1)
				},
				new AccessEntry() {
					PersonId = "_1",
					Reader = "Admin OUT",
					DtTm = new DateTime(1, 1, 1, 1, 1, 2)
				}
			};
			entriesvm.SelectedDate = new DateTime(1, 1, 1);
			entriesvm.ProcessAccessEntries(accessList);
			Assert.IsTrue(entriesvm.Entries.Count == 1);
		}
		
		//person without vehicle signs in
		[Test]
		public void Test2()
		{
			var accessList = new AccessEntry[] {
				new AccessEntry() {
					PersonId = "_3",
					Reader = "Admin IN",
					DtTm = new DateTime(1, 1, 1, 1, 1, 1)
				}
			};
			entriesvm.SelectedDate = new DateTime(1, 1, 1);
			entriesvm.ProcessAccessEntries(accessList);
			Assert.IsTrue(entriesvm.Entries.Count == 0);
		}
		
		//person with multiple vehicles signs in
		[Test]
		public void Test3()
		{
			var accessList = new AccessEntry[] {
				new AccessEntry() {
					PersonId = "_2",
					LogId = 1,
					Reader = "Admin IN",
					DtTm = new DateTime(1, 1, 1, 1, 1, 1)
				},
				new AccessEntry() {
					PersonId = "_2",
					LogId = 2,
					Reader = "Admin OUT",
					DtTm = new DateTime(1, 1, 1, 1, 1, 2)
				}
			};
			entriesvm.SelectedDate = new DateTime(1, 1, 1);
			entriesvm.ProcessAccessEntries(accessList);
			Assert.IsTrue(entriesvm.Entries[0].LicNumber == "Vehicle 2");
		}
		
		[Test]
		public void PersonSignsInMultTimes()
		{
			var accessList = new AccessEntry[] {
				new AccessEntry() {
					PersonId = "_1",
					LogId = 1,
					Reader = "Admin IN",
					DtTm = new DateTime(1, 1, 2, 1, 1, 2)
				},
				new AccessEntry() {
					PersonId = "_1",
					LogId = 2,
					Reader = "Admin IN",
					DtTm = new DateTime(1, 1, 3, 1, 1, 3)
				},
				new AccessEntry() {
					PersonId = "_1",
					LogId = 3,
					Reader = "Admin OUT",
					DtTm = new DateTime(1, 1, 3, 1, 1, 4)
				}
			};
			entriesvm.SelectedDate = new DateTime(1, 1, 3);
			entriesvm.ProcessAccessEntries(accessList);
			Assert.IsTrue(entriesvm.Entries.Count == 1);
		}
		
		//test all fields set
		[Test]
		public void Test4()
		{
			var accessList = new AccessEntry[] {
				new AccessEntry() {
					PersonId = "_2",
					LogId = 1,
					Reader = "Admin IN",
					DtTm = new DateTime(1, 1, 1, 1, 1, 1)
				},
				new AccessEntry() {
					PersonId = "_2",
					LogId = 2,
					Reader = "Admin OUT",
					DtTm = new DateTime(1, 1, 1, 1, 1, 2)
				}
			};
			entriesvm.SelectedDate = new DateTime(1, 1, 1);
			entriesvm.ProcessAccessEntries(accessList);
			Assert.IsTrue(entriesvm.Entries[0].Make == "Make");
			Assert.IsTrue(entriesvm.Entries[0].TagNum == "Tag");
			Assert.IsTrue(entriesvm.Entries[0].Color == "Color");
		}
		
		[Test]
		public void PersonSignsInMultipleTimes2()
		{
			var accessList = new AccessEntry[] {
				new AccessEntry() {
					PersonId = "_1",
					LogId = 1,
					Reader = "Admin IN",
					DtTm = new DateTime(1, 1, 1, 1, 1, 2)
				},
				new AccessEntry() {
					PersonId = "_1",
					LogId = 2,
					Reader = "Admin IN",
					DtTm = new DateTime(1, 1, 1, 1, 1, 3)
				},
				new AccessEntry() {
					PersonId = "_1",
					LogId = 3,
					Reader = "Admin OUT",
					DtTm = new DateTime(1, 1, 1, 1, 1, 4)
				}
			};
			entriesvm.SelectedDate = new DateTime(1, 1, 1);
			entriesvm.ProcessAccessEntries(accessList);
			entriesvm.ProcessAccessEntries(accessList);
			Assert.IsTrue(entriesvm.Entries.Count == 1);
		}
		
		[TestFixtureSetUp]
		public void Init()
		{
			Person personWithVehicle = new Person();
			personWithVehicle.PersonId = "_1";
			personWithVehicle.VehicleList = new List<Vehicle>() {
				new Vehicle() { LicNum = "Vehicle 1" }
			};
			Person personWithVehicles = new Person();
			personWithVehicles.PersonId = "_2";
			personWithVehicles.VehicleList = new List<Vehicle>() {
				new Vehicle() { 
					LicNum = "Vehicle 2",
					Make = "Make",
					Color = "Color",
					TagNum = "Tag"					
				},
				new Vehicle() { LicNum = "Vehicle 3" }
			};
			Person personWithNoVehicle = new Person();
			personWithNoVehicle.PersonId = "_3";

            /*
            DataRepository.PersonList = new List<PersonViewModel>() {
				new PersonViewModel(personWithVehicle),
				new PersonViewModel(personWithVehicles),
				new PersonViewModel(personWithNoVehicle)
			};
			const string file = @"c:\CTApp\DB\Test\Vehicle.sqlite";
			VehicleDatabase db = new VehicleDatabase(file);
			
			//ensure db is cleared
			db.ClearTable("vehicleentry");
			db.Close();
			
			entriesvm = new VehicleEntriesViewModel(file, new MainWindowViewModel());
            */
		}
	}
}
