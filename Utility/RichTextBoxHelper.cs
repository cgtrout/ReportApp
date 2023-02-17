using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace ReportApp.Utility
{
     internal class RichTextBoxHelper : DependencyObject
     {
          #region Fields

          public static readonly DependencyProperty DocumentXamlProperty =
               DependencyProperty.RegisterAttached(
                    "DocumentRtf",
                    typeof(string),
                    typeof(RichTextBoxHelper),
                    new FrameworkPropertyMetadata {
                         BindsTwoWayByDefault = true,
                         PropertyChangedCallback = (obj, e) => {
                              var richTextBox = (RichTextBox)obj;

                              var rtf = GetDocumentRtf(richTextBox);
                              var doc = new FlowDocument();
                              var range = new TextRange(doc.ContentStart, doc.ContentEnd);

                              range.Load(new MemoryStream(Encoding.UTF8.GetBytes(rtf)), DataFormats.Rtf);

                              richTextBox.Document = doc;

                              //range.Changed += (obj2, e2) => {
                              //     if (richTextBox.Document == doc) {
                              //          MemoryStream buffer = new MemoryStream();
                              //          range.Save(buffer, DataFormats.Rtf);
                              //          SetDocumentRtf(richTextBox, Encoding.UTF8.GetString(buffer.ToArray()));
                              //     }
                              //};
                         }
                    });

          #endregion Fields

          #region Methods

          public static string GetDocumentRtf(DependencyObject obj)
          {
               return (string)obj.GetValue(DocumentXamlProperty);
          }

          public static void SetDocumentRtf(DependencyObject obj, string value)
          {
               obj.SetValue(DocumentXamlProperty, value);
          }

          #endregion Methods
     }
}