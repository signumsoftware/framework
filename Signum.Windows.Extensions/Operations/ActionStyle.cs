using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows;
using Signum.Entities.Operations;
using Signum.Utilities;

namespace Signum.Windows.Operations
{
    public class ActionButtonStyle
    {
        public string Text { get; set; }
        public ImageSource Image { get; set; }
        public Color? Color { get; set; }

        public static ToolBarButton CreateButton(ResourceDictionary dictionary, ActionInfo actionInfo)
        {
            Enum key = actionInfo.ActionKey;

            ToolBarButton button =  new ToolBarButton
            {
                IsEnabled = actionInfo.CanExecute,
                Content = EnumExtensions.NiceToString((object)key),
                Tag = actionInfo,
            };

            if (dictionary.Contains(key))
            {
                ActionButtonStyle style = (ActionButtonStyle)dictionary[key];

                if(style.Text != null)
                    button.Content = style.Text;

                if (style.Image != null)
                    button.Image = style.Image;

                if (style.Color == null)
                    button.Background = new SolidColorBrush(style.Color.Value); 
            }

            return button;
        }
    }
}
