using System;
using System.IO;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Navigation;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.ComponentModel;
using Signum.Utilities;
using System.Linq;
using Signum.Utilities.DataStructures;
using Signum.Entities;
using System.Windows.Input;
using System.Windows.Automation.Peers;

namespace Signum.Windows
{
	public partial class ErrorSummary:INotifyPropertyChanged
	{
        public event PropertyChangedEventHandler PropertyChanged;

        private List<ValidationError> bindingExceptions = new List<ValidationError>();
        public List<ValidationError> BindingExceptions
        {
            get { return bindingExceptions;  }
        }

        public static readonly DependencyProperty HasErrorsProperty =
            DependencyProperty.Register("HasErrors", typeof(bool), typeof(ErrorSummary), new UIPropertyMetadata(false));
        public bool HasErrors
        {
            get { return (bool)GetValue(HasErrorsProperty); }
            set { SetValue(HasErrorsProperty, value); }
        }


        public static readonly DependencyProperty LetErrorsBubbleProperty =
            DependencyProperty.Register("LetErrorsBubble", typeof(bool), typeof(ErrorSummary), new UIPropertyMetadata(false));
        public bool LetErrorsBubble
        {
            get { return (bool)GetValue(LetErrorsBubbleProperty); }
            set { SetValue(LetErrorsBubbleProperty, value); }
        }

		public ErrorSummary()
		{
			this.InitializeComponent();
            this.Loaded += new RoutedEventHandler(ErrorSummary_Loaded);
		}

      

        void ErrorSummary_Loaded(object sender, RoutedEventArgs e)
        {
            if (DesignerProperties.GetIsInDesignMode(this))
                return;

            var parent = (FrameworkElement)this.Parent;

            parent.AddHandler(Validation.ErrorEvent, new EventHandler<ValidationErrorEventArgs>(ErrorHandler));
            var multi = new MultiBinding { Converter = DoubleListConverter.Instance };
            multi.Bindings.Add(new Binding("BindingExceptions") { Source = this });
            multi.Bindings.Add(new Binding("DataContext.Error") { Source = parent });
            lb.SetBinding(ItemsControl.ItemsSourceProperty, multi);
            
            this.SetBinding(HasErrorsProperty, new Binding("ItemsSource") {  Source = lb, Converter = Converters.ErrorListToBool });
        }

        void ErrorHandler(object sender, ValidationErrorEventArgs args)
        {
            if (!(args.Error.RuleInError is DataErrorValidationRule))
            {
                if (args.Action == ValidationErrorEventAction.Added)
                    bindingExceptions.Add(args.Error);
                else
                    bindingExceptions.Remove(args.Error);
            }

            if (!LetErrorsBubble)
                args.Handled = true; 

            OnBindingExceptionChanged();
        }

        void OnBindingExceptionChanged()
        {
            if (this.PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs("BindingExceptions"));
        }

        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new ErrorSummaryAutomationPeer(this);
        }
    }

    class ErrorSummaryAutomationPeer : UserControlAutomationPeer
    {
        public ErrorSummaryAutomationPeer(ErrorSummary summary)
            : base(summary)
        {
        }

        protected override string GetHelpTextCore()
        {
            var es = ((ErrorSummary)Owner);

            var exceptions = es.BindingExceptions.ToString(a => a.Exception.Message, "\r\n");

            if (es.DataContext is IDataErrorInfo dei)
                return "\r\n".Combine(exceptions, dei.Error);

            return exceptions;
        }
    }
}