using System;
using System.Collections.Generic;
using System.Linq;

namespace ReportApp.Utility
{
     public class PinHistogram
     {
          #region Fields

          private List<Bin> bins;

          #endregion Fields

          #region Methods

          /// <summary>
          /// Reset bins for new additions
          /// </summary>
          public void IntitializeBins()
          {
               bins = new List<Bin>(100);
               TotalPinCount = 0;

               for (int i = 0; i < 100; i++) {
                    Bin bin = new Bin();
                    bin.StartNumber = i * 100;
                    bins.Add(bin);
               }
          }

          /// <summary>
          /// Increase count in histogram bin
          /// </summary>
          /// <param name="pin"></param>
          public void SetPin(long pin)
          {
               if (pin == 0 || pin > 9999) {
                    return;
               }

               if (pin < 0) {
                    throw new ArgumentException("Pin must be between 1 or greater");
               }

               //increment count in correct bin
               int bin = (int)Math.Floor((double)pin / 100);
               bins[bin].Count++;
               IncrementTotalPinCount();
          }

          /// <summary>
          /// Get a list of bins sorted by count
          /// </summary>
          /// <returns></returns>
          public List<Bin> GetSortedList()
               => bins.OrderBy(x => x.Count).ToList();


          /// <summary>
          /// How many pins total have been assigned
          /// </summary>
          public int TotalPinCount { get; private set; }
          
          /// <summary>
          /// Increment pint count by one
          /// </summary>
          private void IncrementTotalPinCount() => TotalPinCount++;

          #endregion Methods
     }

     /// <summary>
     /// Bin
     /// - one bin of hisogram
     /// </summary>
     public class Bin
     {
          #region Properties

          //number this range starts at
          public int StartNumber { get; set; } = 0;

          //how many are in this bin
          public int Count { get; set; } = 0;

          #endregion Properties
     }
}