using System.Collections.Generic;

namespace ReportApp.Utility
{
     public class FlaggedList
     {
          #region Fields

          private Dictionary<long, bool> flagDict;

          #endregion Fields

          #region Constructors

          public FlaggedList()
          {
               flagDict = new Dictionary<long, bool>();
          }

          #endregion Constructors

          #region Methods

          /// <summary>
          /// Set flag at index
          /// </summary>
          /// <param name="index"></param>
          public void Add(long index)
          {
               flagDict[index] = true;
          }

          public bool Get(long index)
          {
               if (flagDict.ContainsKey(index)) {
                    return true;
               } else {
                    return false;
               }
          }

          #endregion Methods
     }
}