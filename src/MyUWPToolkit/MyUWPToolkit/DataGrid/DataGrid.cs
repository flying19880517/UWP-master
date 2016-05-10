﻿using MyUWPToolkit.DataGrid.Model.Cell;
using MyUWPToolkit.DataGrid.Util;
using MyUWPToolkit.CollectionView;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using System.Reflection;
using MyUWPToolkit.DataGrid.Model.RowCol;
using MyUWPToolkit.Util;
using Windows.Foundation;
using System.Globalization;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;
using Windows.Foundation.Collections;
using Windows.Devices.Input;
using System.Diagnostics;
using Windows.UI;

namespace MyUWPToolkit.DataGrid
{
    /// <summary>
    /// 
    /// </summary>
    [TemplatePart(Name = "ContentGrid", Type = typeof(Grid))]
    [TemplatePart(Name = "VerticalScrollBar", Type = typeof(ScrollBar))]
    [TemplatePart(Name = "HorizontalScrollBar", Type = typeof(ScrollBar))]
    [TemplatePart(Name = "PullToRefreshHeader", Type = typeof(ContentControl))]
    [TemplatePart(Name = "CrossSlideLeftGrid", Type = typeof(Grid))]
    [TemplatePart(Name = "CrossSlideRightGrid", Type = typeof(Grid))]
    public partial class DataGrid : Control
    {

        #region Ctor
        public DataGrid()
        {
            this.DefaultStyleKey = typeof(DataGrid);
            this.InitializePanel();
        }

        #endregion

        #region override method
        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            this.InitializeContentGrid();
            this.InitializeScrollBar();
            UpdateScrollBars();
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            if (RefreshThreshold == 0.0)
            {
                RefreshThreshold = availableSize.Height * 1 / 5;
            }
            return base.MeasureOverride(availableSize);
        }
        #endregion

        #region Private Methods

        #region ScrollBar

        private void InitializeScrollBar()
        {
            _verticalScrollBar = GetTemplateChild("VerticalScrollBar") as ScrollBar;
            _horizontalScrollBar = GetTemplateChild("HorizontalScrollBar") as ScrollBar;
            _verticalScrollBar.Scroll += ScrollBar_Scroll;
            _horizontalScrollBar.Scroll += ScrollBar_Scroll;

        }

        private void ScrollBar_Scroll(object sender, ScrollEventArgs e)
        {
            // get new scroll position from bars
            var sp = new Point(-_horizontalScrollBar.Value, -_verticalScrollBar.Value);

            // scroll now
            ScrollPosition = sp;
        }

