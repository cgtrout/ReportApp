/*
 * Created by SharpDevelop.
 * User: Admin Security
 * Date: 6/23/15
 * Time: 11:51 PM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using API_Interface;
using ReportApp.Model;

namespace ReportApp.Tests
{

    [TestFixture]
	public class TestAPI
	{
		//[Test]
		public void TestMethod()
		{
			List<Param> paramList = new List<Param>() { new Param("STARTFROMKEY", "317") };
			var xDoc = API_Command.ExecuteCommand(Command_Name.SearchPersonData, paramList);
			xDoc.Wait();
			xDoc.Result.Save("Test searchpersondata.xml");
		}
		
		//[Test]
		public async void TestAddPerson()
		{
			DBLoadStatus.DirectToOutputWindow = true;
			Person p1 = new Person {
				LastName = "API_Test",
				FirstName = "",
				Company = "Test",
				OrientationNumber = 1,
				OrientationDate = DateTime.Now,
				OrientationLevel = "2A",
				OrientationTestedBy = "API"
			};
			System.Windows.MessageBox.Show(await API_Interaction.AddPerson(p1, true));
		}
		
		//[Test]
		public async void TestModifyPerson()
		{
			DBLoadStatus.DirectToOutputWindow = true;
			Person p1 = new Person {
				LastName = "API_Test_Modified",
				FirstName = "Modified",
				Company = "Test",
				PersonId = "_452",
				OrientationNumber = 1,
				OrientationDate = DateTime.Now,
				OrientationLevel = "2A",
				OrientationTestedBy = "API"
			};
			await API_Interaction.ModifyPerson(p1, true);
		}	
		
		//[Test]
		public async void TestSearchPerson()
		{
			DBLoadStatus.DirectToOutputWindow = true;
			Person p = new Person {
				LastName = "Trout",
				FirstName = "Corry",
			};
			string res = await API_Interaction.SearchPerson(p, "FALSE");
		}
		
		//[Test]
		public async void TestAddCredential()
		{
			DBLoadStatus.DirectToOutputWindow = true;
			await API_Interaction.AddCredential("_16", 43901, "Facility Code 193");
			await API_Interaction.AddCredential("_16", 3901, "26 bit Wiegand");
			//await API_Interaction.AddCredential("_16", "0", "Invalid Code");
		}
		
		[Test]
		public void TestAddVehicle()
		{
			List<Vehicle> vehicleList = new List<Vehicle>();
			var v1 = new Vehicle();
			var v2 = new Vehicle();
			v1.LicNum = "AAA BBB";
			v1.Color = "Red";
			v1.Make = "Make1";
			
			v2.LicNum = "CCC DDD";
			v2.Color = "Blue";
			v2.Make = "Make2";
			
			vehicleList.Add(v1);
			vehicleList.Add(v2);
			
			DBLoadStatus.DirectToOutputWindow = true;
			Person p1 = new Person {
				LastName = "API_Test_Modified",
				FirstName = "Modified",
				Company = "Test",
				PersonId = "_452",
				OrientationNumber = 1,
				OrientationDate = DateTime.Now,
				OrientationLevel = "2A",
				OrientationTestedBy = "API",
				VehicleList = vehicleList
			};
			API_Interaction.ModifyPerson(p1, true).Wait();
		}
		
		//[Test]
		public void TestVehicleXML()
		{
			List<Vehicle> vehicleList = new List<Vehicle>();
			var v1 = new Vehicle();
			var v2 = new Vehicle();
			v1.LicNum = "AAA BBB";
			v1.Color = "Red";
			v1.Make = "Make1";
			
			v2.LicNum = "CCC DDD";
			v2.Color = "Blue";
			v2.Make = "Make2";
			
			vehicleList.Add(v1);
			vehicleList.Add(v2);
			
			var xml = API_Interaction.GenerateVehicleXML(vehicleList).ToString();
		}
		/*
		[Test]
		//this isn't a test, but a fragment to capitalize all names in netbox system
		public void CapitalizeAllNames()
		{
			//get names from database
			API_Interaction i = new API_Interaction();
			var personList = i.LoadPersonDetails().Result;
			var query = from p in personList
						where p.Deleted == false
						select p;
			TextInfo ti = new CultureInfo("en-US", false).TextInfo;
			DBLoadStatus.DirectToOutputWindow = true;
			
			foreach(var p in query) {
				if(p.LastName == "Administrator") {
					continue;
				}
				p.LastName = ti.ToUpper(p.LastName);
				API_Interaction.ModifyPerson(p, true);
			}
		}*/
	}
	
	
}
