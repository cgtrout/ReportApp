using ReportApp.Model;
using ReportApp.ViewModel;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace ReportApp.Data
{
     public static class PhoneDataImporter
     {
          #region Methods

          public static void ImportPhoneData()
          {
               //prompt user for location
               var file = GetFilePath();

               if (string.IsNullOrEmpty(file)) {
                    MessageBox.Show("No file selected.  Data was not imported.", "Warning");
                    return;
               }

               //initialize database and clear phoneinfotable
               var database = new PhoneDatabase(PathSettings.Default.PhoneDatabasePath);
               database.ClearEntries();

               //use csv importer to get data
               List<PhoneCsvEntry> phoneInfo = CSVImporter.ParseFile<PhoneCsvEntry>(file);

               //gather any that couldn't be matched into a list
               var notMatched = new List<PhoneCsvEntry>();

               var personDict = DataRepository.PersonDict;
               int phoneId = 0;

               foreach (var entry in phoneInfo) {
                    //try to find matching person in main database
                    var personQuery = personDict.Values.Where(x => x.LastName.ToLower() == entry.LastName.ToLower() && x.FirstName == entry.FirstName && x.IsNetbox == true);

                    PersonViewModel person = null;

                    //if not matched, put in list
                    if (personQuery.Any() == false) {
                         notMatched.Add(entry);
                         person = new PersonViewModel(new Person());
                         person.PersonId = string.Empty;
                    } else {
                         person = personQuery.First();
                    }
                    //add to output list
                    var newPhoneInfoItem = new PhoneInfo();
                    newPhoneInfoItem.PhoneInfoId = phoneId++;
                    newPhoneInfoItem.PersonId = person.PersonId;
                    newPhoneInfoItem.Pager = entry.Pager;
                    newPhoneInfoItem.WorkNumber = entry.WorkNumber;
                    newPhoneInfoItem.HomeNumber = entry.HomeNumber;
                    newPhoneInfoItem.CellNumber = entry.CellNumber;
                    newPhoneInfoItem.ImportedName = $"{entry.LastName}, {entry.FirstName}";
                    newPhoneInfoItem.FullName = $"{entry.LastName.ToUpper()}, {entry.FirstName}";
                    database.GetContext().PhoneInfo.InsertOnSubmit(newPhoneInfoItem);
               }

               //submit to database
               database.GetContext().SubmitChanges();

               //warn user of any that couldn't be found
               var outStr = string.Empty;
               foreach (var v in notMatched) {
                    outStr += $"{v.LastName}, {v.FirstName} \n";
               }
               MessageBox.Show("Couldn't find the following people in Netbox:\n " + outStr, "Warning");
          }

          private static string GetFilePath()
          {
               var dlg = new Microsoft.Win32.OpenFileDialog();
               dlg.FileName = "Phonelist.csv";
               dlg.DefaultExt = ".CSV";
               dlg.Filter = "CSV Documents (.CSV)|*.CSV";

               var result = dlg.ShowDialog();

               if (result == true) {
                    return dlg.FileName;
               } else { }
               return string.Empty;
          }

          #endregion Methods
     }

     public class PhoneCsvEntry
     {
          #region Properties

          public string LastName { get; set; }
          public string FirstName { get; set; }
          public string Pager { get; set; }
          public string WorkNumber { get; set; }
          public string HomeNumber { get; set; }
          public string CellNumber { get; set; }

          #endregion Properties
     }
}