using System.Windows;

namespace ReportApp.Utility
{
     /// <summary>
     /// Description of BindingProxy.
     /// </summary>
     public class BindingProxy : Freezable
     {
          #region Fields

          public static readonly DependencyProperty DataProperty = DependencyProperty.Register("Data", typeof(object), typeof(BindingProxy), new UIPropertyMetadata(null));

          #endregion Fields

          #region Properties

          public object Data
          {
               get { return (object)GetValue(DataProperty); }
               set { SetValue(DataProperty, value); }
          }

          #endregion Properties

          #region Methods

          protected override Freezable CreateInstanceCore()
          {
               return new BindingProxy();
          }

          #endregion Methods
     }
}