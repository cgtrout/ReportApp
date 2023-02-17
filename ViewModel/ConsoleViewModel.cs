using ReportApp.Console;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace ReportApp.ViewModel
{
     /// <summary>
     /// Description of ConsoleViewModel.
     /// </summary>
     public class ConsoleViewModel : WorkspaceViewModel
     {
          #region Fields

          private string _currentLine;

          private ICommand _executeCommand;

          private ICommand _partialMatchCommand;
          private ICommand _prevCommand;
          private ICommand _nextCommand;

          #endregion Fields

          #region Constructors

          public ConsoleViewModel()
          {
               base.DisplayName = "Console";
               consoleSystem = ConsoleSystem.ConsoleSystemInstance;
               CurrentLine = String.Empty;
          }

          #endregion Constructors

          #region Properties

          public ConsoleSystem consoleSystem { get; set; }

          public ObservableCollection<string> PrevCommands { get; set; } = new ObservableCollection<string>();
          public int PrevCommandLine { get; set; } = 0;

          public string CurrentLine
          {
               get
               {
                    return _currentLine;
               }
               set
               {
                    _currentLine = value;
                    OnPropertyChanged(nameof(CurrentLine));
               }
          }

          public ICommand ExecuteCommand
          {
               get
               {
                    if (_executeCommand == null) {
                         _executeCommand = new RelayCommand(x => Execute());
                    }
                    return _executeCommand;
               }
          }

          public ICommand PrevCommand
          {
               get
               {
                    if (_prevCommand == null) {
                         _prevCommand = new RelayCommand(x => PrevLine());
                    }
                    return _prevCommand;
               }
          }

          public ICommand NextCommand
          {
               get
               {
                    if (_nextCommand == null) {
                         _nextCommand = new RelayCommand(x => NextLine());
                    }
                    return _nextCommand;
               }
          }

          public ICommand PartialMatchCommand
          {
               get
               {
                    if (_partialMatchCommand == null) {
                         _partialMatchCommand = new RelayCommand(x => PartialMatch());
                    }
                    return _partialMatchCommand;
               }
          }

          #endregion Properties

          #region Methods

          /// <summary>
          /// Get partial matches based on search
          /// </summary>
          /// <param name="search">Partial string to search for</param>
          public void GetPartialMatches(string search)
          {
               consoleSystem.History.AppendLine("Matches for: " + search);
               var currentMatches = new List<string>();
               foreach (var key in consoleSystem.Commands.Keys) {
                    if (key.StartsWith(search, StringComparison.InvariantCultureIgnoreCase)) {
                         currentMatches.Add(key);
                    }
               }
               if (currentMatches.Count == 1) {
                    CurrentLine = currentMatches[0];
               } else {
                    foreach (var s in currentMatches) {
                         consoleSystem.History.AppendLine(s);
                    }
               }
          }

          private void PrevLine()
          {
               GetUserPreviousLine();
               PrevCommandLine--;
               if (PrevCommandLine < 0) {
                    PrevCommandLine = 0;
               }
          }

          private void NextLine()
          {
               GetUserPreviousLine();
               PrevCommandLine++;
               if (PrevCommandLine >= PrevCommands.Count) {
                    PrevCommandLine = PrevCommands.Count - 1;
               }
          }

          private void GetUserPreviousLine()
          {
               if (PrevCommands.Count == 0) {
                    CurrentLine = string.Empty;
               } else {
                    CurrentLine = PrevCommands[PrevCommandLine];
               }
          }

          private void Execute()
          {
               consoleSystem.WriteLine($"> {CurrentLine}\n");
               try {
                    consoleSystem.ExecuteCommand(CurrentLine);
               }
               catch (ConsoleSystemException e) {
                    consoleSystem.WriteLine(e.Message);
               }
               finally {
                    PrevCommands.Add(CurrentLine);
                    PrevCommandLine = PrevCommands.Count - 1;
                    CurrentLine = String.Empty;
               }
          }

          private void PartialMatch()
          {
               if (String.IsNullOrEmpty(CurrentLine)) {
                    return;
               }
               GetPartialMatches(CurrentLine.Split(' ')[0]);
               consoleSystem.OnPropertyChanged("History");
          }

          #endregion Methods
     }
}