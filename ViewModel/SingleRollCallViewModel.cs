using ReportApp.Data;
using ReportApp.Model;
using ReportApp.Utility;
using System;
using System.Windows.Media;

namespace ReportApp.ViewModel
{
     //represents a single roll call view model
     public class SingleRollCallViewModel : WorkspaceViewModel
     {
          #region Fields

          private RollCall rollcall = new RollCall();

          #endregion Fields

          #region Constructors

          public SingleRollCallViewModel(RollCall rollcall)
          {
               this.rollcall = rollcall;
          }

          #endregion Constructors

          #region Properties

          public string Company
          {
               get
               {
                    if (string.IsNullOrEmpty(LinkedPerson.Company)) {
                         return "OldComp Power";
                    } else {
                         return LinkedPerson.Company;
                    }
               }
          }

          public DateTime DtTm
          {
               get { return rollcall.DtTm; }
               set
               {
                    rollcall.DtTm = value;
                    OnPropertyChanged(nameof(DtTm));
               }
          }

          public PersonViewModel LinkedPerson
          {
               get
               {
                    var p = DataRepository.PersonDict[PersonId];
                    return p;
               }
          }

          public string PersonId
          {
               get { return rollcall.PersonId; }
               set
               {
                    rollcall.PersonId = value;
                    OnPropertyChanged(nameof(PersonId));
               }
          }

          public string Reader
          {
               get
               {
                    if (rollcall.Reader.Contains("Admin")) {
                         return "Admin";
                    } else if (rollcall.Reader.Contains("CP")) {
                         return "CP";
                    } else if (rollcall.Reader.Contains("Cont")) {
                         return "Control B.";
                    }
                    return "UNKOWN_READER";
               }
               set
               {
                    rollcall.Reader = value;
                    OnPropertyChanged(nameof(Reader));
               }
          }

          public Brush TimeColorBackground
          {
               get
               {
                    byte r, b, g, a;
                    CalculateColor(out r, out b, out g, out a);
                    var startColor = new Color() { R = r, G = g, B = b, A = a };
                    var endColor = new Color() { R = r, G = g, B = b, A = (byte)(a / 1.5) };
                    //return new LinearGradientBrush(startColor, endColor, 90);
                    var brush = new RadialGradientBrush(endColor, startColor);
                    brush.RadiusX *= 1.75;
                    brush.RadiusY *= 1.75;
                    return brush;
               }
          }

          public Brush TimeColorText
          {
               get
               {
                    byte r, b, g, a;
                    CalculateColor(out r, out b, out g, out a);
                    byte darknessOffset = 175;
                    r = r - darknessOffset < 0 ? (byte)0 : (byte)(r - darknessOffset);
                    g = g - darknessOffset < 0 ? (byte)0 : (byte)(g - darknessOffset);
                    b = b - darknessOffset < 0 ? (byte)0 : (byte)(b - darknessOffset);

                    return new SolidColorBrush(new Color() { R = r, B = b, G = g, A = 255 });
               }
          }

          public Brush TimeColorBorder
          {
               get
               {
                    byte r, b, g, a;
                    CalculateColor(out r, out b, out g, out a);
                    byte darknessOffset = 60;
                    r = r - darknessOffset < 0 ? (byte)0 : (byte)(r - darknessOffset);
                    g = g - darknessOffset < 0 ? (byte)0 : (byte)(g - darknessOffset);
                    b = b - darknessOffset < 0 ? (byte)0 : (byte)(b - darknessOffset);

                    return new SolidColorBrush(new Color() { R = r, B = b, G = g, A = a });
               }
          }

          public string TimeIn => $"{TimeInFloat.ToString("0.0")}";

          public double TimeInFloat
          {
               get
               {
                    double timein = CalculateHours();
                    if (timein < 0) timein = 0;
                    return timein;
               }
          }

          #endregion Properties

          #region Methods

          public void CalculateColor(out byte r, out byte b, out byte g, out byte a)
          {
               //set color depending on what time it is between startpoint and endpoint
               Color color1 = Colors.Yellow;
               Color color2 = Colors.Red;
               float maxAlpha = 0.95f;
               float alpha1 = maxAlpha;
               float alpha2 = maxAlpha;

               var hours = CalculateHours();

               double startPoint = 8;
               double endPoint = 13;

               var preStartPointRegionSize = 1.5f;

               //use diff colors for 0-7.5 hours
               if (hours < startPoint - preStartPointRegionSize) {
                    startPoint = 0;
                    endPoint = 8 - preStartPointRegionSize;
                    alpha1 = 0.00f;
                    alpha2 = 0.75f;
                    color1 = Colors.LightBlue;
                    color2 = Colors.LightGreen;
               } else if (hours > startPoint - preStartPointRegionSize && hours < startPoint) {
                    alpha1 = 0.75f;
                    alpha2 = maxAlpha;
                    startPoint = 8 - preStartPointRegionSize;
                    endPoint = 8;
                    color1 = Colors.LightGreen;
                    color2 = Colors.Yellow;
               }

               double fraction = ColorCalc.CalculateFraction(hours, startPoint, endPoint);

               ColorCalc.CalculateColors(color1, color2, alpha1, alpha2, fraction, out r, out b, out g, out a);
          }

          public override string ToString()
          {
               return $"{PersonId} {this.Reader} {this.DtTm}";
          }

          public void UpdateTime()
          {
               OnPropertyChanged(nameof(TimeIn));
               OnPropertyChanged(nameof(TimeColorText));
               OnPropertyChanged(nameof(TimeColorBackground));
               OnPropertyChanged(nameof(TimeColorBorder));
          }

          private double CalculateHours()
          {
               return (DateTime.Now.ToUniversalTime() - DtTm.ToUniversalTime()).TotalHours;
          }

          #endregion Methods
     }
}