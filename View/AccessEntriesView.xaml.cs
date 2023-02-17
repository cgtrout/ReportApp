using System.Speech.Synthesis;
using System.Windows.Controls;
using System.Windows.Input;

namespace ReportApp.View
{
     /// <summary>
     /// Interaction logic for AccessEntryView.xaml
     /// </summary>
     public partial class AccessEntriesView : UserControl
     {
          #region Fields

          private SpeechSynthesizer synth = new SpeechSynthesizer();

          #endregion Fields

          #region Constructors

          public AccessEntriesView()
          {
               InitializeComponent();
          }

          #endregion Constructors

          #region Methods

          private void UserControl_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
          {
               if ((Keyboard.Modifiers & ModifierKeys.Control) > 0) {
                    if (e.Delta > 0)
                         SliderZoom.Value += 0.05;
                    else
                         SliderZoom.Value -= 0.05;
               }
          }

          private void UserControl_PreviewKeyUp(object sender, KeyEventArgs e)
          {
               //if (e.Key == Key.F12) {
               //     if (synth.State != SynthesizerState.Speaking) {
               //          SystemSounds.Exclamation.Play();
               //          synth.SpeakAsync("Passback! Passback!");
               //     }
               //}
               //else if (e.Key == Key.F11) {
               //     if (synth.State != SynthesizerState.Speaking) {
               //          SystemSounds.Exclamation.Play();
               //          synth.SpeakAsync("Credential! Credential!");
               //     }
               //}
               //else if (e.Key == Key.F9) {
               //     if (synth.State != SynthesizerState.Speaking) {
               //          SystemSounds.Exclamation.Play();
               //          synth.SpeakAsync("Expired! Expired!");
               //     }
               //}
          }

          #endregion Methods
     }
}