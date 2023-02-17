using ReportApp.Utility;

namespace ReportApp
{
     /// <summary>
     /// Description of GlobalScheduler.
     /// </summary>
     public static class GlobalScheduler
     {
          #region Constructors

          static GlobalScheduler()
          {
               PersonUpdateQueue = new SerialTaskQueue("Person");
               AccessUpdateQueue = new SerialTaskQueue("Access");
               DBQueue = new SerialTaskQueue("DB");
               APIUpdateQueue = new SerialTaskQueue("API");

               PersonUpdateQueue.StartUpdateTask();
               AccessUpdateQueue.StartUpdateTask();
               DBQueue.StartUpdateTask();
               APIUpdateQueue.StartUpdateTask();
          }

          #endregion Constructors

          #region Properties

          public static SerialTaskQueue AccessUpdateQueue { get; set; }
          public static SerialTaskQueue APIUpdateQueue { get; set; }
          public static SerialTaskQueue DBQueue { get; set; }
          public static SerialTaskQueue PersonUpdateQueue { get; set; }

          #endregion Properties

          #region Methods

          public static void PrintAllTasks()
          {
               TraceEx.PrintLog(AccessUpdateQueue.ToString());
               TraceEx.PrintLog(APIUpdateQueue.ToString());
               TraceEx.PrintLog(DBQueue.ToString());
               TraceEx.PrintLog(PersonUpdateQueue.ToString());
          }

          #endregion Methods
     }
}