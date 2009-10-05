using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Collections;
using Signum.Utilities.Reflection;

namespace Signum.Windows
{
    public class EntityListBase : EntityBase
    {
        public static readonly DependencyProperty EntitiesProperty =
          DependencyProperty.Register("Entities", typeof(IList), typeof(EntityListBase), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, (d, e) => ((EntityListBase)d).EntitiesChanged(e)));
        public IList Entities
        {
            get { return (IList)GetValue(EntitiesProperty); }
            set { SetValue(EntitiesProperty, value); }
        }

        public static readonly DependencyProperty EntitiesTypeProperty =
          DependencyProperty.Register("EntitiesType", typeof(Type), typeof(EntityListBase), new UIPropertyMetadata((d, e) => ((EntityListBase)d).Type = ReflectionTools.CollectionType((Type)e.NewValue)));
        public Type EntitiesType
        {
            get { return (Type)GetValue(EntitiesTypeProperty); }
            set { SetValue(EntitiesTypeProperty, value); }
        }

        protected internal override DependencyProperty CommonRouteValue()
        {
            return EntitiesProperty;
        }

        protected internal override DependencyProperty CommonRouteType()
        {
            return EntitiesTypeProperty;
        }

        protected override bool CanFind()
        {
            return Find && !Common.GetIsReadOnly(this);
        }

        protected override bool CanCreate()
        {
            return Create && !Common.GetIsReadOnly(this);
        }


        public IList EnsureEntities()
        {
            if (Entities == null)
                Entities = (IList)Activator.CreateInstance(EntitiesType);
            return Entities;
        }

        public virtual void EntitiesChanged(DependencyPropertyChangedEventArgs e)
        {

        }
    }
}
