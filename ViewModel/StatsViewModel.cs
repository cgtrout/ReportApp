using ReportApp.Data;
using System;
using System.Linq;

namespace ReportApp.ViewModel
{
     public class StatsViewModel : WorkspaceViewModel
     {
          #region Fields

          private int _keysAuthorized;

          private int _phoneCalls;

          private DateTime _selectedDate;

          #endregion Fields

          #region Constructors

          public StatsViewModel()
          {
               base.DisplayName = "Stats Sheet";

               //offset date
               double offset = 0.0f;
               DateTime now = DateTime.Now;
               DateTime today = DateTime.Now.Date;
               DateTime tommorow = today.AddDays(1);
               DateTime midnight = new DateTime(today.Year, today.Month, today.Day, 0, 0, 0);
               DateTime morning = new DateTime(today.Year, today.Month, today.Day, 7, 0, 0);
               if (now > midnight && now < morning) {
                    offset = -1.0;
               }

               SelectedDate = DateTime.Now.Date.AddDays(offset);
          }

          #endregion Constructors

          #region Properties

          public int KeysAuthorized
          {
               get { return _keysAuthorized; }
               set
               {
                    _keysAuthorized = value;
                    OnPropertyChanged(nameof(PhoneCalls));
               }
          }

          public int LevelOnes
          {
               get
               {
                    return GetOrientations("1");
               }
          }

          public int LevelTwos
          {
               get
               {
                    return GetOrientations("2");
               }
          }

          public int PhoneCalls
          {
               get { return _phoneCalls; }
               set
               {
                    _phoneCalls = value;
                    OnPropertyChanged(nameof(PhoneCalls));
               }
          }

          public DateTime SelectedDate
          {
               get { return _selectedDate; }
               set
               {
                    _selectedDate = value;
                    OnPropertyChanged(nameof(SelectedDate));
                    OnPropertyChanged(nameof(SelectedDateText));
                    OnPropertyChanged(nameof(LevelOnes));
                    OnPropertyChanged(nameof(LevelTwos));
                    OnPropertyChanged(nameof(VisitorCount));
                    OnPropertyChanged(nameof(VehicleCount));
               }
          }

          public string SelectedDateText
          {
               get
               {
                    return SelectedDate.ToShortDateString();
               }
          }

          public int VehicleCount
          {
               get
               {
                    using (var db = VehicleDatabase.GetReadOnlyInstance()) {
                         return db.GetVehicleCount(SelectedDate);
                    }
               }
          }

          public int VisitorCount
          {
               get
               {
                    NetboxDatabase db = NetboxDatabase.GetReadOnlyInstance();
                    var query = (from x in db.GetContext().ShiftEntries
                                 join y in db.GetContext().People
                                    on x.PersonId equals y.PersonId
                                 where x.InTime > SelectedDate && x.InTime < SelectedDate.Date.AddHours(24)
                                    && y.Company != ""
                                    && (y.LastName.ToLower().Contains("test") == false)
                                 select x.PersonId).Distinct();

                    if (query.Any()) {
                         return query.ToList().Count();
                    } else {
                         return 0;
                    }
               }
          }

          #endregion Properties

          #region Methods

          private int GetOrientations(string number)
          {
               var query = from x in DataRepository.PersonDict.Values
                           where x.OrientationDate == SelectedDate && x.OrientationLevel.Contains(number) && x.OrientationNumber != 0
                           select x;

               if (query.Any()) {
                    return query.Count();
               } else {
                    return 0;
               }
          }

          #endregion Methods
     }
}