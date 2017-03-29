using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Shapes;
using Signum.Utilities;
using System.Linq;

namespace Signum.Windows.ColorUtils
{
    public class SpectrumSlider : Slider
    {
        static SpectrumSlider()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(SpectrumSlider),
                new FrameworkPropertyMetadata(typeof(SpectrumSlider)));
        }

        public static readonly DependencyProperty SelectedColorProperty =
           DependencyProperty.Register
           ("SelectedColor", typeof(Color), typeof(SpectrumSlider),
           new PropertyMetadata(System.Windows.Media.Colors.Transparent));

        public Color SelectedColor
        {
            get { return (Color)GetValue(SelectedColorProperty); }
            set { SetValue(SelectedColorProperty, value); }
        }

        private static string SpectrumDisplayName = "PART_SpectrumDisplay";
        private Rectangle m_spectrumDisplay;
        private LinearGradientBrush pickerBrush;


        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            m_spectrumDisplay = GetTemplateChild(SpectrumDisplayName) as Rectangle;
            updateColorSpectrum();
        }

        protected override void OnValueChanged(double oldValue, double newValue)
        {
            base.OnValueChanged(oldValue, newValue);
            Color theColor = new HsvColor(newValue, 1.0, 1.0).ToColor();
            this.SelectedColor = theColor;
        }

        private void updateColorSpectrum()
        {
            if (m_spectrumDisplay != null)
            {
                createSpectrum();
            }
        }

        private void createSpectrum()
        {

            pickerBrush = new LinearGradientBrush()
            {
                StartPoint = new Point(0.5, 1),
                EndPoint = new Point(0.5, 0),
                ColorInterpolationMode = ColorInterpolationMode.SRgbLinearInterpolation
            };
            List<GradientStop> colorsList = 0.To(31).Select(i => new GradientStop(
                new HsvColor((double)((i % 30) * 12), 1.0, 1.0).ToColor(), ((double)i) / 30.0)).ToList();

            pickerBrush.GradientStops = new GradientStopCollection(colorsList);
            m_spectrumDisplay.Fill = pickerBrush;

        }
    }


    public class ColorThumb : Thumb
    {
        static ColorThumb()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ColorThumb),
                new FrameworkPropertyMetadata(typeof(ColorThumb)));
        }

        public static readonly DependencyProperty ThumbColorProperty =
        DependencyProperty.Register
        ("ThumbColor", typeof(Color), typeof(ColorThumb),
            new FrameworkPropertyMetadata(Colors.Transparent));

        public Color ThumbColor
        {
            get
            {
                return (Color)GetValue(ThumbColorProperty);
            }
            set
            {

                SetValue(ThumbColorProperty, value);
            }
        }
    }
}
