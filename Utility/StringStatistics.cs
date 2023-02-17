using System;
using System.Collections.Generic;

namespace ReportApp.Utility
{
     public class StringSearchResult
     {
          #region Properties

          /// <summary>
          /// How many changes it took to get to this
          /// </summary>
          public int Distance { get; set; } = 0;

          /// <summary>
          /// Matched word
          /// </summary>
          public string Word { get; set; } = String.Empty;

          #endregion Properties
     }

     public class StringStatistics
     {
          #region Methods

          /// <summary>
          /// Calculate how many "changes" it takes to convert from one string to the other
          /// </summary>
          /// <param name="s1"></param>
          /// <param name="s2"></param>
          /// <returns></returns>
          public static int CalculateStringDistance(string s1, string s2)
          {
               return CalculateStringDistance(s1, s2, s1.Length, s2.Length);
          }

          /// <summary>
          /// Gets potential matches
          /// </summary>
          /// <param name="searchWord">Word to search for</param>
          /// <param name="wordsToSearch">List of words to compare to</param>
          /// <returns>List of StringSearchResult of potential matches</returns>
          public static List<StringSearchResult> GetMatches(string searchWord, List<string> wordsToSearch)
          {
               var searchLength = searchWord.Length;
               var retList = new List<StringSearchResult>();
               int count = 0;
               int distanceCutoff = 2;

               //set distance cutoff (based on length of string)
               if (searchWord.Length <= 4) {
                    distanceCutoff = 1;
               }

               searchWord = searchWord.Trim(' ');

               if (string.IsNullOrEmpty(searchWord)) {
                    return retList;
               }

               foreach (var name in wordsToSearch) {
                    count++;
                    if (String.IsNullOrEmpty(name) || searchWord == name) {
                         continue;
                    }
                    char nameChar = Char.ToLower(name[0]);
                    char searchChar = Char.ToLower(searchWord[0]);
                    //char nameChar = name[0];
                    //char searchChar = searchWord[0];

                    //since first letters must match, once we pass the first letter we can abort the search
                    if ((int)nameChar > (int)searchChar) {
                         break;
                    }

                    //first letter should be the same
                    if (nameChar != searchChar) {
                         continue;
                    }

                    bool containsOther = false;

                    if (name.Length < searchLength) {
                         containsOther = name.ToLower().Contains(searchWord);
                    }

                    var distance = StringStatistics.CalculateStringDistance(searchWord.ToLower(), name.ToLower());

                    if (containsOther || distance <= distanceCutoff) {
                         retList.Add(new StringSearchResult() { Word = name, Distance = distance });
                    }
               }

               return retList;
          }

          private static int CalculateStringDistance(string str1, string str2, int m, int n)
          {
               // Create a table to store results of subproblems
               int[,] dp = new int[m + 1, n + 1];

               // Fill d[][] in bottom up manner
               for (int i = 0; i <= m; i++) {
                    for (int j = 0; j <= n; j++) {
                         // If first string is empty, only option is to
                         // isnert all characters of second string
                         if (i == 0)
                              dp[i, j] = j;  // Min. operations = j

                         // If second string is empty, only option is to
                         // remove all characters of second string
                         else if (j == 0)
                              dp[i, j] = i; // Min. operations = i

                         // If last characters are same, ignore last char
                         // and recur for remaining string
                         else if (str1[i - 1] == str2[j - 1])
                              dp[i, j] = dp[i - 1, j - 1];

                         // If last character are different, consider all
                         // possibilities and find minimum
                         else
                              dp[i, j] = 1 + min(dp[i, j - 1],  // Insert
                                             dp[i - 1, j],  // Remove
                                             dp[i - 1, j - 1]); // Replace
                    }
               }

               return dp[m, n];
          }

          private static int min(int x, int y, int z)
          {
               return Math.Min(Math.Min(x, y), z);
          }

          #endregion Methods
     }
}