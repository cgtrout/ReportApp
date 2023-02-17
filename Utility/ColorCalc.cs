using System.Windows.Media;

namespace ReportApp.Utility
{
     public class ColorCalc
     {
          #region Methods

          /// <summary>
          /// get rbg values interpolated between color1 and 2 based on fraction
          /// </summary>
          public static void CalculateColors(Color color1, Color color2, float alpha1, float alpha2, double fraction, out byte r, out byte b, out byte g, out byte a)
          {
               r = CalcColorResult(fraction, color1.R, color2.R);
               b = CalcColorResult(fraction, color1.B, color2.B);
               g = CalcColorResult(fraction, color1.G, color2.G);
               a = (byte)((((alpha2 - alpha1) * fraction) + alpha1) * 255);
          }

          public static double CalculateFraction(double value, double startPoint, double endPoint)
          {
               var fraction = (value - startPoint) / (endPoint - startPoint);
               fraction = fraction > 1.0 ? 1.0 : fraction;
               fraction = fraction < 0.0 ? 0.0 : fraction;
               return fraction;
          }

          private static byte CalcColorResult(double fraction, byte color1, byte color2)
          {
               double calc = (color2 - color1) * fraction + color1;
               return (byte)calc;
          }

          #endregion Methods
     }
}