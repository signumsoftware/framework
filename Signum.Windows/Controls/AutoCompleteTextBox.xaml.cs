using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Collections;
using System.Windows.Controls.Primitives;
using System.ComponentModel;
using System.Windows.Threading;
using System.Diagnostics;
using System.Threading;
using System.Windows.Media;

namespace Signum.Windows
{
    //http://www.lazarciuc.ro/ioan/2008/06/01/auto-complete-for-textboxes-in-wpf/
    public partial class AutoCompleteTextBox : UserControl
    {
        public static readonly RoutedEvent ClosedEvent =
            EventManager.RegisterRoutedEvent("Closed", RoutingStrategy.Bubble, typeof(ClosedEventHandler), typeof(AutoCompleteTextBox));
        public event RoutedEventHandler Closed
        {
            add { AddHandler(ClosedEvent, value); }
            remove { RemoveHandler(ClosedEvent, value); }
        }

        public event Func<string, IEnumerable> AutoCompleting;

        public static readonly DependencyProperty SelectedItemProperty =
            DependencyProperty.Register("SelectedItem", typeof(object), typeof(AutoCompleteTextBox), new UIPropertyMetadata(null));
        public object SelectedItem
        {
            get { return (object)GetValue(SelectedItemProperty); }
            set { SetValue(SelectedItemProperty, value); }
        }

        public static readonly DependencyProperty MinTypedCharactersProperty =
            DependencyProperty.Register("MinTypedCharacters", typeof(int), typeof(AutoCompleteTextBox), new UIPropertyMetadata(2));
        public int MinTypedCharacters
        {
            get { return (int)GetValue(MinTypedCharactersProperty); }
            set { SetValue(MinTypedCharactersProperty, value); }
        }

        DispatcherTimer delayTimer = new DispatcherTimer(DispatcherPriority.Normal);

        public TimeSpan Delay
        {
            get { return delayTimer.Interval; }
            set { delayTimer.Interval = value; }
        }
        
        public AutoCompleteTextBox()
        {
            InitializeComponent();
            delayTimer.Interval = TimeSpan.FromMilliseconds(300);
            itemsSelected = false;
            delayTimer.Tick += new EventHandler(delayTimer_Tick);
        }

        void delayTimer_Tick(object sender, EventArgs e)
        {
            delayTimer.Stop();

            if (AutoCompleting == null) 
                throw new NullReferenceException("SeachMethod cannot be null.");

            IEnumerable res = AutoCompleting(txtBox.Text);


            lstBox.ItemsSource = res;
            if (lstBox.Items.Count > 0)
            {
                lstBox.SelectedIndex = -1;
                pop.IsOpen = true;
            }
            else
            {
                pop.IsOpen = false;
            }
        }

        void MoveDown()
        {
            if (lstBox.SelectedIndex < lstBox.Items.Count - 1)
            {
                lstBox.SelectedIndex++;
            }
        }

        void MoveUp()
        {
            if (lstBox.SelectedIndex > 0)
            {
                lstBox.SelectedIndex--;
            }
        }
    
        void txtBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Up)
            {
                MoveUp();
                e.Handled = true;
            }
            else if (e.Key == Key.Down)
            {
                MoveDown();
                e.Handled = true;
            }
            else if (e.Key == Key.Tab)
            {
                if (lstBox.SelectedItem != null)
                    Commit(CloseReason.Tab);
                else
                    Close(CloseReason.TabExit);

                 txtBox.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
                
                e.Handled = true;
            }
            else if (e.Key == Key.Enter)
            {
                if(lstBox.SelectedItem != null)
                    Commit(CloseReason.Enter);
                
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                Close(CloseReason.Esc);
                e.Handled = true;
            }
           
        }

        void txtBox_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (IsTextChangingKey(e.Key))
            {
                Suggest();
            }
        }


        bool IsTextChangingKey(Key key)
        {
            if (key == Key.Back || key == Key.Delete)
                return true;
            else
            {
                KeyConverter conv = new KeyConverter();
                string keyString = (string)conv.ConvertTo(key, typeof(string));

                return keyString.Length == 1;
            }
        }

        public void Close(CloseReason reason)
        {
            pop.IsOpen = false;
            RaiseEvent(new CloseEventArgs(reason));
        }


        void Commit(CloseReason reason)
        {
            pop.IsOpen = false;
            if (lstBox.SelectedItem != null)
            {
                txtBox.Text = lstBox.SelectedItem.ToString();

                Close(reason); 

                itemsSelected = true;
            }
        }

        private void txtBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (itemsSelected)
                itemsSelected = false;
            else Suggest();
        }

        private void txtBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (!pop.IsKeyboardFocusWithin)
                Close(CloseReason.LostFocus);
        }

        private void Suggest()
        {
            if (txtBox.Text.Length < MinTypedCharacters)
            {
                pop.IsOpen = false;
                lstBox.ItemsSource = null;
                return;
            }

            delayTimer.Start(); 
        }

        private bool itemsSelected;
        private void lstBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (pop.IsKeyboardFocusWithin)
            {
                Commit(CloseReason.ClickList);
            }
        }

        public void SelectAndFocus()
        {
            txtBox.SelectAll();
            txtBox.Focus();
            Mouse.Capture(this, CaptureMode.SubTree); 
        }

        public string Text
        {
            get { return txtBox.Text; }
            set { txtBox.Text = value; }
        }

        private void userControl_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            //Console.WriteLine("Down");
            if (!txtBox.IsMouseOver && !pop.IsMouseOver)
            {
                ReleaseMouseCapture();
                Close(CloseReason.ClickOut);
            }
        }

        //private void me_GotMouseCapture(object sender, MouseEventArgs e)
        //{
        //    Console.WriteLine("Got");
        //}

        //private void me_LostMouseCapture(object sender, MouseEventArgs e)
        //{
        //    Console.WriteLine("Lost");
        //}
    }

    public enum CloseReason
    {
        ClickList,
        Enter,
        Tab,
        TabExit,
        Esc,
        LostFocus,
        ClickOut
    }

    public class CloseEventArgs : RoutedEventArgs
    {
        public CloseReason Reason { get; private set; }
        public bool IsCommit
        {
            get
            {
                return
                    Reason == CloseReason.Enter ||
                    Reason == CloseReason.Tab ||
                    Reason == CloseReason.ClickList;
            }
        }

        public CloseEventArgs(CloseReason reason)
            : base(AutoCompleteTextBox.ClosedEvent)
        {
            Reason = reason;
        }
    }

    public delegate void ClosedEventHandler(object sender, CloseEventArgs e);
}
