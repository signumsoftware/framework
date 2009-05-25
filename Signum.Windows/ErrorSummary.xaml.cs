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


        public static readonly DependencyProperty ValidationTargetProperty =
            DependencyProperty.Register("ValidationTarget", typeof(UIElement), typeof(ErrorSummary), new UIPropertyMetadata(null));
        public UIElement ValidationTarget
        {
            get { return (UIElement)GetValue(ValidationTargetProperty); }
            set { SetValue(ValidationTargetProperty, value); }
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
            this.expander.IsEnabledChanged += new DependencyPropertyChangedEventHandler(expander_IsEnabledChanged);
		}

        void expander_IsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
        }

        void ErrorSummary_Loaded(object sender, RoutedEventArgs e)
        {
            if (DesignerProperties.GetIsInDesignMode(this))
                return;

            if (this.NotSet(ValidationTargetProperty))
                ValidationTarget = (UIElement)this.Parent;
            ValidationTarget.AddHandler(Validation.ErrorEvent, new EventHandler<ValidationErrorEventArgs>(ErrorHandler));
            var multi = new MultiBinding { Converter = DoubleListConverter.Instance };
            multi.Bindings.Add(new Binding("BindingExceptions") { Source = this });
            multi.Bindings.Add(new Binding("DataContext.Error") { Source = ValidationTarget });
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
    }
}