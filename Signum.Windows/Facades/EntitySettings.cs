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
        public Func<Control> View { get; set; }
        public Func<Window> ViewWindow { get; set; }

        public Func<bool, bool> IsCreable { get; set; }
        public Func<bool, bool> IsReadOnly { get; set; }
        public Func<bool, bool> IsViewable { get; set; }
        public Func<bool, bool> ShowOkSave { get; set; }

        public Action<bool, ICollectionView> CollectionViewOperations { get; set; }
        public DataTemplate DataTemplate { get; set; }
        public Func<ImageSource> Icon { get; set; }

        static ImageSource DefaultFind = ImageLoader.GetImageSortName("find.png");
        static ImageSource DefaultAdmin = ImageLoader.GetImageSortName("admin.png");
        static ImageSource DefaultIcon = ImageLoader.GetImageSortName("entity.png");

        public EntitySettings(bool isAdmin)
        {
            if (isAdmin)
            {
                IsReadOnly = admin => !admin;
                IsCreable = admin => admin;
                IsViewable = admin => admin;
                CollectionViewOperations = (isLazy,cv) =>
                {
                    ListCollectionView lcv = cv as ListCollectionView;
                    if (lcv != null)
                        lcv.CustomSort = isLazy ?
                            (IComparer)new LambdaComparer<Lazy, int>(la => la.IdOrNull ?? int.MaxValue) :
                            (IComparer)new LambdaComparer<IdentifiableEntity, int>(ie => ie.IdOrNull ?? int.MaxValue);
                    else
                        cv.SortDescriptions.Add(new SortDescription("IdOrNull", ListSortDirection.Ascending));
                };
            }
            else
            {
                IsReadOnly = admin => false;
                IsCreable = admin => true;
                IsViewable = admin => true;
            }
        }


        internal static ImageSource GetIcon(EntitySettings es, WindowsType wt)
        {
            switch (wt)
            {
                case WindowsType.View:
                    return es != null && es.Icon != null ? es.Icon() : DefaultIcon;
                case WindowsType.Find:
                    return DefaultFind;
                case WindowsType.Admin:
                    return DefaultAdmin;
                default:
                    throw new InvalidOperationException();
            }
        }
    }

    public enum WindowsType
    {
        View,
        Find,
        Admin
    }
}
