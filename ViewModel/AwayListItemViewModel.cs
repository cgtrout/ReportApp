using ReportApp.Data;
using ReportApp.Model;
using System;

namespace ReportApp.ViewModel
{
     public class AwayListItemViewModel : WorkspaceViewModel
     {
          #region Fields

          private AwayList awayListItem;

          #endregion Fields

          #region Constructors

          public AwayListItemViewModel(AwayList v)
          {
               awayListItem = v;
          }

          #endregion Constructors

          #region Properties

          public AwayList AwayListInteral
          {
               get { return awayListItem; }
          }

          public long AwayListId
          {
               get
               {
                    return awayListItem.AwayListId;
               }
               set
               {
                    awayListItem.AwayListId = value;
                    OnPropertyChanged(nameof(AwayListId));
               }
          }

          public string PersonId
          {
               get
               {
                    return awayListItem.PersonId;
               }
               set
               {
                    awayListItem.PersonId = value;
                    OnPropertyChanged(nameof(PersonId));
               }
          }

          public PersonViewModel Person
          {
               get
               {
                    if (PersonId == null) {
                         return null;
                    } else {
                         return DataRepository.PersonDict[PersonId];
                    }
               }
          }

          public DateTime ReturnDate
          {
               get
               {
                    return awayListItem.ReturnDate;
               }
               set
               {
                    awayListItem.ReturnDate = value;
                    OnPropertyChanged(nameof(ReturnDate));
               }
          }

          public string Notes
          {
               get
               {
                    return awayListItem.Notes;
               }
               set
               {
                    awayListItem.Notes = value;
                    OnPropertyChanged(nameof(Notes));
               }
          }

          #endregion Properties
     }
}