using System;
using System.Threading;
using System.Threading.Tasks;

namespace ReportApp.Utility
{
     //How to use:
     //  AsyncHelper.RunSync(() => DoAsyncStuff());
     public static class AsyncHelper
     {
          #region Fields

          private static readonly TaskFactory _taskFactory = new
              TaskFactory(CancellationToken.None,
                          TaskCreationOptions.None,
                          TaskContinuationOptions.None,
                          TaskScheduler.Default);

          #endregion Fields

          #region Methods

          public static TResult RunSync<TResult>(Func<Task<TResult>> func)
              => _taskFactory
                  .StartNew(func)
                  .Unwrap()
                  .GetAwaiter()
                  .GetResult();

          public static void RunSync(Func<Task> func)
              => _taskFactory
                  .StartNew(func)
                  .Unwrap()
                  .GetAwaiter()
                  .GetResult();

          #endregion Methods
     }
}