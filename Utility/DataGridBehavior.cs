using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace ReportApp.Utility
{
     public class DataGridBehavior
     {
          #region Fields

          public static readonly DependencyProperty ScrollOnNewItemProperty =
              DependencyProperty.RegisterAttached(
                  "ScrollOnNewItem",
                  typeof(bool),
                  typeof(DataGridBehavior),
                  new UIPropertyMetadata(false, OnScrollOnNewItemChanged));

          private static readonly Dictionary<DataGrid, Capture> Associations =
                           new Dictionary<DataGrid, Capture>();

          #endregion Fields

          #region Methods

          public static bool GetScrollOnNewItem(DependencyObject obj)
          {
               return (bool)obj.GetValue(ScrollOnNewItemProperty);
          }

          public static void SetScrollOnNewItem(DependencyObject obj, bool value)
          {
               obj.SetValue(ScrollOnNewItemProperty, value);
          }

          public static void OnScrollOnNewItemChanged(
              DependencyObject d,
              DependencyPropertyChangedEventArgs e)
          {
               var dataGrid = d as DataGrid;
               if (dataGrid == null) return;
               bool oldValue = (bool)e.OldValue, newValue = (bool)e.NewValue;
               if (newValue == oldValue) return;
               if (newValue) {
                    dataGrid.Loaded += DataGrid_Loaded;
                    dataGrid.Unloaded += DataGrid_Unloaded;
                    var itemsSourcePropertyDescriptor = TypeDescriptor.GetProperties(dataGrid)["ItemsSource"];
                    itemsSourcePropertyDescriptor.AddValueChanged(dataGrid, DataGrid_ItemsSourceChanged);
               } else {
                    dataGrid.Loaded -= DataGrid_Loaded;
                    dataGrid.Unloaded -= DataGrid_Unloaded;
                    if (Associations.ContainsKey(dataGrid))
                         Associations[dataGrid].Dispose();
                    var itemsSourcePropertyDescriptor = TypeDescriptor.GetProperties(dataGrid)["ItemsSource"];
                    itemsSourcePropertyDescriptor.RemoveValueChanged(dataGrid, DataGrid_ItemsSourceChanged);
               }
          }

          private static void DataGrid_ItemsSourceChanged(object sender, EventArgs e)
          {
               var dataGrid = (DataGrid)sender;
               if (Associations.ContainsKey(dataGrid))
                    Associations[dataGrid].Dispose();
               Associations[dataGrid] = new Capture(dataGrid);
          }

          private static void DataGrid_Unloaded(object sender, RoutedEventArgs e)
          {
               var dataGrid = (DataGrid)sender;
               if (Associations.ContainsKey(dataGrid))
                    Associations[dataGrid].Dispose();
               dataGrid.Unloaded -= DataGrid_Unloaded;
          }

          private static void DataGrid_Loaded(object sender, RoutedEventArgs e)
          {
               var dataGrid = (DataGrid)sender;
               var incc = dataGrid.Items as INotifyCollectionChanged;
               if (incc == null) return;
               dataGrid.Loaded -= DataGrid_Loaded;
               Associations[dataGrid] = new Capture(dataGrid);
          }

          #endregion Methods

          #region Classes

          private class Capture : IDisposable
          {
               #region Fields

               private readonly DataGrid dataGrid;
               private readonly INotifyCollectionChanged incc;

               #endregion Fields

               #region Constructors

               public Capture(DataGrid dataGrid)
               {
                    this.dataGrid = dataGrid;
                    incc = dataGrid.ItemsSource as INotifyCollectionChanged;
                    if (incc != null) {
                         incc.CollectionChanged += incc_CollectionChanged;
                    }
               }

               #endregion Constructors

               #region Methods

               public void Dispose()
               {
                    if (incc != null)
                         incc.CollectionChanged -= incc_CollectionChanged;
               }

               private void incc_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
               {
                    if (e.Action == NotifyCollectionChangedAction.Add) {
                         dataGrid.ScrollIntoView(e.NewItems[0]);
                         dataGrid.SelectedItem = e.NewItems[0];
                    }
               }

               #endregion Methods
          }

          #endregion Classes
     }
}