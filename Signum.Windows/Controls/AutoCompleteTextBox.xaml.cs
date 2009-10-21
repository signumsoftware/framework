using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
using System.Timers;
using System.Collections;
using System.Windows.Threading;
using System.Reflection;
using Signum.Utilities;

namespace Signum.Windows
{
    /// <summary>
    /// Interaction logic for AutoCompleteTextBox.xaml
    /// </summary>    
    public partial class AutoCompleteTextBox : Grid
    {

        public static readonly RoutedEvent SelectedItemChangedEvent = EventManager.RegisterRoutedEvent(
            "SelectedItemChanged", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(AutoCompleteTextBox));
        public event RoutedEventHandler SelectedItemChanged
        {
            add { AddHandler(SelectedItemChangedEvent, value); }
            remove { RemoveHandler(SelectedItemChangedEvent, value); }
        }


        public static readonly RoutedEvent RealLostFocusEvent = EventManager.RegisterRoutedEvent(
            "RealLostFocus", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(AutoCompleteTextBox));
        public event RoutedEventHandler RealLostFocus
        {
            add { AddHandler(RealLostFocusEvent, value); }
            remove { RemoveHandler(RealLostFocusEvent, value); }
        }


        bool insertText;
        Timer timer;
        bool abort = false;

        public string Text
        {
            get { return textBox.Text; }
            set
            {
                insertText = true;
                textBox.Text = value;
            }
        }
        public object SelectedItem
        {
            get { return listBox.SelectedItem; }
        }

        public int Threshold { get; set; }
        public int Delay { get; set; }

        public event Func<string, IEnumerable> AutoCompleting; 

        public AutoCompleteTextBox()
        {
            InitializeComponent();
            AsserTimer();
            lostFocusTimer.Elapsed += new ElapsedEventHandler(lostFocusTimer_Elapsed);
            Delay = 300;
        }

        private void AsserTimer()
        {
            if (Delay == 0)
                timer = null;
            else
            {
                if (timer == null)
                    timer = new Timer();
                timer.Interval = Delay;
                timer.AutoReset = false;
                timer.Elapsed += new ElapsedEventHandler(timer_Elapsed);
            }
        }
    
        private void TextChanged(IEnumerable values)
        {
            listBox.ItemsSource = values;
            popup.IsOpen = listBox.HasItems;
        }

        public void SelectAll()
        {
            textBox.SelectAll(); 
        }

        private void textBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // text was not typed, do nothing and consume the flag
            if (insertText == true)
                insertText = false;
            // if the delay time is set, delay handling of text changed
            else if (textBox.Text.Length >= Threshold)
            {
                if (Delay == 0)
                    AutoComplete();
                else
                {
                    AsserTimer();
                    abort = false;
                    timer.Start();
                }
            }
        }

        void timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            timer.Stop();
            Dispatcher.Invoke(new Action(AutoComplete)); 
        }

        private void AutoComplete()
        {
            string text = textBox.Text;

            new Action(delegate
            {
                IEnumerable result = AutoCompleting(text);
                Dispatcher.BeginInvoke(new Action(delegate
                {
                    if (!abort)
                    {
                        listBox.ItemsSource = result;
                        popup.IsOpen = listBox.HasItems && textBox.IsFocused;
                    }
                }));

            }).BeginInvoke(null, null);
        }

        public new bool Focus()
        {
            return textBox.Focus();
        }

        private void textBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {   
            if (e.Key == Key.Down)
            {
                abort = true;
                if (listBox.HasItems)
                {
                    listBox.SelectedIndex = 0;
                    ListBoxItem lbi =  (ListBoxItem)listBox.ItemContainerGenerator.ContainerFromIndex(0);
                    lbi.Focus();
                }
                e.Handled = true; 
            }
            if (e.Key == Key.Tab)
            {
                switch (listBox.Items.Count) {
                    case 0: textBox.Text = string.Empty;
                            //e.Handled = true;
                            break;

                    case 1: listBox.SelectedItem = listBox.Items[0];
                            SelectItem();
                            break;

                    default:
                            foreach (object o in listBox.ItemsSource)
                            {
                                if (string.Equals(o.ToString(), textBox.Text, StringComparison.InvariantCultureIgnoreCase))
                                {
                                    listBox.SelectedItem = o;
                                    SelectItem();
                                    break;
                                }
                            }
                            break;
                }
            }
        }

        private void listbox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Tab)
            {
                SelectItem();
                e.Handled = true; 
            }
            if (e.Key == Key.Escape)
            {
                popup.IsOpen = false;
                textBox.Focus(); 
                e.Handled = true; 
            }
            if (e.Key == Key.Up || e.Key == Key.Down)
                e.Handled = true; 
        }

        private void listBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            SelectItem();
        }

        private void SelectItem()
        {
            popup.IsOpen = false;
            RaiseEvent(new RoutedEventArgs(SelectedItemChangedEvent, this)); 
        }

        internal void Close()
        {
            popup.IsOpen = false; 
        }

        Timer lostFocusTimer = new Timer(100) { AutoReset = false };

        private void textBox_LostFocus(object sender, RoutedEventArgs e)
        {
            lostFocusTimer.Start();  
        }
        private void listBox_LostFocus(object sender, RoutedEventArgs e)
        {
            lostFocusTimer.Start();
        }
        void lostFocusTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(delegate
            {
                if (!textBox.IsFocused && Common.FindChildrenBreadthFirst(listBox, dep => (dep is UIElement) && ((UIElement)dep).IsFocused) == null)
                    RaiseEvent(new RoutedEventArgs(RealLostFocusEvent, this));
            })); 
        }
    }
}