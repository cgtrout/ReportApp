/*
 * Created by SharpDevelop.
 * User: Admin Security
 * Date: 6/22/15
 * Time: 7:54 PM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using NUnit.Framework;
using API_Interface;
using ReportApp.Model;
using ReportApp.API;

namespace ReportApp.Tests
{
	[TestFixture]
	public class TestShiftEntry
	{
		[Test]
		public void TestDuplicateShiftEntryOnSameDay()
		{
			//want to test if 2 shift entries created on same day are possible - should not be (if one is currently active)
			//new accessentry
			AccessEntry entry1 = new AccessEntry(){ LogId=15680, DtTm=DateTime.Parse("2015-06-22 11:00:15"), PersonId="1", Reason=0, Reader="Admin IN Reader"};
			AccessEntry entry2 = new AccessEntry(){ LogId=15681, DtTm=DateTime.Parse("2015-06-22 11:00:19"), PersonId="1",Reason=ReasonCode.AntiPassbackViolation, Reader="Admin IN Reader"};
			AccessEntry entry3 = new AccessEntry(){ LogId=15682, DtTm=DateTime.Parse("2015-06-22 11:01:32"), PersonId="1",Reason=ReasonCode.AntiPassbackViolation, Reader="Admin IN Reader"};
			AccessEntry entry4 = new AccessEntry(){ LogId=15783, DtTm=DateTime.Parse("2015-06-22 15:52:47"), PersonId="1",Reason=0, Reader="Admin OUT Reader"};
			var access_list = new List<AccessEntry>(){entry1,entry2,entry3, entry4};
						
			var existing_shiftlist = new List<ShiftEntry>();
			List<ShiftEntry> new_shiftlist = API_Interaction.CreateShiftList(access_list, existing_shiftlist);
			
			//second run
			var shiftlist2 = API_Interaction.CreateShiftList(access_list, new_shiftlist);
			
			//third run
			var shiftlist3 = API_Interaction.CreateShiftList(access_list, new_shiftlist);
			
			//4th run
			var shiftlist4 = API_Interaction.CreateShiftList(access_list, new_shiftlist);		
			
			Assert.IsTrue(shiftlist4.Count==1, "This test should not add a shift entry");
		}
		
		[Test]
		public void EnsureMoreThanOneShiftEntryCanBeCreated()
		{
			AccessEntry entry1 = new AccessEntry(){ LogId=15680, DtTm=DateTime.Parse("2015-06-22 11:00:15"), PersonId="1", Reason=0, Reader="Admin IN Reader"};
			AccessEntry entry2 = new AccessEntry(){ LogId=15681, DtTm=DateTime.Parse("2015-06-22 11:00:16"), PersonId="1", Reason=0, Reader="Admin OUT Reader"};
			AccessEntry entry3 = new AccessEntry(){ LogId=15682, DtTm=DateTime.Parse("2015-06-22 11:00:17"), PersonId="1", Reason=0, Reader="Admin IN Reader"};
			AccessEntry entry4 = new AccessEntry(){ LogId=15682, DtTm=DateTime.Parse("2015-06-22 11:00:18"), PersonId="1", Reason=0, Reader="Admin OUT Reader"};
			var access_list = new List<AccessEntry>(){entry1,entry2,entry3,entry4};
			
			var existing_shiftlist = new List<ShiftEntry>();
			List<ShiftEntry> new_shiftlist = API_Interaction.CreateShiftList(access_list, existing_shiftlist);
			Assert.IsTrue(new_shiftlist.Count==2, "Test should have two entries");
		}
		
		[Test]
		public void PersonForgetsToSignOutThenSignsInNextDay()
		{
			var access_list = new List<AccessEntry>() {
				new AccessEntry(){ LogId=1, DtTm=DateTime.Parse("2015-06-22 11:00:00"), PersonId="1", Reason=0, Reader="Admin IN Reader"},
				new AccessEntry(){ LogId=2, DtTm=DateTime.Parse("2015-06-23 11:00:00"), PersonId="1", Reason=0, Reader="Admin IN Reader"}
			};
			var existing_shiftlist = new List<ShiftEntry>();
			List<ShiftEntry> new_shiftlist = API_Interaction.CreateShiftList(access_list, existing_shiftlist);
			Assert.IsTrue(new_shiftlist.Count==2, "Person Forgets to sign out and signs in next day");
		}
		
		[Test]
		public void PersonForgetsToSignOutCorrectEntrySelected()
		{
			var access_list = new List<AccessEntry>() {
				new AccessEntry(){ LogId=15443, DtTm=DateTime.Parse("2015-06-19 18:48:27"), PersonId="1", Reason=0, Reader="Admin IN Reader"},
				new AccessEntry(){ LogId=15501, DtTm=DateTime.Parse("2015-06-20 18:37:50"), PersonId="1", Reason=0, Reader="Admin IN Reader"},
				new AccessEntry(){ LogId=15532, DtTm=DateTime.Parse("2015-06-21 06:44:38"), PersonId="1", Reason=0, Reader="Admin OUT Reader"},
				new AccessEntry(){ LogId=15558, DtTm=DateTime.Parse("2015-06-21 18:36:00"), PersonId="1", Reason=0, Reader="Admin IN Reader"}
			};
			var existing_shiftlist = new List<ShiftEntry>();
			List<ShiftEntry> new_shiftlist = API_Interaction.CreateShiftList(access_list, existing_shiftlist);
			new_shiftlist = API_Interaction.CreateShiftList(access_list, new_shiftlist);
			Assert.IsTrue(new_shiftlist[0].Hours < 35.0f, "Shift list not created in correct order");
		}
		
		[Test]
		public void DuplicateSignIns()
		{
			var accessList = GetAccessLog();
			var shiftlist = new List<ShiftEntry>();
			
			for(int i = 0; i < 200; i++) {
				shiftlist = API_Interaction.CreateShiftList(accessList, shiftlist);
			}
			var query = shiftlist.Where(x => x.PersonId == "_884").ToList();
			Assert.IsTrue(query.Count==1, "This test should not add a shift entry");
		}
		
		private List<AccessEntry> GetAccessLog()
		{
			//would be better to pull this from database
			API_Interaction api = new API_Interaction();
			return api.LoadAccessLog(31424, true, DateTime.Parse("09/28/15")).Result;
		}
		
		[TestFixtureSetUp]
		public void Init()
		{
			// TODO: Add Init code.
		}
		
		[TestFixtureTearDown]
		public void Dispose()
		{
			// TODO: Add tear down code.
		}
	}
}
