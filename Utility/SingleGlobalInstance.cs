using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;

internal class SingleGlobalInstance : IDisposable
{
     #region Fields

     public bool hasHandle = false;
     private Mutex mutex;

     #endregion Fields

     #region Constructors

     public SingleGlobalInstance(int timeOut)
     {
          InitMutex();
          try {
               if (timeOut < 0)
                    hasHandle = mutex.WaitOne(Timeout.Infinite, false);
               else
                    hasHandle = mutex.WaitOne(timeOut, false);

               if (hasHandle == false)
                    throw new TimeoutException("Timeout waiting for exclusive access on SingleInstance");
          }
          catch (AbandonedMutexException) {
               hasHandle = true;
          }
     }

     #endregion Constructors

     #region Methods

     public void Dispose()
     {
          if (mutex != null) {
               if (hasHandle)
                    mutex.ReleaseMutex();
               mutex.Dispose();
          }
     }

     private void InitMutex()
     {
          //string appGuid = ((GuidAttribute)Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(GuidAttribute), false).GetValue(0)).Value.ToString();
          var assembly = Assembly.GetExecutingAssembly();
          var attributes = assembly.GetCustomAttributes(typeof(GuidAttribute), false);
          var value = attributes.GetValue(0);
          var appGuid = value.ToString();

          string mutexId = string.Format("Global\\{{{0}}}", appGuid);
          mutex = new Mutex(false, mutexId);

          var allowEveryoneRule = new MutexAccessRule(new SecurityIdentifier(WellKnownSidType.WorldSid, null), MutexRights.FullControl, AccessControlType.Allow);
          var securitySettings = new MutexSecurity();
          securitySettings.AddAccessRule(allowEveryoneRule);
          mutex.SetAccessControl(securitySettings);
     }

     #endregion Methods
}