        void UpdateScrollBars()
        {
            if (_contentGrid != null && _horizontalScrollBar != null && _verticalScrollBar != null)
            {
                // calculate size available (client and scrollbars)
                var wid = _contentGrid.ActualWidth;
                var hei = _contentGrid.ActualHeight;


                if (!double.IsPositiveInfinity(wid) && !double.IsPositiveInfinity(hei))
                {
                    //if (_sbV.Visibility == Visibility.Visible && _sbV.IsEnabled == false)
                    //{
                    //    if (_sbV.ActualWidth != double.NaN)
                    //    {
                    //        wid -= _sbV.ActualWidth;
                    //    }
                    //}
                    //if (_sbH.Visibility == Visibility.Visible && _sbH.IsEnabled == false)
                    //{
                    //    if (_sbH.ActualHeight != double.NaN)
                    //    {
                    //        hei -= _sbH.ActualHeight;
                    //    }
                    //}

                    // make sure star sizes are current (TFS 30814)
                    UpdateStarSizes();

                    // check which scrollbars we need
                    //bool needV = Rows.GetTotalSize() > hei;
                    //if (needV)
                    //{
                    //    wid -= _root.ColumnDefinitions[2].ActualWidth;
                    //}
                    //bool needH = Columns.GetTotalSize() > wid;
                    //if (needH)
                    //{
                    //    hei -= _root.RowDefinitions[2].ActualHeight;
                    //    needV = Rows.GetTotalSize() > hei;
                    //}

                    // update scrollbar parameters
                    _verticalScrollBar.SmallChange = _horizontalScrollBar.SmallChange = Rows.DefaultSize;
                    _verticalScrollBar.LargeChange = _horizontalScrollBar.ViewportSize = hei;
                    _verticalScrollBar.LargeChange = _horizontalScrollBar.ViewportSize = wid;
                    _verticalScrollBar.Maximum = Rows.GetTotalSize() - hei;
                    _horizontalScrollBar.Maximum = Columns.GetTotalSize() - wid;


                    //ViewportHeight = hei;
                    //ViewportWidth = wid;
                    //ScrollableHeight = Rows.GetTotalSize() - hei;
                    //ScrollableWidth = Columns.GetTotalSize() - wid;
                    //var rowSize = Rows.GetTotalSize(); 
                    //_sbV.Maximum = rowSize - hei + 1; 
                    //_sbH.Maximum = Columns.GetTotalSize() - wid + 1;

                    // update scrollbar visibility
                    if (_verticalScrollBar != null)
                    {
                        _verticalScrollBar.Visibility = _verticalScrollBar.Maximum > 0 ? Visibility.Visible : Visibility.Collapsed;

                    }
                    //UpdateScrollbarVisibility(_sbH, HorizontalScrollBarVisibility, needH);

                    // make sure current scroll position is valid
                    ScrollPosition = ScrollPosition;
                    if (_view != null && _verticalScrollBar != null)
                    {
                        if (-ScrollPosition.Y >= _verticalScrollBar.Maximum && _verticalScrollBar.Maximum >= 0)
                        {
                            if (_view.HasMoreItems && !_isLoadingMoreItems)
                            {
                                _isLoadingMoreItems = true;
                                var firstRow = Math.Max(0, Math.Min(Rows.Count - 1, Rows.GetItemAt(_verticalScrollBar.Value)));
                                var lastRow = Math.Max(-1, Math.Min(Rows.Count - 1, Rows.GetItemAt(_verticalScrollBar.Value + _cellPanel.ActualHeight)));
                                uint count = Math.Max(1, (uint)(lastRow - firstRow));
                                //uint count = Math.Max(1, (uint)(10));
                                if (count == uint.MaxValue)
                                {
                                    count = (uint)((this.ActualHeight - this._columnHeaderPanel.ActualHeight) / Rows.DefaultSize + 0.5);
                                }
                                _view.LoadMoreItemsAsync(count).AsTask().ContinueWith(t =>
                                {
                                    _isLoadingMoreItems = false;
                                }, System.Threading.Tasks.TaskScheduler.FromCurrentSynchronizationContext());

                            }
                        }
                    }
                }
            }

        }
        #endregion

        #region ContentGrid
        private void InitializeContentGrid()
        {
            _contentGrid = GetTemplateChild("ContentGrid") as Grid;
            _contentGrid.ManipulationMode = ManipulationModes.TranslateX | ManipulationModes.TranslateY | ManipulationModes.TranslateInertia;
            _contentGrid.ManipulationDelta += _contentGrid_ManipulationDelta;
            _contentGrid.ManipulationCompleted += _contentGrid_ManipulationCompleted;
            _contentGrid.ManipulationStarting += _contentGrid_ManipulationStarting;

            _pullToRefreshHeader = GetTemplateChild("PullToRefreshHeader") as ContentControl;
            _pullToRefreshHeader.DataContext = this;

            _crossSlideLeftGrid = GetTemplateChild("CrossSlideLeftGrid") as Grid;
            _crossSlideRightGrid = GetTemplateChild("CrossSlideRightGrid") as Grid;

            _cellPanel.SetValue(Grid.RowProperty, 2);
            _columnHeaderPanel.SetValue(Grid.RowProperty, 0);
            _cellPanel.LayoutUpdated += _cellPanel_LayoutUpdated;
            _columnHeaderPanel.Tapped += _columnHeaderPanel_Tapped;
            _cellPanel.Tapped += _cellPanel_Tapped;

            _canvas.SetValue(Grid.RowSpanProperty, 3);
            _contentGrid.Children.Add(_cellPanel);
            _contentGrid.Children.Add(_columnHeaderPanel);
            _contentGrid.Children.Add(_canvas);

            int sz = (int)(FontSize * 1.6 + 4);
            Rows.DefaultSize = sz;
            ColumnHeaders.Rows.DefaultSize = sz;
        }


        private void _contentGrid_ManipulationStarting(object sender, ManipulationStartingRoutedEventArgs e)
        {
            startingPullToRefresh = false;
            startingCrossSlide = false;
            if (_verticalScrollBar.Value == 0)
            {
                startingPullToRefresh = true;
            }

            //Cross Slide left
            if (_horizontalScrollBar.Value == 0 || _horizontalScrollBar.Value == _horizontalScrollBar.Maximum)
            {
                startingCrossSlide = true;
            }
        }

