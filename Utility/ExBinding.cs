using System.Windows.Controls;
using System.Windows.Data;

namespace ReportApp.Utility
{
     /// <summary>
     /// ExBinding
     ///  - WPF Binding with ExceptionValidation set by default
     /// </summary>
     public class ExBinding : Binding
     {
          #region Constructors

          public ExBinding()
          {
               NotifyOnValidationError = true;
               ValidationRules.Add(new ExceptionValidationRule());
               ValidationRules.Add(new DataErrorValidationRule());
          }

          #endregion Constructors
     }
}