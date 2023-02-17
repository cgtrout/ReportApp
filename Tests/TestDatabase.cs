/*
 * Created by SharpDevelop.
 * User: Admin Security
 * Date: 6/21/15
 * Time: 12:27 AM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using NUnit.Framework;
using API_Interface;
using ReportApp.API;
using ReportApp.API.Test;
using ReportApp.Data;

using ReportApp.Model;
using System.IO;

namespace ReportApp.Tests
{
	[TestFixture]
	public class TestDatabase
	{
		
		//API_InteractionSimulator apiInteraction;
		NetboxDatabase db;
		private const string filename = "test.sqlite";
		//[Test]
		/*
		public async Task TestMethod()
		{
			//#pragma warning disable 4014
			API_Loader loader = new API_Loader();
			apiInteraction.AccelerationFactor = 60;
			while(apiInteraction.Tick()) {
				DBLoadStatus.WriteLine(":::::::::::::Executing UpdateData Time:" + apiInteraction.SimulationTime);
				try {
					//await loader.UpdateData(apiInteraction, filename, apiInteraction.SimulationTime);
				} catch {
					throw;
				}
				
				//wait
				Task.WaitAll(Task.Delay(new TimeSpan(0,0,15)));
				System.Diagnostics.Debug.WriteLine("Setting last time");
				
			}
			//#pragma warning restore 4014
		}
		*/
		
		[Test]
		public void TestReprocessEntry()
		{
			//add test entry
			var access_list = new List<AccessEntry>() {
				new AccessEntry(){ LogId=15443, DtTm=DateTime.Parse("2015-06-19 18:48:27"), PersonId="", Reason=0, Reader="Admin IN Reader"},
				new AccessEntry(){ LogId=15443, DtTm=DateTime.Parse("2015-06-19 18:48:27"), PersonId="1", Reason=0, Reader="Admin IN Reader"}
			};
			
			db.CopyListToTable(access_list);
			var reprocess_list = db.GetReprocessList();
			
			Assert.IsTrue(reprocess_list.Count == 1);
			
			db.Close();
		}
		
		[TestFixtureSetUp]
		public void Init()
		{
			SQLiteConnection.CreateFile(filename);
			db = new NetboxDatabase(filename);
			db.CreateTables();
			db.ClearTable("accessentry");
			db.ClearTable("shiftentry");
			db.Close();
			
			DBLoadStatus.DirectToOutputWindow = true;

			#pragma warning disable 4014
			//apiInteraction = new API_InteractionSimulator(new DateTime(2015, 06, 22, 6, 30, 0));
			//Task t = apiInteraction.LoadDataFromActualDB();
			#pragma warning restore 4014
			//t.Wait();
		}
		
		[TestFixtureTearDown]
		public void Dispose()
		{
			// TODO: Add tear down code.
		}
	}
}
