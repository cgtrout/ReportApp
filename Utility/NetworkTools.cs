using ReportApp.Console;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace ReportApp.Utility
{
     public static class NetworkTools
     {
          public static bool PingHost(string nameOrAddress)
          {
               bool pingable = false;
               Ping pinger;

               using (pinger = new Ping()) {
                    try {
                         PingReply reply = pinger.Send(nameOrAddress);
                         pingable = (reply.Status == IPStatus.Success);
                    }
                    catch (PingException) {
                         return false;
                    }
               }
               
               return pingable;
          }

          class PingTableElement
          {
               public string Name { get; set; }
               public string Address { get; set; }
          }

          static List<PingTableElement> PingTableList = new List<PingTableElement>() {
               new PingTableElement() { Name = "Netbox",         Address="192.168.0.200" },
               new PingTableElement() { Name = "Admin Module",   Address="192.168.0.201" },
               new PingTableElement() { Name = "CB Module",      Address="192.168.0.202" },
               new PingTableElement() { Name = "CP Module",      Address="192.168.0.203" },
               new PingTableElement() { Name = "Admin Camera",   Address="192.168.0.210" },
               new PingTableElement() { Name = "CP Camera",      Address="192.168.0.211" }
          };

          public static string PingAll()
          {
               string results = $"PING TEST RESULTS \n {DateTime.Now} \n\n";

               foreach(var v in PingTableList) {
                    bool res = PingHost(v.Address);
                    string line = $"{v.Name, -15} {v.Address,10} pingable = {res} \n";
                    results += line;
               }

               results += "\n";

               return results;
          }

          private static DateTime LastLogTime = DateTime.Now.AddMinutes(-2);

          /// <summary>
          /// Log to trace and console
          /// </summary>
          public static void PingLog(bool executeTimeCheck = true)
          {
               if(executeTimeCheck) {
                    if ((DateTime.Now - LastLogTime).TotalMinutes < 1) {
                         return;
                    }
               }
               
               string pingResults = PingAll();
               Trace.TraceWarning(pingResults);
               ConsoleSystem.ConsoleSystemInstance.WriteLine(pingResults);
               LastLogTime = DateTime.Now;
          }
     }
}
