using System;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;

namespace ReportApp.Utility
{
     public delegate void DictionaryChangedEventHandler(DictionaryChangedEventArgs e);

     public class DictionaryChangedEventArgs : EventArgs
     {
          #region Fields

          public Object ChangedValue;
          public bool Edit = false;
          public Object OriginalValue;
          public bool Remove = false;

          #endregion Fields
     }

     /// <summary>
     /// Observes a ConcurrentDictionary - mirrors changes to an observable collection that can be bound to
     /// </summary>
     public class DictionaryObserver<TKey, TValue>
     {
          #region Constructors

          public DictionaryObserver(ConcurrentDictionary<TKey, TValue> dict)
          {
               Dictionary = dict;
               Collection = new ObservableCollection<TValue>(dict.Values);
               //foreach(var v in dict.Values) {
               //     Collection.Add(v);
               //}
          }

          #endregion Constructors

          #region Properties

          public ObservableCollection<TValue> Collection { get; set; }
          public ConcurrentDictionary<TKey, TValue> Dictionary { get; set; }

          #endregion Properties

          #region Methods

          //must be linked to DictionaryChanged event
          public void DataRepository_ValueChanged(DictionaryChangedEventArgs e)
          {
               TValue val = (TValue)e.ChangedValue;

               if (e.Edit == false && e.Remove == false) {
                    Collection?.Add((TValue)e.ChangedValue);
               } else if (e.Remove == true) {
                    Collection?.Remove((TValue)e.ChangedValue);
               } else {
                    var elem = Collection?.Where(x => x.Equals(e.OriginalValue));
                    if (elem != null && elem.Any()) {
                         try {
                              var first = elem.First();
                              TraceEx.PrintLog($"Edit=true elem {elem.First()} found");

                              CopyableObject obj = (CopyableObject)first;
                              obj.CopyFromOther((TValue)e.ChangedValue);
                         }
                         catch (InvalidOperationException) {
                              //need to catch this - occurs if user closes window before message is handled
                              Trace.TraceError("Invalid Operation Exception!");
                              TraceEx.PrintLog("DataRepository_ValueChanged exception caught");
                              return;
                         }
                    }
               }
          }

          #endregion Methods
     }
}