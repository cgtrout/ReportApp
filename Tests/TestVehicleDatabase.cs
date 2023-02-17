/*
 * Created by SharpDevelop.
 * User: Admin Security
 * Date: 8/12/15
 * Time: 1:16 AM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using NUnit.Framework;
using ReportApp.Data;
using ReportApp.Model;

namespace ReportApp.Tests
{
	[TestFixture]
	public class TestVehicleDatabase
	{
		VehicleDatabase db;
		[Test]
		public void TestAdd()
		{
			VehicleEntry entry = new VehicleEntry();
			entry.EntryId = 1;
			entry.TagNum = "Test";
			db.AddEntry(entry);
		}
		
		[Test]
		public void TestEdit()
		{
			VehicleEntry entry = new VehicleEntry();
			entry.EntryId = 1;
			entry.TagNum = "Test2";
			db.EditEntry(entry);
		}
		
		[Test]
		public void TestDelete()
		{
			VehicleEntry entry = new VehicleEntry();
			entry.EntryId = 1;
			
			db.DeleteEntry(entry);
		}
		
		[TestFixtureSetUp]
		public void Init()
		{
			db = new VehicleDatabase(@"c:\CTAPP\DB\Test\Vehicle.sqlite");
		}
	}
}
