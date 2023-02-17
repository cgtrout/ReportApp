/*
 * Created by SharpDevelop.
 * User: umwr
 * Date: 2015-05-29
 * Time: 9:33 PM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Data.Linq.Mapping;

namespace ReportApp.Data
{
	/// <summary>
	/// Description of Person.
	/// </summary>
	[Table(Name="person")]
	public class Person
	{
		[Column(Name="firstname")]
		public string FirstName { get; set; }
		
		[Column(Name="lastname")]
		public string LastName { get; set; }
		
		[Column(Name="personid", IsPrimaryKey=true)]
		public string PersonId{ get; set; }
		
		[Column(Name="company")]
		public string Company { get; set; }
		
		[Column(Name="deleted")]
		public bool Deleted { get; set; }
		
	}
}
