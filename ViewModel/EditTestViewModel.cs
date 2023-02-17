using ReportApp.Model;

namespace ReportApp.ViewModel
{
     /// <summary>
     /// Description of EditTestViewModel.
     /// </summary>
     public class EditTestViewModel : WorkspaceViewModel
     {
          #region Constructors

          public EditTestViewModel()
          {
               Model = new EditTest();
          }

          #endregion Constructors

          #region Properties

          public EditTest Model { get; set; }

          public string TestString
          {
               get { return Model.TestString; }
          }

          #endregion Properties
     }
}