        private void _contentGrid_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            VisualStateManager.GoToState(this, "NoIndicator", true);
            _pullToRefreshHeader.Height = 0;
            _crossSlideLeftGrid.Width = 0;
            _crossSlideRightGrid.Width = 0;
            _contentGrid.ManipulationMode = ManipulationModes.TranslateX | ManipulationModes.TranslateY | ManipulationModes.TranslateInertia;
            _contentGrid.ManipulationDelta += _contentGrid_ManipulationDelta;
            if (PivotItem != null)
            {
                PivotItem.Margin = _defaultPivotItemMargin;
            }
            if (IsReachThreshold && startingPullToRefresh)
            {
                if (PullToRefresh != null)
                {
                    LastRefreshTime = DateTime.Now;
                    PullToRefresh(this, null);
                }
            }
        }

        private void _contentGrid_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {

            ////Cross Slide left
            if (((_crossSlideLeftGrid.Width == 0 && e.Delta.Translation.X > 0 && e.Delta.Translation.Y == 0) || _crossSlideLeftGrid.Width > 0) && _horizontalScrollBar.Value == 0 && startingCrossSlide)
            {
                var maxThreshold = (int)(Window.Current.Bounds.Width * 5 / 6.0);
                if (_crossSlideLeftGrid.Width <= maxThreshold)
                {
                    _contentGrid.ManipulationMode = ManipulationModes.TranslateX;

                    if (PivotItem != null)
                    {
                        if (PivotItem.Margin.Left >= maxThreshold)
                        {
                            HandleCrossSlide(CrossSlideMode.Left);
                        }
                        else
                        {
                            var width = PivotItem.Margin.Left + e.Delta.Translation.X;
                            if (width > maxThreshold)
                            {
                                width = maxThreshold;
                                _contentGrid.ManipulationDelta -= _contentGrid_ManipulationDelta;
                            }

                            PivotItem.Margin = new Thickness(width, PivotItem.Margin.Top, PivotItem.Margin.Right, PivotItem.Margin.Bottom);

                            if (PivotItem.Margin.Left >= maxThreshold)
                            {
                                HandleCrossSlide(CrossSlideMode.Left);
                            }
                        }
                    }
                    else
                    {
                        var width = _crossSlideLeftGrid.Width + e.Delta.Translation.X;
                        if (width > maxThreshold)
                        {
                            width = maxThreshold;
                            if (CrossSlide != null)
                            {
                                _crossSlideLeftGrid.Width = 0;
                                if (this.CrossSlide != null)
                                {
                                    CrossSlide(this, new CrossSlideEventArgs(CrossSlideMode.Left));
                                }
                                _contentGrid.ManipulationDelta -= _contentGrid_ManipulationDelta;
                                return;
                            }
                        }

                        _crossSlideLeftGrid.Width = width >= 0 ? width : 0;
                    }
                }
                VisualStateManager.GoToState(this, "NoIndicator", true);
            }
            ////Cross Slide right
            if (((_crossSlideRightGrid.Width == 0 && e.Delta.Translation.X < 0 && e.Delta.Translation.Y == 0) || _crossSlideRightGrid.Width > 0) && _horizontalScrollBar.Value == _horizontalScrollBar.Maximum && startingCrossSlide)
            {
                var maxThreshold = (int)(Window.Current.Bounds.Width * 5 / 6.0);
                if (_crossSlideRightGrid.Width <= maxThreshold)
                {
                    _contentGrid.ManipulationMode = ManipulationModes.TranslateX;

                    if (PivotItem != null)
                    {
                        if (PivotItem.Margin.Right >= maxThreshold)
                        {
                            HandleCrossSlide(CrossSlideMode.Right);
                        }
                        else
                        {
                            var width = PivotItem.Margin.Right - e.Delta.Translation.X;
                            if (width > maxThreshold)
                            {
                                width = maxThreshold;
                                _contentGrid.ManipulationDelta -= _contentGrid_ManipulationDelta;
                            }

                            PivotItem.Margin = new Thickness(PivotItem.Margin.Left, PivotItem.Margin.Top, width, PivotItem.Margin.Bottom);

                            if (PivotItem.Margin.Right >= maxThreshold)
                            {
                                HandleCrossSlide(CrossSlideMode.Right);
                            }
                        }
                    }
                    else
                    {
                        var width = _crossSlideRightGrid.Width - e.Delta.Translation.X;
                        if (width > maxThreshold)
                        {
                            width = maxThreshold;
                            if (CrossSlide != null)
                            {
                                _crossSlideRightGrid.Width = 0;
                                if (this.CrossSlide != null)
                                {
                                    CrossSlide(this, new CrossSlideEventArgs(CrossSlideMode.Right));
                                }
                                _contentGrid.ManipulationDelta -= _contentGrid_ManipulationDelta;
                                return;
                            }
                        }
                        _crossSlideRightGrid.Width = width >= 0 ? width : 0;
                    }
                   
                }

                VisualStateManager.GoToState(this, "NoIndicator", true);
            }


            //support pull to refresh
            //1.starting pull to refresh, it should height is 0, delta y is more than 0 and delta x is 0
            //2.pulling to refresh, height should be more than 0
            //verticaloffset should be 0 and startingPullToRefresh should be true
            if (((_pullToRefreshHeader.Height == 0 && e.Delta.Translation.Y > 0 && e.Delta.Translation.X == 0) || _pullToRefreshHeader.Height > 0) && _verticalScrollBar.Value == 0 && startingPullToRefresh)
            {
                var maxThreshold = RefreshThreshold * 4 / 3.0;
                if (_pullToRefreshHeader.Height <= maxThreshold)
                {
                    _contentGrid.ManipulationMode = ManipulationModes.TranslateY;
                    var height = _pullToRefreshHeader.Height + e.Delta.Translation.Y;
                    if (height > maxThreshold)
                    {
                        height = maxThreshold;
                        _contentGrid.ManipulationDelta -= _contentGrid_ManipulationDelta;
                    }
                    _pullToRefreshHeader.Height = height >= 0 ? height : 0;
                }
                if (_pullToRefreshHeader.Height >= RefreshThreshold)
                {
                    this.IsReachThreshold = true;
                }
                else
                {
                    this.IsReachThreshold = false;
                }
                VisualStateManager.GoToState(this, "NoIndicator", true);
            }
            else if (_pullToRefreshHeader.Height == 0)
            {
                var point = new Point() { X = ScrollPosition.X + e.Delta.Translation.X, Y = ScrollPosition.Y + e.Delta.Translation.Y };

                ScrollPosition = point;

                if (_view != null && _verticalScrollBar != null)
                {
                    if (-point.Y >= _verticalScrollBar.Maximum && _verticalScrollBar.Maximum > 0)
                    {
                        if (_view.HasMoreItems && !_isLoadingMoreItems)
                        {
                            _isLoadingMoreItems = true;

                            var firstRow = Math.Max(0, Math.Min(Rows.Count - 1, Rows.GetItemAt(_verticalScrollBar.Value)));
                            var lastRow = Math.Max(-1, Math.Min(Rows.Count - 1, Rows.GetItemAt(_verticalScrollBar.Value + _cellPanel.ActualHeight)));
                            uint count = Math.Max(1, (uint)(lastRow - firstRow));

                            _view.LoadMoreItemsAsync(count).AsTask().ContinueWith(t =>
                            {
                                _isLoadingMoreItems = false;
                            }, System.Threading.Tasks.TaskScheduler.FromCurrentSynchronizationContext());

                        }
                    }
                }

                this.IsReachThreshold = false;
                if (e.PointerDeviceType == PointerDeviceType.Touch)
                {
                    VisualStateManager.GoToState(this, "TouchIndicator", true);
                }
                else if (e.PointerDeviceType == PointerDeviceType.Mouse)
                {
                    VisualStateManager.GoToState(this, "MouseIndicator", true);
                }
            }

        }

        private void HandleCrossSlide(CrossSlideMode mode)
        {

            _contentGrid.ManipulationDelta -= _contentGrid_ManipulationDelta;

            if (PivotItem != null)
            {
                PivotItem.Margin = _defaultPivotItemMargin;
                var pivot = PivotItem.Parent as Pivot;
                if (pivot != null)
                {
                    var index = pivot.SelectedIndex;
                    if (mode == CrossSlideMode.Left)
                    {
                        index = index - 1;
                        if (index < 0)
                        {
                            index = pivot.Items.Count - 1;
                        }
                    }
                    else
                    {
                        index = index + 1;
                        if (index > pivot.Items.Count - 1)
                        {
                            index = 0;
                        }
                    }

                    pivot.SelectedIndex = index;
                }
            }

        }

        #endregion

        #region GridPanel
        private void InitializePanel()
        {
            _cellPanel = new DataGridPanel(this, CellType.Cell, Consts.ROWHEIGHT, Consts.COLUMNWIDTH);

            _columnHeaderPanel = new DataGridPanel(this, CellType.ColumnHeader, Consts.ROWHEIGHT, Consts.COLUMNWIDTH);
            _columnHeaderPanel.Columns = _cellPanel.Columns;

            _columnHeaderPanel.Rows.Add(new Row());
            #region frozen
            _canvas = new Canvas();
            _lnFX = new Line();
            _lnFX.Visibility = Visibility.Collapsed;
            _lnFX.StrokeThickness = 1;
            _canvas.Children.Add(_lnFX);

            _lnFY = new Line();
            _lnFY.Visibility = Visibility.Collapsed;
            _lnFY.StrokeThickness = 1;
            _canvas.Children.Add(_lnFY);
            #endregion

            #region More Columns

            #endregion

        }

        private void _cellPanel_LayoutUpdated(object sender, object e)
        {
            UpdateScrollBars();

            // clip canvas
            var g = new RectangleGeometry();
            _canvas.Clip = g;
            g.Rect = new Rect(0, 0, _canvas.ActualWidth, _canvas.ActualHeight);

            // update frozen row, column indicators
            var hdrX = 0;
            var hdrY = ColumnHeaders.Visibility == Visibility.Visible ? ColumnHeaders.ActualHeight : 0;
            if (Columns.Frozen > 0)
            {
                var fx = Columns.GetFrozenSize();
                _lnFX.X1 = _lnFX.X2 = fx;
                _lnFX.Y2 = Math.Min(10000, Cells.ActualHeight + hdrY);
                _lnFX.Stroke = FrozenLinesBrush;
                _lnFX.Visibility = Visibility.Visible;
            }
            else
            {
                _lnFX.Visibility = Visibility.Collapsed;
            }
            if (Rows.Frozen > 0)
            {
                var fy = Rows.GetFrozenSize() + hdrY;
                _lnFY.Y1 = _lnFY.Y2 = fy;
                _lnFY.X2 = Math.Min(10000, Cells.ActualWidth + hdrX);
                _lnFY.Stroke = FrozenLinesBrush;
                _lnFY.Visibility = Visibility.Visible;
            }
            else
            {
                _lnFY.Visibility = Visibility.Collapsed;
            }
        }
        private void _cellPanel_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var pt = e.GetPosition(_cellPanel);
            pt = this.TransformToVisual(_cellPanel).TransformPoint(pt);
            var fy = _cellPanel.Rows.GetFrozenSize();
            pt.Y += _columnHeaderPanel.ActualHeight;
            var sp = _cellPanel.ScrollPosition;
            if (pt.Y < 0 || pt.Y > fy) pt.Y -= sp.Y;
            // get row and column at given coordinates
            var row = _cellPanel.Rows.GetItemAt(pt.Y);
            if (ItemClick != null && row > -1)
            {
                var args = new ItemClickEventArgs(this.Rows[row]);
                ItemClick(this, args);
            }
        }

        private void _columnHeaderPanel_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var pt = e.GetPosition(_columnHeaderPanel);
            pt = this.TransformToVisual(_columnHeaderPanel).TransformPoint(pt);

            var fx = _columnHeaderPanel.Columns.GetFrozenSize();
            // adjust for scroll position
            var sp = _columnHeaderPanel.ScrollPosition;
            if (pt.X < 0 || pt.X > fx) pt.X -= sp.X;
            var column = _columnHeaderPanel.Columns.GetItemAt(pt.X);
            HandleSort(column);
        }

        void HandleSort(int column)
        {
            if (column > -1 && this.AllowSorting && this.Columns[column].AllowSorting)
            {

                // get view
                var view = _view as ICollectionViewEx;

                // get column to sort
                var col = this.Columns[column];
                var direction = ListSortDirection.Ascending;
                var sds = view.SortDescriptions;

                // get property to sort on
                var pn = col.BoundPropertyName;
                if (!string.IsNullOrEmpty(pn))
                {

                    var args = new SortingColumnEventArgs(this.Columns[column]);
                    if (this.SortingColumn != null)
                    {
                        SortingColumn(this, args);
                    }

                    if (!args.Cancel)
                    {
                        // apply new sort
                        try
                        {
                            // if already sorted, reverse direction
                            foreach (var sd in sds)
                            {
                                if (sd.PropertyName == pn && sd.Direction == ListSortDirection.Ascending)
                                {
                                    direction = ListSortDirection.Descending;
                                    break;
                                }
                            }
                            using (view.DeferRefresh())
                            {
                                sds.Clear();
                                sds.Add(new SortDescription(pn, direction));
                            }
                        }
                        catch
                        {
                        }

                    }
                    else
                    {
                        if (this.SortMode == SortMode.Manual)
                        {
                            // if already sorted, reverse direction
                            foreach (var sd in ManualSortDescriptions)
                            {
                                if (sd.PropertyName == pn && sd.Direction == ListSortDirection.Ascending)
                                {
                                    direction = ListSortDirection.Descending;
                                    break;
                                }
                            }
                            ManualSortDescriptions.Clear();

                            ManualSortDescriptions.Add(new SortDescription(pn, direction));
                            this.Invalidate();
                        }
                    }
                }
            }
        }
        #endregion

        #region DP
        private void OnItemsSourceChanged()
        {
            _manualSort.Clear();
            ScrollPosition = new Point(0, 0);
            if (_view != null)
            {
                _view.VectorChanged -= _view_VectorChanged;
            }
            _view = ItemsSource as ICollectionView;

            _props = null;
            _itemType = null;
            if (_view == null && ItemsSource != null)
            {
                _view = new UWPCollectionView(ItemsSource);
            }

            // remove old rows, auto-generated columns
            Rows.Clear();
            ClearAutoGeneratedColumns();

            // bind grid to new data source
            if (_view != null)
            {
                // connect event handlers
                _view.VectorChanged += _view_VectorChanged;
                // get list of properties available for binding
                _props = GetItemProperties();

                // just in case GetItemProperties changed something
                ClearAutoGeneratedColumns();

                //auto - generate columns
                if (AutoGenerateColumns)
                {
                    using (Columns.DeferNotifications())
                    {
                        GenerateColumns(_props);
                    }
                }

                // initialize non-auto-generated column bindings
                foreach (var col in Columns)
                {
                    if (!col.AutoGenerated)
                    {
                        BindColumn(col);
                    }
                }

                // load rows
                LoadRows();

            }
        }


        #endregion

        #region View
        private void _view_VectorChanged(Windows.Foundation.Collections.IObservableVector<object> sender, Windows.Foundation.Collections.IVectorChangedEventArgs e)
        {
            if (_props == null || _props.Count == 0 || _itemType != GetItemType(_view))
            {
                if (_itemType == null || !MyUWPToolkit.Util.Util.IsPrimitive(_itemType))
                {
                    if (GetItemType(_view) != typeof(object))
                    {
                        OnItemsSourceChanged();
                        return;
                    }
                }
            }

            // handle the collection change
            OnViewChanged(sender, e);

        }

        internal void OnViewChanged(IObservableVector<object> sender, IVectorChangedEventArgs e)
        {
            // handle action
            var rows = Rows;
            var index = (int)e.Index;
            var rowIndex = GetRowIndex(index);
            switch (e.CollectionChange)
            {
                case CollectionChange.ItemInserted:

                    // create the new bound row
                    var r = CreateBoundRow(sender[index]);

                    if (rowIndex < 0)
                    {
                        rowIndex = rows.Count;
                    }

                    // add the new bound row to the rows collection
                    if (rowIndex > -1)
                    {
                        rows.Insert(rowIndex, r);
                    }
                    else
                    {
                        LoadRows();
                    }
                    break;

                case CollectionChange.ItemRemoved:
                    if (rowIndex > -1)
                    {
                        rows.RemoveAt(rowIndex);
                    }
                    else
                    {
                        LoadRows();
                    }
                    break;

                case CollectionChange.ItemChanged:
                    rows[rowIndex].DataItem = sender[index];
                    _cellPanel.Invalidate(new CellRange(rowIndex, 0, rowIndex, Columns.Count - 1));
                    break;

                default: // Reset, Move
                    LoadRows();
                    break;
            }

            // ensure scrollbars are in sync
            InvalidateArrange();
        }

        internal int GetRowIndex(int dataIndex)
        {
            if (dataIndex > -1)
            {
                // update DataIndex members
                Rows.Update();

                // look for the row with the right DataIndex
                for (int rowIndex = dataIndex; rowIndex < Rows.Count; rowIndex++)
                {
                    if (Rows[rowIndex].DataIndex == dataIndex)
                        return rowIndex;
                }
            }

            // not found
            return -1;
        }
        #endregion

        #region Columns
        private void ClearAutoGeneratedColumns()
        {
            for (int i = 0; i < Columns.Count; i++)
            {
                if (Columns[i].AutoGenerated)
                {
                    Columns.RemoveAt(i);
                    i--;
                }
            }
        }

        void GenerateColumns(Dictionary<string, PropertyInfo> props)
        {
            if (props.Count == 0 && _view != null)
            {
                // special case: binding directly to primitive types (int, string, etc)
                var type = GetItemType(_view);
                if (MyUWPToolkit.Util.Util.IsPrimitive(type))
                {
                    var col = new Column();
                    BindAutoColumn(col, type);
                    col.AutoGenerated = true;
                    Columns.Add(col);
                }
            }
            else
            {
                // generate one column for each property
                foreach (var cpi in props.Values)
                {
                    var col = new Column();
                    BindAutoColumn(col, cpi);
                    col.AutoGenerated = true;
                    Columns.Add(col);
                }
            }
        }

        void BindAutoColumn(Column c, PropertyInfo cpi)
        {
            // create automatic binding (property name may be enclosed in square brackets)
            var name = cpi.Name;
            List<char> specialChar = new List<char>() { '/', '.' };
            bool containsSpecialChar = specialChar.Any(letter => name.Contains(letter));
            if (name != null && name.StartsWith("[") && name.EndsWith("]") && !containsSpecialChar)
            {
                name = name.Substring(1, name.Length - 2);
            }
            var b = new Binding { Path = new PropertyPath(name) };
            b.Mode = !cpi.CanWrite ? BindingMode.OneWay : BindingMode.TwoWay;

            // assign name, binding, property info to column (pi after b!!!!)
            c.ColumnName = cpi.Name;
            c.Binding = b;
            c.PropertyInfo = cpi;

            // initialize alignment, format, etc
            InitializeAutoColumn(c, cpi.PropertyType);
        }

        void BindAutoColumn(Column c, Type type)
        {
            c.Header = type != null ? type.Name : null;
            var b = new Binding();
            if (c.BoundPropertyName == null)
            {
                b.Mode = BindingMode.OneWay;
            }
            c.Binding = b;

            InitializeAutoColumn(c, type);
        }

        // initialize auto column properties based on data type
        void InitializeAutoColumn(Column c, Type type)
        {
            // save column type
            c.DataType = type;

            // handle nullable types
            type = type.GetNonNullableType();

            // initialize column properties based on type
            if (type == typeof(string))
            {
                c.Width = new GridLength(180);
            }
            else if (type.IsNumericIntegral())
            {
                c.Width = new GridLength(80);
                c.Format = "n0";
            }
            else if (type.IsNumericNonIntegral())
            {
                c.Width = new GridLength(80);
                c.Format = "n2";
            }
            else if (type == typeof(bool))
            {
                c.Width = new GridLength(60);
            }
            else if (type == typeof(DateTime))
            {
                c.Format = "d";
            }
        }

        // bind custom column (non-auto generated)
        internal void BindColumn(Column c)
        {
            var b = c.Binding;
            if (b != null)
            {
                // get path from binding (may be null)
                var path = b.Path != null ? b.Path.Path : string.Empty;

                // get PropertyInfo from binding
                PropertyInfo cpi = null;
                if (_props != null && b.Path != null && _props.TryGetValue(path, out cpi))
                {
                    c.PropertyInfo = cpi;
                    if (c.PropertyInfo == null && (c.DataType == null || c.DataType == typeof(object)))
                    {
                        c.DataType = cpi.PropertyType;
                    }
                }

                // set column name if empty
                if (string.IsNullOrEmpty(c.ColumnName))
                {
                    c.ColumnName = cpi != null ? cpi.Name : path;
                }
            }
        }
        #endregion

        #region Rows
        void LoadRows()
        {
            if (_view != null)
            {
                using (Rows.DeferNotifications())
                {
                    // add all data items
                    Rows.Clear();
                    CreateBoundRows();
                }
                // show new data and sorting order
                Invalidate();
            }
        }

        private void CreateBoundRows()
        {
            if (_view != null)
            {

                int count = _view.Count;
                for (int i = 0; i < _view.Count; i++)
                {
                    var item = _view[i];

                    var r = CreateBoundRow(item);
                    Rows.Add(r);
                }

            }
        }

        private Row CreateBoundRow(object dataItem)
        {
            return new BoundRow(dataItem);
        }
        #endregion

        #region Common
        private Dictionary<string, PropertyInfo> GetItemProperties()
        {
            var props = new Dictionary<string, PropertyInfo>();
            if (_view != null)
            {
                // get item type
                _itemType = GetItemType(_view);

                if (_itemType != null && !MyUWPToolkit.Util.Util.IsPrimitive(_itemType))
                {

                    foreach (var pi in _itemType.GetRuntimeProperties())
                    {
                        // skip indexed properties
                        var ix = pi.GetIndexParameters();
                        if (ix != null && ix.Length > 0)
                        {
                            continue;
                        }

                        // keep this one
                        props[pi.Name] = pi;
                    }
                }
            }

            return props;
        }

        private Type GetItemType(ICollectionView view)
        {
            if (view != null)
            {
                // get type from current item
                if (view.CurrentItem != null)
                {
                    return view.CurrentItem.GetType();
                }

                // get type from *any* item
                foreach (var item in view)
                {
                    if (item != null)
                    {
                        return item.GetType();
                    }
                }
            }

            return null;
        }


        internal void Invalidate()
        {
            if (_contentGrid != null)
            {
                // invalidate cells
                _columnHeaderPanel.Invalidate();
                _cellPanel.Invalidate();
            }
        }

        internal void DisposeCell(DataGridPanel panel, FrameworkElement cell)
        {
            var cf = GetCellFactory();
            cf.DisposeCell(this, panel.CellType, cell);
        }

        private ICellFactory GetCellFactory()
        {
            return _cellFactory == null ? _defautlCellFactory : _cellFactory;
        }

        internal FrameworkElement CreateCell(DataGridPanel panel, CellRange rng)
        {
            var cf = GetCellFactory();
            return cf.CreateCell(this, panel.CellType, rng);
        }

        internal bool TryChangeType(ref object value, Type type)
        {
            if (type != null && type != typeof(object))
            {
                // handle nullable types
                if (type.IsNullableType())
                {
                    // if value is null, we're done
                    if (value == null || object.Equals(value, string.Empty))
                    {
                        value = null;
                        return true;
                    }

                    // get actual type for parsing
                    type = Nullable.GetUnderlyingType(type);
                }
                else if (type.GetTypeInfo().IsValueType && value == null)
                {
                    // not nullable, can't assign null value to this
                    return false;
                }

                // handle special numeric formatting
                var ci = GetCultureInfo();
                var str = value as string;
                if (!string.IsNullOrEmpty(str) && type.IsNumeric())
                {
                    // handle percentages (ci.NumberFormat.PercentSymbol? overkill...)
                    bool pct = str[0] == '%' || str[str.Length - 1] == '%';
                    if (pct)
                    {
                        str = str.Trim('%'); // TFS 47808
                    }
                    decimal d;
                    if (decimal.TryParse(str, NumberStyles.Any, ci, out d))
                    {
                        if (pct)
                        {
                            value = d / 100;
                        }
                        else
                        {
                            // <<IP>> for currencies Convert.ChangeType will always give exception if we do without parsing,
                            // so change value here to the parsed one
                            value = d;
                        }
                    }
                    else
                    {
                        return false;
                    }
                }

                // ready to change type
                try
                {
                    value = Convert.ChangeType(value, type, ci);
                }
                catch
                {
                    return false;
                }
            }
            return true;
        }

        internal CultureInfo GetCultureInfo()
        {
            // refresh culture info based on this.Language (as in Bindings)
            if (_ci == null || _lang != this.Language)
            {
                _lang = this.Language;
                _ci = CultureInfo.CurrentCulture;
            }

            // done
            return _ci;
        }

        internal void UpdateStarSizes()
        {
            if (_contentGrid != null && Columns != null && Columns.Grid == this)
            {
                var width = _contentGrid.ActualWidth;
                Columns.UpdateStarSizes(width);
            }
        }

        internal ListSortDirection? GetColumnSort(int col)
        {
            var view = _view as ICollectionViewEx;
            if (view != null)
            {
                var colName = Columns[col].BoundPropertyName;
                foreach (var sd in view.SortDescriptions)
                {
                    if (sd.PropertyName == colName)
                    {
                        return sd.Direction;
                    }
                }
            }
            if (SortMode == SortMode.Manual)
            {
                var colName = Columns[col].BoundPropertyName;
                foreach (var sd in ManualSortDescriptions)
                {
                    if (sd.PropertyName == colName)
                    {
                        return sd.Direction;
                    }
                }
            }
            return null;
        }
        #endregion

        #endregion

        #region public Methods
        public IEnumerable<object> GetVisibleItems()
        {
            if (_cellPanel != null && _view != null)
            {
                var viewRange = _cellPanel.ViewRange;

                for (int i = viewRange.Row; i <= viewRange.Row2; i++)
                {
                    yield return _view[i];
                }
            }
            else
            {
                yield return null;
            }
        }
        #endregion
    }

}