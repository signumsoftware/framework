using System;
using System.IO;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Navigation;
using Signum.Utilities.DataStructures;

namespace Signum.Windows
{
	public partial class EntityButtons
	{
        public static readonly DependencyProperty OrientationProperty =
            DependencyProperty.Register("Orientation", typeof(Orientation), typeof(EntityButtons), new UIPropertyMetadata(Orientation.Horizontal));
        public Orientation Orientation
        {
            get { return (Orientation)GetValue(OrientationProperty); }
            set { SetValue(OrientationProperty, value); }
        }

        public static readonly DependencyProperty CreateProperty =
       DependencyProperty.Register("Create", typeof(bool), typeof(EntityButtons), new UIPropertyMetadata(true));
        public bool Create
        {
            get { return (bool)GetValue(CreateProperty); }
            set { SetValue(CreateProperty, value); }
        }

        public static readonly DependencyProperty ViewProperty =
            DependencyProperty.Register("View", typeof(bool), typeof(EntityButtons), new UIPropertyMetadata(true));
        public bool View
        {
            get { return (bool)GetValue(ViewProperty); }
            set { SetValue(ViewProperty, value); }
        }

        public static readonly DependencyProperty FindProperty =
            DependencyProperty.Register("Find", typeof(bool), typeof(EntityButtons), new UIPropertyMetadata(true));
        public bool Find
        {
            get { return (bool)GetValue(FindProperty); }
            set { SetValue(FindProperty, value); }
        }

        public static readonly DependencyProperty RemoveProperty =
            DependencyProperty.Register("Remove", typeof(bool), typeof(EntityButtons), new UIPropertyMetadata(true));
        public bool Remove
        {
            get { return (bool)GetValue(RemoveProperty); }
            set { SetValue(RemoveProperty, value); }
        }


        public static readonly RoutedEvent CreatingEvent = EventManager.RegisterRoutedEvent(
            "Creating", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(EntityButtons));
        public event RoutedEventHandler Creating
        {
            add { AddHandler(CreatingEvent, value); }
            remove { RemoveHandler(CreatingEvent, value); }
        }

        public static readonly RoutedEvent ViewingEvent = EventManager.RegisterRoutedEvent(
            "Viewing", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(EntityButtons));
        public event RoutedEventHandler Viewing
        {
            add { AddHandler(ViewingEvent, value); }
            remove { RemoveHandler(ViewingEvent, value); }
        }

        public static readonly RoutedEvent FindingEvent = EventManager.RegisterRoutedEvent(
            "Finding", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(EntityButtons));
        public event RoutedEventHandler Finding
        {
            add { AddHandler(FindingEvent, value); }
            remove { RemoveHandler(FindingEvent, value); }
        }

        public static readonly RoutedEvent RemovingEvent = EventManager.RegisterRoutedEvent(
            "Removing", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(EntityButtons));
        public event RoutedEventHandler Removing
        {
            add { AddHandler(RemovingEvent, value); }
            remove { RemoveHandler(RemovingEvent, value); }
        }

		public EntityButtons()
		{
			this.InitializeComponent();
            AddHandler(Button.ClickEvent, new RoutedEventHandler(ButtonClick));  		
        }

        public void ButtonClick(object sender, RoutedEventArgs e)
        {
            Button bt = (Button)e.OriginalSource;
            if (bt == btCreate) RaiseEvent(new RoutedEventArgs(CreatingEvent));
            else if (bt == btView) RaiseEvent(new RoutedEventArgs(ViewingEvent));
            else if (bt == btFind) RaiseEvent(new RoutedEventArgs(FindingEvent));
            else if (bt == btRemove) RaiseEvent(new RoutedEventArgs(RemovingEvent));
            else throw new NotImplementedException();
            e.Handled = true; 
        }
	}
}