using System;
using System.IO;
using System.Windows.Forms;

namespace ReportApp.ViewModel
{
     internal class HelpFileViewModel : WorkspaceViewModel
     {
          #region Fields

          private string _fileName;
          private string _rtfDocument;
          private string _helpImage;

          #endregion Fields

          #region Properties

          public string FileName
          {
               get
               {
                    return _fileName;
               }
               set
               {
                    _fileName = value;

                    try {
                         var ext = _fileName.Split('.')[1];

                         if (ext == "rtf") {
                              RtfDocument = File.ReadAllText(PathSettings.Default.HelpPath + _fileName);
                         } else if (ext == "tif") {
                              HelpImage = value;
                         }

                         base.DisplayName = _fileName.Split('.')[0];
                    }
                    catch (Exception e) {
                         var errorMsg = $"Problem showing help file: {e.GetType()} {e.Message}";
                         MessageBox.Show(errorMsg);
                    }
                    OnPropertyChanged(nameof(FileName));
               }
          }

          public string RtfDocument
          {
               get
               {
                    return _rtfDocument;
               }
               set
               {
                    _rtfDocument = value;
                    File.WriteAllText(PathSettings.Default.HelpPath + _fileName, value);
                    OnPropertyChanged(nameof(RtfDocument));
               }
          }

          public string HelpImage
          {
               get
               {
                    return _helpImage;
               }
               set
               {
                    _helpImage = value;
                    OnPropertyChanged(nameof(HelpImage));
               }
          }

          #endregion Properties
     }
}