using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Signum.Utilities;

namespace Signum.Windows.Extensions
{
    public class TreeMapPanel : Panel
    {
        public static readonly DependencyProperty AreaProperty =  DependencyProperty.RegisterAttached("Area",
                                        typeof(double),typeof(TreeMapPanel), new FrameworkPropertyMetadata(1.0, FrameworkPropertyMetadataOptions.AffectsParentArrange));

        public static double GetArea(DependencyObject element)
        {
            if (element == null)
                throw new ArgumentNullException("element");
            return (double)element.GetValue(AreaProperty);
        }

        public static void SetArea(DependencyObject element, double value)
        {
            if (element == null)
                throw new ArgumentNullException("element");
            element.SetValue(AreaProperty, value);
        }

        struct ChildArea
        {
            public UIElement Child;
            public double Area;
        }

        private const double tol = 1e-2;

        protected override Size ArrangeOverride(Size finalSize)
        {
            if (finalSize.Width < tol || finalSize.Height < tol)
                return finalSize;

            ChildArea[] children = (from UIElement child in InternalChildren
                                    let area = GetArea(child)
                                    orderby area descending
                                    select new ChildArea { Child = child, Area = area }).ToArray(); 
           

            Rect strip = new Rect(finalSize);
            double remainingWeight = children.Sum(c => c.Area);

            int arranged = 0;
            while (arranged < children.Length)
            {
                double bestStripArea = 0;
                double bestRatio = double.PositiveInfinity;

                int i;

                if (strip.Width < tol || strip.Height < tol)
                    return finalSize;

                if (strip.Width > strip.Height)
                {
                    double bestWidth = strip.Width;

                    // Arrange Vertically
                    for (i = arranged; i < children.Length; i++)
                    {
                        double stripArea = bestStripArea + children[i].Area;
                        double ratio = double.PositiveInfinity;
                        double width = strip.Width * stripArea / remainingWeight;

                        for (int j = arranged; j <= i; j++)
                        {
                            double height = strip.Height * children[j].Area / stripArea;
                            ratio = Math.Min(ratio, height > width ? height / width : width / height);

                            if (ratio > bestRatio)
                                goto ArrangeVertical;
                        }
                        bestRatio = ratio;
                        bestWidth = width;
                        bestStripArea = stripArea;
                    }

                ArrangeVertical:
                    double y = strip.Y;
                    for (; arranged < i; arranged++)
                    {
                        UIElement child = children[arranged].Child;

                        double height = strip.Height * children[arranged].Area / bestStripArea;
                        child.Arrange(new Rect(strip.X, y, bestWidth, height));
                        y += height;
                    }

                    strip.X = strip.X + bestWidth;
                    strip.Width = Math.Max(0.0, strip.Width - bestWidth);
                }
                else
                {
                    double bestHeight = strip.Height;

                    // Arrange Horizontally
                    for (i = arranged; i < children.Length; i++)
                    {
                        double stripArea = bestStripArea + children[i].Area;
                        double ratio = double.PositiveInfinity;
                        double height = strip.Height * stripArea / remainingWeight;

                        for (int j = arranged; j <= i; j++)
                        {
                            double width = strip.Width * children[j].Area / stripArea;
                            ratio = Math.Min(ratio, height > width ? height / width : width / height);

                            if (ratio > bestRatio)
                                goto ArrangeHorizontal;
                        }
                        bestRatio = ratio;
                        bestHeight = height;
                        bestStripArea = stripArea;
                    }

                ArrangeHorizontal:
                    double x = strip.X;
                    for (; arranged < i; arranged++)
                    {
                        UIElement child = children[arranged].Child;

                        double width = strip.Width * children[arranged].Area / bestStripArea;
                        child.Arrange(new Rect(x, strip.Y, width, bestHeight));
                        x += width;
                    }

                    strip.Y = strip.Y + bestHeight;
                    strip.Height = Math.Max(0.0, strip.Height - bestHeight);
                }
                remainingWeight -= bestStripArea;
            }

            return finalSize;
        }

     

    }
}