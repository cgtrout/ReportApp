using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ReportApp.Utility
{
     internal class EditableTextBox : TextBox
     {
          #region Constructors

          public EditableTextBox()
          {
               this.IsReadOnly = true;
               this.IsReadOnlyCaretVisible = true;
          }

          #endregion Constructors

          #region Methods

          protected override void OnGotFocus(RoutedEventArgs e)
          {
               base.OnGotFocus(e);
          }

          protected override void OnGotKeyboardFocus(KeyboardFocusChangedEventArgs e)
          {
               base.OnGotKeyboardFocus(e);
               this.IsReadOnly = false;
               DispatcherHelper.GetDispatcher().BeginInvoke(new Action(() => Keyboard.Focus(this)));
          }

          protected override void OnLostKeyboardFocus(KeyboardFocusChangedEventArgs e)
          {
               base.OnLostKeyboardFocus(e);
               this.IsReadOnly = true;
          }

          protected override void OnMouseDoubleClick(MouseButtonEventArgs e)
          {
               //switch to edit mode
               this.IsReadOnly = false;

               DispatcherHelper.GetDispatcher().BeginInvoke(new Action(() => Keyboard.Focus(this)));
          }

          protected override void OnMouseDown(MouseButtonEventArgs e)
          {
          }

          protected override void OnMouseEnter(MouseEventArgs e)
          {
               base.OnMouseEnter(e);
          }

          protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
          {
          }

          protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
          {
          }

          //never called either
          protected override void OnPreviewDragEnter(DragEventArgs e)
          {
               base.OnPreviewDragEnter(e);
          }

          //not called in error cond
          //never called apparently??
          protected override void OnPreviewDragOver(DragEventArgs e)
          {
               base.OnPreviewDragOver(e);
          }

          protected override void OnPreviewMouseMove(MouseEventArgs e)
          {
               base.OnPreviewMouseMove(e);
          }

          protected override void OnPreviewKeyUp(KeyEventArgs e)
          {
               base.OnPreviewKeyUp(e);

               if (e.Key == Key.Enter) {
                    TraversalRequest r = new TraversalRequest(FocusNavigationDirection.Next);
                    MoveFocus(r);
               }
          }

          #endregion Methods
     }
}