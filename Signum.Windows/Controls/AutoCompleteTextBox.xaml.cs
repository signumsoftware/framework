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
using Signum.Utilities;
using System.Windows.Automation.Peers;
using System.Collections.Generic;
using System.Windows.Automation.Provider;

namespace Signum.Windows
{
    //http://www.lazarciuc.ro/ioan/2008/06/01/auto-complete-for-textboxes-in-wpf/
    public partial class AutoCompleteTextBox : UserControl
    {
        public static readonly RoutedEvent ClosedEvent =
            EventManager.RegisterRoutedEvent("Closed", RoutingStrategy.Bubble, typeof(ClosedEventHandler), typeof(AutoCompleteTextBox));
        public event ClosedEventHandler Closed
        {
            add { AddHandler(ClosedEvent, value); }
            remove { RemoveHandler(ClosedEvent, value); }
        }

        public event Func<string, IEnumerable> AutoCompleting;

        public static readonly DependencyProperty SelectedItemProperty =
            DependencyProperty.Register("SelectedItem", typeof(object), typeof(AutoCompleteTextBox), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, (s, o) => ((AutoCompleteTextBox)s).txtBox.Text = o.NewValue.TryToString()));
        public object SelectedItem
        {
            get { return (object)GetValue(SelectedItemProperty); }
            set { SetValue(SelectedItemProperty, value); }
        }

        public static readonly DependencyProperty MinTypedCharactersProperty =
            DependencyProperty.Register("MinTypedCharacters", typeof(int), typeof(AutoCompleteTextBox), new UIPropertyMetadata(1));
        public int MinTypedCharacters
        {
            get { return (int)GetValue(MinTypedCharactersProperty); }
            set { SetValue(MinTypedCharactersProperty, value); }
        }


        public static readonly DependencyProperty AllowFreeTextProperty =
            DependencyProperty.Register("AllowFreeText", typeof(bool), typeof(AutoCompleteTextBox), new UIPropertyMetadata(false));
        public bool AllowFreeText
        {
            get { return (bool)GetValue(AllowFreeTextProperty); }
            set { SetValue(AllowFreeTextProperty, value); }
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

        public void delayTimer_Tick(object sender, EventArgs e)
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
                else if (lstBox.Items.Count == 1)
                {
                    MoveDown();
                    Commit(CloseReason.Tab);
                }
                else
                    Close(CloseReason.TabExit);
                
                //e.Handled = true;
            }
            else if (e.Key == Key.Enter)
            {
                if(lstBox.SelectedItem != null)
                    Commit(CloseReason.Enter);
                else if (lstBox.Items.Count == 1)
                {
                    MoveDown();
                    Commit(CloseReason.Enter);
                }
                
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
                if (Key.NumPad0 <= key && key <= Key.NumPad9)
                    key += ((int)Key.D0 - (int)Key.NumPad0);

                KeyConverter conv = new KeyConverter();
                string keyString = (string)conv.ConvertTo(key, typeof(string));

                return keyString.Length == 1;
            }
        }

        public void Close(CloseReason reason)
        {
            pop.IsOpen = false;
            if (SelectedItem.TryToString() != txtBox.Text)
            {
                if (string.IsNullOrEmpty(txtBox.Text))
                    SelectedItem = null;
                else if (AllowFreeText)
                    SelectedItem = txtBox.Text;
            }
            RaiseEvent(new CloseEventArgs(reason));
        }


        void Commit(CloseReason reason)
        {
            pop.IsOpen = false;
            if (lstBox.SelectedItem != null)
            {
                SelectedItem = lstBox.SelectedItem;

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
            {
                if (string.IsNullOrEmpty(this.txtBox.Text))
                    SelectedItem = null;

                Close(CloseReason.LostFocus);
            }
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
            else if (pop.IsMouseOver)
            {
                ReleaseMouseCapture();
                Commit(CloseReason.ClickList);
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

        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new AutoCompleteAutomationPeer(this);
        }

    }

    public class AutoCompleteAutomationPeer : UserControlAutomationPeer, IValueProvider
    {
        public AutoCompleteAutomationPeer(AutoCompleteTextBox ac)
            : base(ac)
        {
        }

        protected override List<AutomationPeer> GetChildrenCore()
        {
            List<AutomationPeer> childrenCore = new List<AutomationPeer>();
            AutoCompleteTextBox owner = (AutoCompleteTextBox)base.Owner;
            if (owner.pop.IsOpen)
            {
                AutomationPeer item = UIElementAutomationPeer.CreatePeerForElement(owner.lstBox);
                if (item == null)
                    return childrenCore;

                childrenCore.Add(item);
            }
            return childrenCore;
        }

        public override object GetPattern(PatternInterface patternInterface)
        {
            if (patternInterface == PatternInterface.Value)
                return this;

            return base.GetPattern(patternInterface);
        }

        public bool IsReadOnly
        {
            get { return !((AutoCompleteTextBox)base.Owner).IsEnabled; }
        }

        public void SetValue(string value)
        {
            AutoCompleteTextBox ac = ((AutoCompleteTextBox)base.Owner);
            ac.Text = value;
            ac.delayTimer_Tick(null, null);
            if (ac.AllowFreeText && ac.lstBox.Items.Count == 0)
                ac.Close(CloseReason.LostFocus);
        }

        public string Value
        {
            get { return ((AutoCompleteTextBox)base.Owner).Text; }
        }
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
