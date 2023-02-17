using CefSharp;
using ReportApp;
using ReportApp.Model;
using ReportApp.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace ReportApp.Utility
{
     /// <summary>
     /// SerialTaskQueue allows a set of Tasks to be run in serial, one after another
     /// </summary>
     public class SerialTaskQueue
     {
          #region Fields

          private List<TaskWithName> taskList;
          private TextWriter _logFile;

          #endregion Fields

          #region Constructors

          public SerialTaskQueue(string name)
          {
               taskList = new List<TaskWithName>();
               Name = name;
          }

          #endregion Constructors

          #region Properties

          public TaskWithName RunningTask { get; private set; } = null;
          public string Name { get; set; }

          /// <summary>
          /// Write to Trace file if set to true
          /// </summary>
          public bool WriteToTrace { get; set; } = false;

          private TextWriter LogFile
          {
               get
               {
                    if (_logFile == null) {
                         var filename = $"STQ {Name} {DateTime.Now.ToString("yyyy MM dd mm ss")}.txt";
                         _logFile = File.CreateText(filename);
                    }

                    return _logFile;
               }
          }

          #endregion Properties

          #region Methods

          public TaskWithName[] GetArray()
          {
               lock (taskList) {
                    return taskList.ToArray();
               }
          }

          /// <summary>
          /// Queue Task to the end of queue
          /// </summary>
          /// <param name="name">Name of Task</param>
          /// <param name="t">Task to run</param>
          public void QueueTaskEnd(string name, Task t)
          {
               var task = new TaskWithName() { Name = name, task = t };
               lock (taskList) {
                    taskList.Add(task);
               }
               //PrintAllTasks();
          }

          /// <summary>
          /// Schedule task to execute at earliest time possible
          /// </summary>
          /// <param name="t">Task to run</param>
          /// <param name="name">Name of Task</param>
          public void QueueTaskPriority(string name, Task t)
          {
               TaskWithName task = new TaskWithName { Name = name, task = t };

               //var taskArray = taskList.ToArray();

               lock (taskList) {
                    for (int i = 0; i < taskList.Count; i++) {
                         //find first task that is running
                         var status = taskList[i].task.Status;
                         if (status == TaskStatus.Created) {
                              DBLoadStatus.WriteLine("Insert before");
                              taskList.Insert(i, task);
                              return;
                         }
                         if (status == TaskStatus.Created || status == TaskStatus.Running || status == TaskStatus.WaitingToRun || status == TaskStatus.WaitingForActivation) {
                              DBLoadStatus.WriteLine("Inserting");
                              //place task after this task
                              taskList.Insert(i + 1, task);
                              return;
                         }
                    }
               }
               DBLoadStatus.WriteLine("Adding to end");
               //if none are running simply place at end of list and run
               QueueTaskEnd(name, t);
          }

          public void RemoveAll()
          {
               //PrintAllTasks();
               lock (taskList) {
                    //remove completed tasks from llis
                    taskList.RemoveAll(item => item.task.Status != TaskStatus.Running
                                                  && item.task.Status != TaskStatus.WaitingForActivation
                                                  && item.task.Status != TaskStatus.WaitingToRun);
               }
          }

          /// <summary>
          /// Start running background task that will update the background
          /// tasks
          /// </summary>
          ///
          //TODO add way to cancel this task
          public void StartUpdateTask()
          {
               var task = Task.Factory.StartNew(async () => {
                    try {
                         while (true) {
                              UpdateTasks();
                              await Task.Delay(TimeSpan.FromMilliseconds(10));
                         }
                    }
                    catch (Exception e) {
                         string message = "Program has failed in UpdateTask. \n " + "Exception caught: " + ExceptionUtility.GetExceptionText(e);
                         var now = DateTime.Now;
                         using (var f = File.CreateText(String.Format("UpdateTask Exception {0} {1} {2} {3} {4}.txt", now.Month, now.Day, now.Hour, now.Minute, now.Millisecond))) {
                              if (taskList.Any()) {
                                   var tList = this.taskList.ToArray();
                                   var first = tList.First();
                                   f.WriteLine(String.Format("Task {0} has failed on a exception.\n", first.Name));
                              }
                              f.WriteLine(ExceptionUtility.GetExceptionText(e));
                              f.WriteLine(ToString());
                         }
                         Trace.WriteLine(message);
                         message += "\nProgram will now close";
                         ShutDownProgram(message);
                    }
               }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
          }

          public override string ToString()
          {
               StringWriter sw = new StringWriter();
               lock (taskList) {
                    int i = 0;
                    foreach (var t in taskList) {
                         sw.WriteLine(String.Format("{2} {0} {1} {3}", t.Name, t.task.Status, i, t.startTime));
                         i++;
                    }
               }
               return sw.ToString();
          }

          private static void ShutDownProgram(string message)
          {
               System.Windows.MessageBox.Show(message, "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);

               DispatcherHelper.GetDispatcher().Invoke(() => {
                    GlobalScheduler.PrintAllTasks();
                    Cef.Shutdown();
                    Application.Current.Dispatcher.BeginInvokeShutdown(System.Windows.Threading.DispatcherPriority.Render);
                    Application.Current.Shutdown();
                    System.Environment.Exit(0);
               });
          }

          /// <summary>
          /// Are all of the tasks completed?
          /// </summary>
          /// <returns>bool if all have completed</returns>
          private bool AllAreDone()
          {
               lock (taskList) {
                    foreach (var t in taskList) {
                         if (t.task.Status == TaskStatus.Running || t.task.Status == TaskStatus.WaitingToRun || t.task.Status == TaskStatus.Created || t.task.Status == TaskStatus.WaitingForActivation) {
                              return false;
                         }
                    }
               }
               return true;
          }

          private void PrintAllTasks()
          {
               TraceEx.PrintLog("All Running Tasks:\n");
               Trace.WriteLine(ToString());
          }

          [Conditional("DEBUG")]
          private void PrintLog(string s)
          {
               LogFile.WriteLine(s);
               LogFile.Flush();

               if (WriteToTrace) {
                    TraceEx.PrintLog("__SerialTaskQueue: " + s);
               }
          }

          private void UpdateTasks()
          {
          begin:
               if (AllAreDone()) {
                    return;
               }
               //attempt to fix System.ArgumentException
               //Destination array was not long enough.
               TaskWithName[] tempList = new TaskWithName[0];
               lock (taskList) {
                    tempList = taskList.ToArray();
               }
               foreach (var t in tempList) {
                    var status = t.task.Status;
                    //if a task is running or about to run then simply exit
                    if (status == TaskStatus.Running || status == TaskStatus.WaitingForActivation || status == TaskStatus.WaitingToRun) {
                         break;
                    }
                    if (status == TaskStatus.Created) {
                         //found first task to start so start it
                         PrintLog($"Starting task: {t.Name}");
                         PrintLog($"{DateTime.Now.ToShortDateString()} {DateTime.Now.ToLongTimeString()}");

                         Stopwatch sw = new Stopwatch();
                         sw.Start();

                         RunningTask = t;
                         t.startTime = DateTime.Now;
                         t.task.Start();
                         t.task.Wait();
                         RunningTask = null;

                         sw.Stop();
                         PrintLog($"    ENDING task: \t\t{sw.ElapsedMilliseconds}ms");

                         break;
                    }
               }
               lock (taskList) {
                    //remove completed tasks from llis
                    taskList.RemoveAll(item => item.task.Status == TaskStatus.RanToCompletion || item.task.Status == TaskStatus.Faulted);
               }
               goto begin;
          }

          #endregion Methods
     }

     /// <summary>
     /// Allows task to have an associated name
     /// </summary>
     public class TaskWithName
     {
          #region Fields

          public DateTime startTime;
          private static int globalId = 0;

          #endregion Fields

          #region Constructors

          public TaskWithName()
          {
               Id = globalId++;
          }

          #endregion Constructors

          #region Properties

          public int Id { get; set; }
          public string Name { get; set; }
          public Task task { get; set; }

          #endregion Properties
     }
}