using System;
using System.Windows.Input;

namespace ReportApp.ViewModel
{
     /// <summary>
     /// Represents an actionable item displayed by a View.
     /// </summary>
     public class CommandViewModel : ViewModelBase
     {
          #region Constructors

          public CommandViewModel(string displayName, ICommand command)
          {
               if (command == null)
                    throw new ArgumentNullException(nameof(command));

               base.DisplayName = displayName;
               this.Command = command;
          }

          #endregion Constructors

          #region Properties

          public ICommand Command { get; private set; }

          #endregion Properties
     }
}