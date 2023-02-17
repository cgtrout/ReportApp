using System;

namespace ReportApp.Utility
{
     public delegate void CollectionChangedEventHandler(CollectionChangedEventArgs e);

     public enum CollectionChangeType { Add, Edit, Remove }

     public class CollectionChangedEventArgs : EventArgs
     {
          #region Fields

          public object ChangedValue;
          public CollectionChangeType ChangeType;

          #endregion Fields
     }

     public class CollectionMessageHandler
     {
          #region Events

          public event CollectionChangedEventHandler CollectionChanged;

          #endregion Events

          #region Methods

          public void OnCollectionChanged(CollectionChangedEventArgs e)
          {
               //DispatcherHelper.GetDispatcher().BeginInvoke(new Action(() => {
               if (CollectionChanged != null) {
                    CollectionChanged(e);
               }
               //}), DispatcherPriority.Render);
          }

          #endregion Methods
     }
}