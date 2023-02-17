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
	
	public enum TypeCode { 	ValidAccess = 1,
							InvalidAccess = 2,
							ElevatorValidAccess = 37,
							ElevatorInvalidAccess = 38,
							AccessNotCompleted = 64 };
	public enum ReasonCode { 	CardNotInLocalDatabase = 1,
								CardNotInS2NCDatabase = 2,
								WrongTime = 3,
								WrongLocation = 4,
								CardMisread = 5,
								TailgateViolation = 6,
								AntiPassbackViolation = 7,
								CardExpired = 14,
								CardBitLengthMismatch = 15,
								WrongDay = 16,
								ThreatLevel = 17 };
	/// <summary>
	/// Description of AccessEntry.
	/// </summary>
	[Table(Name="AccessEntry")]
	public class AccessEntry
	{
		[Column(Name="accessentryid", IsPrimaryKey=true)]
		public long LogId { get; set; }
		
		[Column(Name="personid")]
		public string PersonId { get; set; }
		
		[Column(Name="reader")]
		public string Reader { get; set; }
		
		[Column(Name="dttm")]
		public DateTime DtTm { get; set; }
		
		[Column(Name="type")]
		public TypeCode Type { get; set; }
		
		[Column(Name="reason")]
		public ReasonCode Reason { get; set; }
		
		[Column(Name="readerkey")]
		public long ReaderKey  { get; set; }
		
		[Column(Name="portalkey")]
		public long PortalKey { get; set; }
	}
}
