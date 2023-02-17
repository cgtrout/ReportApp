/*
 * Created by SharpDevelop.
 * User: umwr
 * Date: 2015-05-29
 * Time: 9:29 PM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Data.Linq.Mapping;

namespace ReportApp.Data
{
	/// <summary>
	/// Description of ShiftEntry.
	/// </summary>
	[Table(Name="shiftentry")]
	public class ShiftEntry
	{
		[Column(Name="shiftEntryId", IsPrimaryKey=true)]
		public int ShiftEntryId { get; set; }
		
		[Column(Name="personid")]
		public string PersonId { get; set; }
				
		[Column(Name="inTime")]
		public DateTime InTime { get; set; }
		
		[Column(Name="outTime")]
		public DateTime OutTime { get; set; }
		
		[Column(Name="inlogid")]
		public long InLogId { get; set; }
		
		[Column(Name="outlogid")]
		public long OutLogId { get; set; }
		
		[Column(Name="hoursworked")]
		public double Hours { get; set; }
	}
}
