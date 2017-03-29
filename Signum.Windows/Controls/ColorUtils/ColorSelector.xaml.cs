using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Signum.Windows.ColorUtils
{
    /// <summary>
    /// Interaction logic for ColorSelector.xaml
    /// </summary>
    public partial class ColorSelector : UserControl
    {
        private TranslateTransform markerTransform = new TranslateTransform();
        private Point m_ColorPosition;

        public ColorSelector()
        {
            InitializeComponent();

            PART_ColorMarker.RenderTransform = markerTransform;

            updateMarkerPosition(SelectedColor);
        }

        #region Public Properties

        public static readonly DependencyProperty SelectedColorProperty =
        DependencyProperty.Register
        ("SelectedColor", typeof(Color), typeof(ColorSelector),
        new FrameworkPropertyMetadata(Colors.Black, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, 
            (o, e) => ((ColorSelector)o).SelectedColorChanging(e)));

    
        // Gets or sets the selected color.
        public Color SelectedColor
        {
            get
            {
                return (Color)GetValue(SelectedColorProperty);
            }
            set
            {
                SetValue(SelectedColorProperty, value);
            }
        }

        #endregion

        #region Public Events

        public static readonly RoutedEvent SelectedColorChangedEvent = EventManager.RegisterRoutedEvent(
            "SelectedColorChanged",
            RoutingStrategy.Bubble,
            typeof(RoutedPropertyChangedEventHandler<Color>),
            typeof(ColorSelector)
            );

        public event RoutedPropertyChangedEventHandler<Color> SelectedColorChanged
        {
            add { AddHandler(SelectedColorChangedEvent, value); }
            remove { RemoveHandler(SelectedColorChangedEvent, value); }
        }

        #endregion

        #region Property Changed Callbacks

        private void SelectedColorChanging(DependencyPropertyChangedEventArgs e)
        {
            Color newC = (Color)e.NewValue;

            updateMarkerPosition(newC);

            OnSelectedColorChanged((Color)e.OldValue, newC);
        }

        protected virtual void OnSelectedColorChanged(Color oldColor, Color newColor)
        {
            RaiseEvent(new RoutedPropertyChangedEventArgs<Color>(oldColor, newColor, ColorSelector.SelectedColorChangedEvent));
        }

        #endregion

        #region Color Resolution Helpers

        private void setMarkerPosition(Point p)
        {
            m_ColorPosition = new Point(
                Math.Min(Math.Max(0, p.X / PART_ColorDetail.ActualWidth), 1),
                Math.Min(Math.Max(0, p.Y / PART_ColorDetail.ActualHeight), 1)
                );

            updateTransform();

            determineColor();
        }

        private void updateTransform()
        {
            markerTransform.X = m_ColorPosition.X * PART_ColorDetail.ActualWidth;
            markerTransform.Y = m_ColorPosition.Y * PART_ColorDetail.ActualHeight;
        }


        bool updating = false; 
        private void updateMarkerPosition(Color theColor)
        {
            if (updating) return;
            try
            {
                updating = true;

                opacitySlider.Value = theColor.ScA;
                HsvColor hsv = HsvColor.FromColor(theColor);

                PART_ColorSlider.Value = hsv.H;

                Point p = new Point(hsv.S, 1 - hsv.V);
                m_ColorPosition = p;
                updateTransform();
            }
            finally
            {
                updating = false;
            }
        }


        private void determineColor()
        {
            if (updating) return;

            Point p = m_ColorPosition;
            HsvColor hsv = new HsvColor(PART_ColorSlider.Value, 1, 1)
            {
                S = p.X,
                V = 1 - p.Y
            };
            Color color = hsv.ToColor();

            SelectedColor = Color.FromArgb((byte)(255 * opacitySlider.Value), color.R, color.G, color.B);
        }

        #endregion

        private void opacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            determineColor();            
        }

        private void PART_ColorSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            determineColor();
        }

        private void PART_ColorDetail_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Mouse.Capture(PART_ColorDetail);
            Point p = e.GetPosition(PART_ColorDetail);
            setMarkerPosition(p);
        }

        private void PART_ColorDetail_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                Point p = e.GetPosition(PART_ColorDetail);
                setMarkerPosition(p);
                Mouse.Synchronize();
            }
        }

        private void PART_ColorDetail_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Mouse.Capture(null);
        }
    }
}
