using ReportApp.Model;
using ReportApp.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace API_Interface
{
     //possible commands
     public enum Command_Name
     {
          SearchPersonData,
          ModifyPerson,
          GetAccessHistory,
          AddPerson,
          AddCredential,
          RemoveCredential,
          RemovePerson,
          GetAccessDataLog
     };

     /// <summary>
     /// Parameter used for API commands
     /// </summary>
     public class Param
     {
          public Param(string name, string value)
          {
               Name = name;
               Value = value;
          }

          public string Name { get; set; }
          public string Value { get; set; }

          public override string ToString()
          {
               return string.Format("[Param Name={0}, Value={1}]", Name, Value);
          }

          public static string ToStringList(List<Param> paramList)
          {
               string outs = "";
               if (paramList != null) {
                    foreach (var p in paramList) {
                         outs += p.ToString() + "\n";
                    }
               }
               return outs;
          }
     }

     /// <summary>
     /// Error coes that can be returned by API
     /// </summary>
	public enum API_Error
     {
          API_NONE = 0,
          API_INIT_FAIL = 1,
          API_DISABLED = 2,
          API_NOCOMMAND = 3,
          API_PARSE_ERROR = 4,
          API_AUTH_FAILURE = 5,
          API_UNKNOWN_COMMAND = 6
     };

     /// <summary>
     /// Description of API_Command.
     /// </summary>
     public class API_Command
     {
          /// <summary>
          /// Web address to Netbox System
          /// </summary>
          private const string apiWebAddress = "http://192.168.0.200/goform/nbapi";
          
          /// <summary>
          /// Timeout used for API connections
          /// </summary>
          private static readonly int API_CONNECT_TIMEOUT = 1000;

          /// <summary>
          /// Convert API Code to string
          /// </summary>
          /// <param name="errorcode"></param>
          /// <returns>string of error code</returns>
          public static string getAPIErrorString(int errorCode)
          {
               var e = (API_Error)errorCode;
               return e.ToString();
          }

          public API_Command()
          {
          }

          /// <summary>
          /// Build XML for command to be sent to S2Netbox
          /// </summary>
          private static XElement BuildCommandXML(Command_Name command, List<Param> paramList)
          {
               var xelement = new XElement("NETBOX-API",
                                        new XElement("COMMAND",
                                           new XAttribute("name", command.ToString()),
                                           new XAttribute("num", "1"),
                                           new XAttribute("dateformat", "tzoffset"),
                                           new XElement("PARAMS",
                                               from p in paramList
                                               select new XElement(p.Name, p.Value))));
#if XML_LOG
			string dateString = String.Format("Command {0} {1}{2}{3}{4}.xml", command.ToString(), System.DateTime.Now.Day, System.DateTime.Now.Month, System.DateTime.Now.Minute, System.DateTime.Now.Second);
			xelement.Save(dateString);
#endif
               return xelement;
          }

          /// <summary>
		/// Build XML for batched commands to be sent to S2Netbox
		/// </summary>
		private static XElement BuildCommandXML(Command_Name[] commands, List<Param>[] paramList)
          {
               int i = 0;
               var xelement = new XElement("NETBOX-API",
                                    from c in commands
                                    select
                     new XElement("COMMAND",
                        new XAttribute("name", c.ToString()),
                        new XAttribute("num", (i + 1).ToString()),
                        new XAttribute("dateformat", "tzoffset"),
                        new XElement("PARAMS",
                            from p in paramList[i++]
                            select new XElement(p.Name, p.Value))));

               return xelement;
          }

          /// <summary>
          /// Sends XML request and gets a response
          /// </summary>
          /// <param name="command">Command to run</param>
          /// <param name="paramList">List of paramaters to call</param>
          public static async Task<XDocument> ExecuteCommandAsync(Command_Name command, List<Param> paramList = null, bool logXmlInput = false)
          {
               XDocument xdoc = null;

               if (paramList == null) {
                    paramList = new List<Param>();
               }

               string commandXMLstr = GetCommandXML(command, paramList);

               if (logXmlInput) {
                    TraceEx.PrintLog($"Command String::\n{commandXMLstr}\n");
               }

               using (var responseStream = await ConnectAndGetResponseAsync(commandXMLstr)) {
                    xdoc = GetXDocFromStream(responseStream);
               }

               PrintXmlOutput(command, xdoc);
               CheckError(xdoc);

               return xdoc;
          }

          /// <summary>
          /// Sends XML request and gets a response
          /// </summary>
          /// <param name="command">Command to run</param>
          /// <param name="paramList">List of paramaters to call</param>
          public static XDocument ExecuteCommand(Command_Name command, List<Param> paramList = null)
          {
               XDocument xdoc = null;
               string commandXMLstr = GetCommandXML(command, paramList);

               if (paramList == null) {
                    paramList = new List<Param>();
               }

               using (var responseStream = ConnectAndGetResponse(commandXMLstr)) {
                    xdoc = GetXDocFromStream(responseStream);
               }

               PrintXmlOutput(command, xdoc);
               CheckError(xdoc);

               return xdoc;
          }

          public static async Task<XDocument> ExecuteCommandsAsync(Command_Name[] commands, List<Param>[] paramLists)
          {
               string commandXMLstr = GetCommandXML(commands, paramLists);

               try {
                    XDocument xdoc = null;

                    using (var responseStream = await ConnectAndGetResponseAsync(commandXMLstr)) {
                         xdoc = GetXDocFromStream(responseStream);
                    }

                    CheckError(xdoc);

                    return xdoc;
               }
               catch (WebException) {
                    NetworkTools.PingLog();
                    DBLoadStatus.WriteLine("Error connecting to S2Netbox");
                    throw;
               }
          }

          public static XDocument ExecuteCommands(Command_Name[] commands, List<Param>[] paramLists)
          {
               string commandXMLstr = GetCommandXML(commands, paramLists);

               try {
                    XDocument xdoc = null;

                    using (var responseStream = ConnectAndGetResponse(commandXMLstr)) {
                         xdoc = GetXDocFromStream(responseStream);
                    }

                    CheckError(xdoc);

                    return xdoc;
               }
               catch (WebException) {
                    NetworkTools.PingLog();
                    DBLoadStatus.WriteLine("Error connecting to S2Netbox");
                    throw;
               }
          }

          private static string GetCommandXML(Command_Name command, List<Param> paramList)
          {
               XElement commandXML = BuildCommandXML(command, paramList);
               string commandXMLstr = commandXML.ToString();

               commandXMLstr = ReplaceGreaterLessThan(commandXMLstr);
               return commandXMLstr;
          }

          private static string GetCommandXML(Command_Name[] command, List<Param>[] paramList)
          {
               XElement commandXML = BuildCommandXML(command, paramList);
               string commandXMLstr = commandXML.ToString();

               commandXMLstr = ReplaceGreaterLessThan(commandXMLstr);
               return commandXMLstr;
          }

          private static async Task<Stream> ConnectAndGetResponseAsync(string commandXMLstr)
          {
               DBLoadStatus.WriteLine(commandXMLstr);
               DBLoadStatus.WriteLine("");
               DBLoadStatus.WriteLine("Creating web request");

               WebRequest request = WebRequest.Create(apiWebAddress);
               request.Method = "POST";

               byte[] b = System.Text.Encoding.ASCII.GetBytes(commandXMLstr);
               request.ContentLength = b.Length;
               request.ContentType = "application/x-www-form-urlencoded";
               request.Timeout = API_CONNECT_TIMEOUT;

               DBLoadStatus.WriteLine("Getting request stream");

               var requestStream = await request.GetRequestStreamAsync();
               requestStream.ReadTimeout = API_CONNECT_TIMEOUT;
               requestStream.WriteTimeout = API_CONNECT_TIMEOUT;

               DBLoadStatus.WriteLine("Writing request stream");

               if (requestStream == null) {
                    return null;
               }

               try {
                    requestStream.Write(b, 0, b.Length);
               }
               catch (IOException) {
                    return null;
               }
               requestStream.Close();
               DBLoadStatus.WriteLine("Getting web response");

               WebResponse response = await request.GetResponseAsync();
               DBLoadStatus.WriteLine(((HttpWebResponse)response).StatusDescription);
               Stream responseStream = response.GetResponseStream();
               responseStream.ReadTimeout = API_CONNECT_TIMEOUT;
               responseStream.WriteTimeout = API_CONNECT_TIMEOUT;

               return responseStream;
          }

          private static Stream ConnectAndGetResponse(string commandXMLstr)
          {
               DBLoadStatus.WriteLine(commandXMLstr);
               DBLoadStatus.WriteLine("");
               DBLoadStatus.WriteLine("Creating web request");

               WebRequest request = WebRequest.Create(apiWebAddress);
               request.Method = "POST";

               byte[] b = System.Text.Encoding.ASCII.GetBytes(commandXMLstr);
               request.ContentLength = b.Length;
               request.ContentType = "application/x-www-form-urlencoded";
               request.Timeout = API_CONNECT_TIMEOUT;

               DBLoadStatus.WriteLine("Getting request stream");
               Stream requestStream = null;
               try {
                    requestStream = request.GetRequestStream();
                    requestStream.ReadTimeout = API_CONNECT_TIMEOUT;
                    requestStream.WriteTimeout = API_CONNECT_TIMEOUT;
               }
               catch (WebException) {
                    NetworkTools.PingLog();
                    Trace.TraceError("GetRequestStream connection error.");
                    return null;
               }

               DBLoadStatus.WriteLine("Writing request stream");

               if (requestStream == null) {
                    return null;
               }

               try {
                    requestStream.Write(b, 0, b.Length);
               }
               catch (IOException) {
                    return null;
               }
               requestStream.Close();
               DBLoadStatus.WriteLine("Getting web response");

               try {
                    WebResponse response = request.GetResponse();
                    DBLoadStatus.WriteLine(((HttpWebResponse)response).StatusDescription);
                    Stream responseStream = response.GetResponseStream();
                    responseStream.ReadTimeout = API_CONNECT_TIMEOUT;
                    responseStream.WriteTimeout = API_CONNECT_TIMEOUT;
                    return responseStream;
               }
               catch (WebException e) {
                    NetworkTools.PingLog();
                    Trace.TraceError($"WebException: {e.Message}");
                    return null;
               }
          }

          private static XDocument GetXDocFromStream(Stream responseStream)
          {
               if (responseStream == null) { return null; }

               XmlReader xmlReader = XmlReader.Create(responseStream);
               XDocument xdoc = XDocument.Load(xmlReader);
               return xdoc;
          }

          private static void CheckError(XDocument xdoc)
          {
               if (xdoc == null) { return; }

               var q = from a in xdoc.Descendants("APIERROR")
                       select a;
               if (q.Any()) {
                    int value = Convert.ToInt32(q.First().Value);
                    throw new InvalidOperationException("Error returned from NetBoxAPI: " + getAPIErrorString(value));
               }
          }

          [Conditional("XML_LOG")]
          private static void PrintXmlOutput(Command_Name command, XDocument xdoc)
          {
               string dateString = String.Format("Output {5} {0}{1}{2}{3} {4}.xml", System.DateTime.Now.Day, System.DateTime.Now.Month, System.DateTime.Now.Minute, System.DateTime.Now.Second, System.DateTime.Now.Millisecond, command.ToString());

               try {
                    xdoc.Save(dateString);
               }
               catch (IOException) {
                    //only used for debugging- not a big deal if we get this
               }
          }

          private static string ReplaceGreaterLessThan(string commandXMLstr)
          {
               //replace these so 'all access' works
               commandXMLstr = commandXMLstr.Replace("&lt;", "<");
               commandXMLstr = commandXMLstr.Replace("&gt;", ">");
               return commandXMLstr;
          }
     }
}