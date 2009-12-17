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
    public class EntitySettings
    {
        public Func<IdentifiableEntity, Control> View { get; set; }
        public Func<EmbeddedEntity, TypeContext, Control> ViewEmbedded { get; set; }

        public Func<bool, bool> IsCreable { get; set; }
        public Func<bool, bool> IsReadOnly { get; set; }
        public Func<bool, bool> IsViewable { get; set; }
        public bool ShowSave { get; set; }

        public Action<bool, ICollectionView> CollectionViewOperations { get; set; }
        public DataTemplate DataTemplate { get; set; }
        public ImageSource Icon { get; set; }

        //public EntitySettings()
        //    : this(EntityType.Default)
        //{
        //}

        public EntitySettings(EntityType entityType)
        {
            switch (entityType)
            {
                case EntityType.Default:
                    ShowSave = true;
                    break;
                case EntityType.Admin:
                    ShowSave = true;
                    IsReadOnly = admin => !admin;
                    IsCreable = admin => admin;
                    IsViewable = admin => admin;
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
                    IsReadOnly = admin => true;
                    IsCreable = admin => false;
                    break;
                case EntityType.Content:
                    ShowSave = false; 
                    IsCreable = admin => false;
                    IsViewable = admin => false;
                    break;
                default:
                    break;
            }
        }

        public Control CreateView(ModifiableEntity entity, TypeContext typeContext)
        {
            if (entity is EmbeddedEntity)
            {
                if (ViewEmbedded == null)
                    throw new InvalidOperationException("View not defined in EntitySettings");

                if (typeContext == null)
                    throw new ArgumentException("An EmbeddedEntity needs TypeContext");

                return ViewEmbedded((EmbeddedEntity)entity, typeContext);
            }
            else //entity could be null
            {
                if (View == null)
                    throw new InvalidOperationException("View not defined in EntitySettings");

                return View((IdentifiableEntity)entity);
            }
        }

        public void OverrideView(Func<IdentifiableEntity, Control, Control> overrideView)
        {
            var view = View;
            View = e => overrideView(e, view(e));
        }

        public void OverrideViewEmbedded(Func<EmbeddedEntity, TypeContext, Control, Control> overrideView)
        {
            var viewEmbedded = ViewEmbedded;
            ViewEmbedded = (e, tc) => overrideView(e, tc, viewEmbedded(e, tc));
        }
    }

    public enum WindowsType
    {
        View,
        Find,
        Admin
    }

    public enum EntityType
    {
        Admin,
        Default,
        NotSaving,
        ServerOnly,
        Content,
    }
}
