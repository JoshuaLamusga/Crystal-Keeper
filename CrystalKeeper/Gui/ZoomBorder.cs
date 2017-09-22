using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace CrystalKeeper.Gui
{
    /// <summary>
    /// Zooms and pans a child element using transforms. Mouse wheel zooms,
    /// LMB drags to pan, RMB clicks to reset zoom and pan.
    /// </summary>
    public class ZoomBorder : Border
    {
        #region Members
        /// <summary>
        /// The element to zoom and pan inside the border.
        /// </summary>
        private UIElement child = null;

        /// <summary>
        /// Stores translation transform coords for panning.
        /// </summary>
        private Point origin;

        /// <summary>
        /// Stores the initial panning position.
        /// </summary>
        private Point start;

        /// <summary>
        /// Get/set the child element.
        /// </summary>
        public override UIElement Child
        {
            get
            {
                return base.Child;
            }
            set
            {
                if (value != null && value != Child)
                {
                    Initialize(value);
                }

                base.Child = value;
            }
        }
        #endregion

        #region Methods
        /// <summary>
        /// Captures the mouse on the child element for panning.
        /// </summary>
        private void Child_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (child != null)
            {
                var tt = GetTranslateTransform(child);
                start = e.GetPosition(this);
                origin = new Point(tt.X, tt.Y);
                Cursor = Cursors.Hand;
                child.CaptureMouse();
            }
        }

        /// <summary>
        /// Stops panning the child element when the mouse is released.
        /// </summary>
        private void Child_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (child != null)
            {
                child.ReleaseMouseCapture();
                Cursor = Cursors.Arrow;
            }
        }

        /// <summary>
        /// Handles panning the child element while the mouse is held.
        /// </summary>
        private void Child_MouseMove(object sender, MouseEventArgs e)
        {
            if (child != null && child.IsMouseCaptured)
            {
                var tt = GetTranslateTransform(child);
                Vector v = start - e.GetPosition(this);
                tt.X = origin.X - v.X;
                tt.Y = origin.Y - v.Y;
            }
        }

        /// <summary>
        /// Zooms the child element in and out with the mousewheel.
        /// </summary>
        private void Child_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (child != null)
            {
                var st = GetScaleTransform(child);
                var tt = GetTranslateTransform(child);

                double zoom = e.Delta > 0 ? .2 : -.2;
                if (!(e.Delta > 0) && (st.ScaleX < .4 || st.ScaleY < .4))
                    return;

                Point relative = e.GetPosition(child);
                double absoluteX;
                double absoluteY;

                absoluteX = relative.X * st.ScaleX + tt.X;
                absoluteY = relative.Y * st.ScaleY + tt.Y;

                st.ScaleX += zoom;
                st.ScaleY += zoom;

                tt.X = absoluteX - relative.X * st.ScaleX;
                tt.Y = absoluteY - relative.Y * st.ScaleY;
            }
        }

        /// <summary>
        /// Right-clicking the mouse resets child zoom and pan to default.
        /// </summary>
        private void Child_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            Reset();
        }

        /// <summary>
        /// Returns the current scale transform on the child element.
        /// </summary>
        private ScaleTransform GetScaleTransform(UIElement element)
        {
            return (ScaleTransform)((TransformGroup)element.RenderTransform)
              .Children.First(tr => tr is ScaleTransform);
        }

        /// <summary>
        /// Returns the current translation transform on the child element.
        /// </summary>
        private TranslateTransform GetTranslateTransform(UIElement element)
        {
            return (TranslateTransform)((TransformGroup)element.RenderTransform)
              .Children.First(tr => tr is TranslateTransform);
        }

        /// <summary>
        /// Connects zoom border controls to the child element given.
        /// </summary>
        public void Initialize(UIElement element)
        {
            child = element;
            if (child != null)
            {
                //Creates and adds transforms.
                TransformGroup group = new TransformGroup();
                ScaleTransform st = new ScaleTransform();
                group.Children.Add(st);

                TranslateTransform tt = new TranslateTransform();
                group.Children.Add(tt);

                child.RenderTransform = group;
                child.RenderTransformOrigin = new Point(0.0, 0.0);

                //Connects all mouse events.
                MouseWheel += Child_MouseWheel;
                MouseLeftButtonDown += Child_MouseLeftButtonDown;
                MouseLeftButtonUp += Child_MouseLeftButtonUp;
                MouseMove += Child_MouseMove;
                PreviewMouseRightButtonDown += new MouseButtonEventHandler(
                  Child_PreviewMouseRightButtonDown);
            }
        }

        /// <summary>
        /// Resets the zoom and pan on the child element.
        /// </summary>
        public void Reset()
        {
            if (child != null)
            {
                //Resets zoom
                var st = GetScaleTransform(child);
                st.ScaleX = 1.0;
                st.ScaleY = 1.0;

                //Resets pan
                var tt = GetTranslateTransform(child);
                tt.X = 0.0;
                tt.Y = 0.0;
            }
        }
        #endregion
    }
}