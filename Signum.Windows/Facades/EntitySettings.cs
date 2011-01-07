using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows;
using System.ComponentModel;
using System.Windows.Media;
using System.Windows.Data;
using System.Collections;
using Signum.Utilities.DataStructures;
using Signum.Entities;

namespace Signum.Windows
{
    public abstract class EntitySettings
    {
        public abstract Type StaticType { get; }

        public abstract Control CreateView(ModifiableEntity entity, PropertyRoute typeContext);

        public abstract bool OnIsReadOnly(ModifiableEntity entity, bool isAdmin);
        public abstract bool OnIsViewable(ModifiableEntity entity, bool isAdmin);
        public abstract bool OnIsCreable(bool isAdmin);
        public abstract bool OnShowSave(); 

        public DataTemplate DataTemplate { get; set; }
        public ImageSource Icon { get; set; }

        public Action<bool, ICollectionView> CollectionViewOperations { get; set; }

    }

    public class EntitySettings<T> : EntitySettings where T:IdentifiableEntity
    {
        public override Type StaticType
        {
            get { return typeof(T); }
        }

        public Func<T, Control> View { get; set; }

        public Func<bool, bool> IsCreable { get; set; }
        public Func<T, bool, bool> IsReadOnly { get; set; }
        public Func<T, bool, bool> IsViewable { get; set; }      

        public bool ShowSave { get; set; }

        public override bool OnShowSave()
        {
            return ShowSave;
        }
        
        public EntitySettings(EntityType entityType)
        {
            switch (entityType)
            {
                case EntityType.Default:
                    ShowSave = true;
                    break;
                case EntityType.Admin:
                    ShowSave = true;
                    IsReadOnly = (_, admin) => !admin;
                    IsCreable = admin => admin;
                    IsViewable = (_, admin) => admin;
                    CollectionViewOperations = (isLite, cv) =>
                    {
                        ListCollectionView lcv = cv as ListCollectionView;
                        if (lcv != null)
                            lcv.CustomSort = isLite ?
                                (IComparer)new LambdaComparer<Lite, int>(la => la.IdOrNull ?? int.MaxValue) :
                                (IComparer)new LambdaComparer<IdentifiableEntity, int>(ie => ie.IdOrNull ?? int.MaxValue);
                        else
                            cv.SortDescriptions.Add(new SortDescription("IdOrNull", ListSortDirection.Ascending));
                    };
                    break;
                case EntityType.NotSaving:
                    ShowSave = false; 
                    break;
                case EntityType.ServerOnly:
                    ShowSave = false;
                    IsReadOnly = (_, admin) => true;
                    IsCreable = admin => false;
                    break;
                case EntityType.Content:
                    ShowSave = false;
                    IsCreable = admin => false;
                    IsViewable = (_, admin) => false;
                    break;
                default:
                    break;
            }
        }

        public override Control CreateView(ModifiableEntity entity, PropertyRoute typeContext)
        {
            if (View == null)
                throw new InvalidOperationException("View not defined in EntitySettings");

            return View((T)entity);
        }

        public void OverrideView(Func<IdentifiableEntity, Control, Control> overrideView)
        {
            var view = View;
            View = e => overrideView(e, view(e));
        }

        public override bool OnIsReadOnly(ModifiableEntity entity, bool isAdmin)
        {
            if(IsReadOnly != null)
                foreach (Func<T, bool, bool> isReadOnly in IsReadOnly.GetInvocationList())
                {
                    if (isReadOnly((T)entity, isAdmin))
                        return true;
                }

            return false;
        }

        public override bool OnIsViewable(ModifiableEntity entity, bool isAdmin)
        {
            if (IsViewable != null)
                foreach (Func<T, bool, bool> isViewable in IsViewable.GetInvocationList())
                {
                    if (!isViewable((T)entity, isAdmin))
                        return false;
                }

            return true;
        }

        public override bool OnIsCreable(bool isAdmin)
        {
            if (IsCreable != null)
                foreach (Func<bool, bool> isCreable in IsCreable.GetInvocationList())
                {
                    if (!isCreable(isAdmin))
                        return false;
                }

            return true;
        }
    }


    public enum EntityType
    {
        Admin,
        Default,
        NotSaving,
        ServerOnly,
        Content,
    }

    public class EmbeddedEntitySettings<T> : EntitySettings where T : EmbeddedEntity
    {
        public override Type StaticType
        {
            get { return typeof(T); }
        }

        public Func<T, PropertyRoute, Control> View { get; set; }

        public Func<Type, bool> IsCreable { set { IsCreableEvent += value; } }
        public Func<T, bool> IsReadOnly { set { IsReadOnlyEvent += value; } }
        public Func<T, bool> IsViewable { set { IsViewableEvent += value; } }

        public event Func<Type, bool> IsCreableEvent;
        public event Func<T, bool> IsReadOnlyEvent;
        public event Func<T, bool> IsViewableEvent;

        public override bool OnShowSave()
        {
            return false;
        }

        public override Control CreateView(ModifiableEntity entity, PropertyRoute typeContext)
        {
            if (View == null)
                throw new InvalidOperationException("View not defined in EntitySettings");

            if (typeContext == null)
                throw new ArgumentException("An EmbeddedEntity neeed TypeContext");

            return View((T)entity, typeContext);
        }

        public void OverrideView(Func<EmbeddedEntity, PropertyRoute, Control, Control> overrideView)
        {
            var viewEmbedded = View;
            View = (e, tc) => overrideView(e, tc, viewEmbedded(e, tc));
        }

        public override bool OnIsReadOnly(ModifiableEntity entity, bool isAdmin)
        {
            if (IsReadOnlyEvent != null)
                foreach (Func<T, bool> isReadOnly in IsReadOnlyEvent.GetInvocationList())
                {
                    if (isReadOnly((T)entity))
                        return true;
                }

            return false;
        }

        public override bool OnIsViewable(ModifiableEntity entity, bool isAdmin)
        {
            if (IsViewableEvent != null)
                foreach (Func<T, bool> isViewable in IsViewableEvent.GetInvocationList())
                {
                    if (!isViewable((T)entity))
                        return false;
                }

            return true;
        }

        public override bool OnIsCreable(bool isAdmin)
        {
            if (IsCreableEvent != null)
                foreach (Func<Type, bool> isCreable in IsCreableEvent.GetInvocationList())
                {
                    if (!isCreable(typeof(T)))
                        return false;
                }

            return true;
        }
    }

    public enum WindowsType
    {
        View,
        Find,
        Admin
    }

}
