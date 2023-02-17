using ReportApp.Model;
using System.Collections.Generic;

namespace ReportApp.ViewModel
{
     /// <summary>
     /// Description of AddMultiplePeopleViewModel.
     /// </summary>
     public class AddMultiplePeopleViewModel : WorkspaceViewModel
     {
          #region Constructors

          public AddMultiplePeopleViewModel()
          {
               base.DisplayName = "Add Mult People";
          }

          #endregion Constructors

          #region Properties

          public List<string> CompanyList { get; set; }
          public List<Person> PersonList { get; set; }

          #endregion Properties
     }
}