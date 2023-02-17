using System;
using System.Windows.Input;

namespace ReportApp.ViewModel
{
     /// <summary>
     /// This ViewModelBase subclass requests to be removed
     /// from the UI when its CloseCommand executes.
     /// This class is abstract.
     /// </summary>
     public abstract class WorkspaceViewModel : ViewModelBase
     {
          #region Fields

          private RelayCommand _closeCommand;
          private bool _isBusy = false;
          private bool _isVisible = true;

          #endregion Fields

          #region Constructors

          protected WorkspaceViewModel()
          {
          }

          #endregion Constructors

          #region Events

          /// <summary>
          /// Raised when this workspace should be removed from the UI.
          /// </summary>
          public event EventHandler RequestClose;

          #endregion Events

          #region Properties

          /// <summary>
          /// Returns the command that, when invoked, attempts
          /// to remove this workspace from the user interface.
          /// </summary>
          public ICommand CloseCommand
          {
               get
               {
                    if (_closeCommand == null)
                         _closeCommand = new RelayCommand(param => this.OnRequestClose());

                    return _closeCommand;
               }
          }

          /// <summary>
          /// IsBusy - used to display a busy indicator
          /// </summary>
          [System.Xml.Serialization.XmlIgnore]
          public bool IsBusy
          {
               get { return _isBusy; }
               set
               {
                    _isBusy = value;
                    OnPropertyChanged(nameof(IsBusy));
               }
          }

          [System.Xml.Serialization.XmlIgnore]
          public bool IsVisible
          {
               get { return _isVisible; }
               set
               {
                    _isVisible = value;
                    if (value == false) {
                         OnRequestClose();
                         return;
                    }
                    OnPropertyChanged(nameof(IsVisible));
               }
          }

          /// <summary>
          /// If set to true, only one of these viewmodels can be open at a time
          /// </summary>
          [System.Xml.Serialization.XmlIgnore]
          public bool OnlyOneCanRun { get; protected set; } = false;

          #endregion Properties

          #region Methods

          public virtual void OnRequestClose()
          {
               EventHandler handler = this.RequestClose;
               if (handler != null)
                    handler(this, EventArgs.Empty);
          }

          #endregion Methods
     }
}