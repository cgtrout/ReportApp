using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace ReportApp.Utility
{
     internal class QueryObserver<TValue> : IDisposable
     {
          #region Fields

          private CollectionMessageHandler _messageHandler;

          #endregion Fields

          #region Constructors

          public QueryObserver()
          {
               //Dictionary = new ConcurrentDictionary<TKey, TValue>();
               Collection = new ObservableCollection<TValue>();
          }

          #endregion Constructors

          #region Properties

          //ConcurrentDictionary<TKey, TValue> Dictionary { get; set; }
          public ObservableCollection<TValue> Collection { get; set; }

          public CollectionMessageHandler MessageHandler
          {
               get { return _messageHandler; }
               set
               {
                    _messageHandler = value;
                    var dispatcher = DispatcherHelper.GetDispatcher();
                    dispatcher.Invoke(new Action(() => {
                         _messageHandler.CollectionChanged += HandleCollectionMessage;
                    }));
               }
          }

          public Predicate<TValue> Predicate { get; set; }

          #endregion Properties

          #region Methods

          public void AddQuery(List<TValue> query)
          {
               Collection = new ObservableCollection<TValue>(query);
          }

          public void Dispose()
          {
               Dispose(true);
               GC.SuppressFinalize(this);
          }

          public void HandleCollectionMessage(CollectionChangedEventArgs e)
          {
               var value = (TValue)e.ChangedValue;

               //should only process based on predicate

               if (Predicate == null) {
                    TraceEx.PrintLog("QueryObserver:Predicate is null.");
                    return;
               }
               if (Predicate(value) == false) {
                    return;
               }

               var dispatch = DispatcherHelper.GetDispatcher();
               switch (e.ChangeType) {
                    case CollectionChangeType.Add:
                         Collection.Add(value);
                         break;

                    case CollectionChangeType.Edit:
                         var foundValue = Collection.Where(x => x.Equals(value));
                         if (foundValue.Any() == false) {
                              TraceEx.PrintLog($"QueryObserver: could not find {value} ");
                         } else {
                              //copy to found object
                              var copyable = (CopyableObject)foundValue.First();
                              copyable.CopyFromOther((CopyableObject)value);
                         }
                         break;

                    case CollectionChangeType.Remove:
                         bool wasRemoved = Collection.Remove(value);
                         if (wasRemoved == false) {
                              TraceEx.PrintLog($"QueryObserver: There was a problem removing {value}");
                         }
                         break;
               }
          }

          protected virtual void Dispose(bool disposing)
          {
               if (disposing) {
                    var dispatcher = DispatcherHelper.GetDispatcher();
                    dispatcher.Invoke(new Action(() => {
                         MessageHandler.CollectionChanged -= HandleCollectionMessage;
                    }));
               }
          }

          #endregion Methods
     }
}