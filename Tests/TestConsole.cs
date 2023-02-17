/*
 * Created by SharpDevelop.
 * User: Admin Security
 * Date: 10/10/15
 * Time: 8:44 PM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using NUnit.Framework;
using ReportApp.Console;
using System.Diagnostics;

namespace ReportApp.Tests
{
	[TestFixture]
	public class TestConsole
	{
		ConsoleSystem consoleSystem;
		
		[TestFixtureSetUp]
		public void Init()
		{
			consoleSystem = new ConsoleSystem();
		}
		
		[Test]
		public void CreateCommandAndExecute()
		{
			int i = 0;
			ConsoleCommand command = new ConsoleCommand("testcommand", "description", ()=>i++);
			command.Name = "testcommand";
			
			command.Method = () => i++;
			
			consoleSystem.AddCommand(command);
			consoleSystem.ExecuteCommand("testcommand");
			Assert.IsTrue(i == 1);
		}
		
		[Test]
		public void InvalidCommand()
		{
			try {
				consoleSystem.ExecuteCommand("invalid command");
			} catch (ConsoleSystemException) {
				
			}
		}
	}
}
