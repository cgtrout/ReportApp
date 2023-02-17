using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ReportApp.Utility
{
     public static class ScreenShotHelper
     {
          #region Methods

          /// <summary>
          /// Saves screenshot from main window
          /// </summary>
          public static void SaveScreenShot(string description)
          {
               try {
                    TraceEx.PrintLog($"SaveScreenShot called {description}");

                    Visual visual = Application.Current.MainWindow;
                    string filePath = @"C:\CTApp\ScreenShots\";
                    string fileName = $"{DateTime.Now.Year}-{DateTime.Now.Month}-{DateTime.Now.Day} {DateTime.Now.Hour}-{DateTime.Now.Minute}-{DateTime.Now.Second} {description}";
                    Directory.CreateDirectory(filePath);

                    var rect = VisualTreeHelper.GetDescendantBounds(visual);
                    var width = Convert.ToInt32(rect.Width);
                    var height = Convert.ToInt32(rect.Height);

                    var renderTargetBitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
                    renderTargetBitmap.Render(visual);
                    var pngImage = new PngBitmapEncoder();
                    pngImage.Frames.Add(BitmapFrame.Create(renderTargetBitmap));

                    using (var fileStream = File.Create(filePath + fileName + ".png")) {
                         pngImage.Save(fileStream);
                    }
               } catch(Exception e) {
                    Trace.TraceError($"Screenshot exception: {e.GetType()} {e.Message} ");
               }
          }

          #endregion Methods
     }
}