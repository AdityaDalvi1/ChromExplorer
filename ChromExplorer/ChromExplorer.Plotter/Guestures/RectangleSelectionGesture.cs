using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ChromExplorer.Plotter.Guestures
{
    /// <summary>
    /// This class provides the ability to draw a rectangle on a zoomable object and zoom into that location.
    /// </summary>
    internal class RectangleSelectionGesture
    {
        RectangleSelectionVisual _selectionRectVisual;
        Point _start;
        bool _watching;
        XYPlotter _target;
        //MapZoom _zoom;

        double zoom = 1;
        Panel _container;
        Point _mouseDownPoint;
        Rect _selectionRect;
        bool _zoomSelection;
        int _zoomSizeThreshold = 20;
        int _selectionThreshold = 5; // allow some mouse wiggle on mouse down without actually selecting stuff!
        ModifierKeys _mods;
        ModifierKeys _unzoom_mods;
        private ModifierKeys _selectMods;
        private SimplePlotter _parent;

        public event EventHandler Selected;
        public bool IsSelectEnabled { get; set; }
        public bool IsMultiSelectEnabled { get; set; }


        public RectangleSelectionGesture(XYPlotter target, SimplePlotter parent, ModifierKeys selectMods)
        {
            _parent = parent;
            _unzoom_mods = ModifierKeys.Shift;
            _mods = ModifierKeys.None;
            _selectMods = selectMods;
            _target = target;
            _container = target.Parent as Panel;
            
            if (_container == null)
            {
                throw new ArgumentException("Target object must live in a Panel");
            }

          
            _container.MouseLeftButtonDown += new MouseButtonEventHandler(OnMouseLeftButtonDown);
            _container.MouseLeftButtonUp += new MouseButtonEventHandler(OnMouseLeftButtonUp);
            _container.MouseMove += new MouseEventHandler(OnMouseMove);
            _container.MouseRightButtonUp += new MouseButtonEventHandler(_container_MouseRightButtonUp);

            
           
        }

        void _container_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!e.Handled && (Keyboard.Modifiers & _unzoom_mods) == _unzoom_mods)
            {
                _target.Unzoom();
                e.Handled = true;
            }
        }

        /// <summary>
        /// Get the rectangle the user drew on the target object.
        /// </summary>
        public Rect SelectionRectangle
        {
            get { return _selectionRect; }
        }

        /// <summary>
        /// Get/Set whether to also zoom the selected rectangle.
        /// </summary>
        public bool ZoomSelection
        {
            get { return _zoomSelection; }
            set { _zoomSelection = value; }
        }

        /// <summary>
        /// Handle the mouse left button down event
        /// </summary>
        /// <param name="sender">Mouse</param>
        /// <param name="e">Mouse down information</param>
        void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if(e.ClickCount == 2 && IsSelectEnabled)
            {
                OnMouseLeftDoubleClick(e);

                if(e.Handled)
                    return;
            }

            if (!e.Handled && (Keyboard.Modifiers & _mods) == _mods)
            {
                if (_selectionRectVisual != null)
                {
                    _container.Children.Remove(_selectionRectVisual);
                    _selectionRectVisual = null;
                }
                //_start = e.GetPosition(_container);
                _start = new Point(e.GetPosition(_container).X, 0.0);
                //_start = e.GetPosition(_target);

                _watching = true;
                _mouseDownPoint = _start;
            }
        }

        void OnMouseLeftDoubleClick(MouseButtonEventArgs e)
        {
            var args = e;

            e.Handled = true;

            var pos = GetCurrentPoint(e.GetPosition(_container));
            int newIndex = _target.GetNearestIndex(pos);

            if (newIndex > 0)
            {
                //_parent.SelectedIndex = newIndex;
                _parent.SelectedIndexes = null;
                _parent.SelectedIndex = newIndex;

            }
        }

        /// <summary>
        /// Get/Set threshold that sets the minimum size rectangle we will allow user to draw.
        /// This allows user to start drawing a rectangle by then change their mind and mouse up
        /// without trigging an almost infinite zoom out to a very smalle piece of real-estate.
        /// </summary>
        public int ZoomSizeThreshold
        {
            get { return _zoomSizeThreshold; }
            set { _zoomSizeThreshold = value; }
        }

        /// <summary>
        /// Handle Mouse Move event.  Here we detect whether we've exceeded the _selectionThreshold
        /// and if so capture the mouse and create the visual zoom rectangle on the container object.
        /// </summary>
        /// <param name="sender">Mouse</param>
        /// <param name="e">Mouse move information.</param>
        void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (_watching)
            {
                Point pos = e.GetPosition(_container);
                pos.Y = _container.ActualHeight;
                if (pos.Y > _container.ActualHeight)
                {
                    pos.Y = _container.ActualHeight;
                }

                if (new Vector(_start.X - pos.X, _start.Y - pos.Y).Length > _selectionThreshold)
                {
                    _watching = false;
                    Mouse.Capture(_target, CaptureMode.SubTree);
                    _selectionRectVisual = new RectangleSelectionVisual(_start, _start);
                    _container.Children.Add(_selectionRectVisual);
                }
            }
            if (_selectionRectVisual != null)
            {
                //if (_selectionRectVisual.Zoom != zoom)
                //{
                //    _selectionRectVisual.Zoom = zoom;
                //}
                Point pos = e.GetPosition(_container);
                pos.Y = _container.ActualHeight;
                _selectionRectVisual.SecondPoint = pos;
            }
        }

        /// <summary>
        /// Handle the mouse left button up event.  Here we actually process the selected rectangle
        /// if any by first raising an event for client to receive then also zooming to that rectangle
        /// if ZoomSelection is true
        /// </summary>
        /// <param name="sender">Mouse</param>
        /// <param name="e">Mouse button information</param>
        void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _watching = false;
            if (_selectionRectVisual != null)
            {
                Mouse.Capture(_target, CaptureMode.None);
                Point pos = e.GetPosition(_container);
                double f = Math.Min(Math.Abs(pos.X - _mouseDownPoint.X), Math.Abs(pos.Y - _mouseDownPoint.Y));
                Rect r = GetSelectionRect(pos);
                _selectionRect = r;
                if (Selected != null)
                {
                    Selected(this, EventArgs.Empty);
                }

                if (_zoomSelection && f > _zoomSizeThreshold)
                {

                    if (IsMultiSelectEnabled)
                    {
                        

                        var left = _target.GetNearestIndex(r.TopLeft);
                        var right = _target.GetNearestIndex(r.TopRight);

                        if (left > 0)
                            left--;

                        //if (right < _target.DataSource.Length - 1)
                        //    right++;

                        var selected = new List<int>();
                        for(var i = left; i <= right; i++)
                        {
                            selected.Add(i);

                            if(selected.Count > _parent.SelectionLimit)
                                break;
                        }

                        _parent.SelectedIndex = -1;
                        _parent.SelectedIndexes = selected;
                    }
                    else
                    {


                        _target.ZoomToRect(r);

                        if (IsSelectEnabled)
                        {
                            int newIndex = _target.GetNearestIndex(r.TopLeft);

                            if (newIndex > 0)
                            {
                                //_parent.SelectedIndex = newIndex;
                                _parent.SelectedIndexes = null;
                                _parent.SelectedIndex = newIndex;
                            }
                        }
                    }

                   

                }

                _container.Children.Remove(_selectionRectVisual);
                _selectionRectVisual = null;
            }
        }

        /// <summary>
        /// Get the actual selection rectangle that encompasses the mouse down position and the given point.
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        Rect GetSelectionRect(Point p)
        {
            Rect r = new Rect(_start, p);
            return _container.TransformToDescendant(_target).TransformBounds(r);
        }

        Point GetCurrentPoint(Point p)
        {
            return _container.TransformToDescendant(_target).Transform(p);
        }
    }
}
