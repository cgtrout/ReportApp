using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.Linq;
using System.Data.SQLite;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace ReportApp.Data
{
     /// <summary>
     /// Table of TableColDefs
     ///  - used to generate sqlite table if file does not exist
     /// </summary>
     public class TableDefinition
     {
          public string Name { get; }
          public List<TableColDef> List { get; }

          public TableDefinition(string name)
          {
               this.Name = name;
               List = new List<TableColDef>();
          }

          /// <summary>
          /// Add a new tabledef to table
          /// </summary>
          /// <param name="_n">name to add</param>
          /// <param name="_t">data type of column</param>
          public void Add(string n, string t)
          {
               List.Add(new TableColDef(n, t));
          }
     }

     /// <summary>
     /// One column definition
     /// </summary>
     public class TableColDef
     {
          public TableColDef(string n, string t)
          {
               Name = n;
               Type = t;
          }

          /// <summary>
          /// Name of column
          /// </summary>
          public string Name { get; set; }

          /// <summary>
          /// Data type of column
          /// </summary>
          public string Type { get; set; }
     }

     /// <summary>
     /// Base database - other databases extend off this
     /// </summary>
     public abstract class ReportAppBaseDatabase : IDisposable
     {
          internal System.Data.SQLite.SQLiteConnection connection;
          internal DataContext context;

          //string of last sql command - for debugging
          public string LastCommandText { get; set; }

          /// <summary>
          /// Connect to data
          /// </summary>
          /// <param name="connectionString">Connection string containing sqlite parameters</param>
          protected void Connect(string connectionString)
          {
               connection = new SQLiteConnection(connectionString) {
                    BusyTimeout = 100,
                    DefaultTimeout = 100
               };
               connection.Open();
               
               //connection.Trace += Connection_Trace;
          }

          private void Connection_Trace(object sender, TraceEventArgs e)
          {
               Debug.WriteLine(e.Statement);
          }

          /// <summary>
          /// Close the connection
          /// </summary>
          public void Close()
          {
               connection.Close();
          }

          /// <summary>
          /// Creates all tables for application
          /// </summary>
          public abstract void CreateTables();

          /// <summary>
          /// Submit changes to context
          /// </summary>
          public abstract void SubmitChanges();

          public void EnableObjectTracking() => context.ObjectTrackingEnabled = true;

          public void DisableObjectTracking() => context.ObjectTrackingEnabled = false;

          /// <summary>
          /// Route sqlite logs to debug output
          /// </summary>
          public void SetDebugText()
          {
#if DEBUG
			context.Log = new DebugTextWriter();
#endif
          }

          protected void SetLastCommandText<T>(IQueryable<T> e)
          {
               DbCommand dc = context.GetCommand(e);
               LastCommandText = dc.CommandText;
          }


          /// <summary>
          /// Create Table from TableDefinition
          /// </summary>
          [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Def is null checked")]
          public void CreateTable(TableDefinition def)
          {
               if (def is null) throw new ArgumentNullException(nameof(def));

               string sql = String.Format("create table if not exists {0} (", def.Name);
               int i = 0;
               foreach (var t in def.List) {
                    sql += t.Name + " " + t.Type;
                    if (i++ != def.List.Count - 1) {
                         sql += ",";
                    }
               }
               sql += ")";
               try {
                    var command = new SQLiteCommand(sql, connection);
                    command.ExecuteNonQuery();
               }
               catch (SQLiteException e) {
                    Debug.Write("Error writing table in CreateTable: " + e.Message);
               }
          }

          /// <summary>
          /// Deletes all entries from a table
          /// </summary>
          /// <param name="tablename">Table to clear</param>
          public void ClearTable(string tablename)
          {
               ExecuteSQL(String.Format("delete from {0}", tablename));
          }

          /// <summary>
          /// Runs sql string
          /// </summary>
          /// <param name="sql">sql string to execute</param>
          private void ExecuteSQL(string sql)
          {
               using (var command = new SQLiteCommand(sql, connection)) {
                    command.ExecuteNonQuery();
               }
          }

          public void Dispose()
          {
               Dispose(true);
               GC.SuppressFinalize(this);
          }

          private bool disposed = false;

          protected virtual void Dispose(bool disposing)
          {
               if (disposed) return;

               if (disposing) {
               }
               //if(connection.State==

               connection.Close();
               context.Dispose();

               disposed = true;
          }
     }

#pragma warning disable CA1812 //warning for no use
     internal class DebugTextWriter : System.IO.TextWriter
     {
          public override void Write(char[] buffer, int index, int count)
          {
               System.Diagnostics.Debug.Write(new string(buffer, index, count));
          }

          public override void Write(string value)
          {
               System.Diagnostics.Debug.Write(value);
          }

          public override System.Text.Encoding Encoding
          {
               get
               {
                    return System.Text.Encoding.Default;
               }
          }
     }
}
#pragma warning restore CA1812