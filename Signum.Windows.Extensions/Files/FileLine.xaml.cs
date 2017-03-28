using System;
using System.Collections.Generic;
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
using System.Diagnostics;
using Signum.Utilities;
using Signum.Utilities.ExpressionTrees;
using Signum.Utilities.Reflection;
using System.Reflection;
using System.ComponentModel;
using Signum.Entities;
using System.IO;
using Microsoft.Win32;
using Signum.Entities.Basics;
using Signum.Entities.Reflection;
using Signum.Entities.Files;
using System.Net;
using Signum.Services;
using Signum.Windows.Extensions.Files;

namespace Signum.Windows.Files
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class FileLine : LineBase
    {
        public static readonly DependencyProperty EntityProperty =
            DependencyProperty.Register("Entity", typeof(object), typeof(FileLine), new FrameworkPropertyMetadata(null,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, (d, e) => ((FileLine)d).EntityChanged(e.OldValue, e.NewValue)));
        public object Entity
        {
            get { return (object)GetValue(EntityProperty); }
            set { SetValue(EntityProperty, value); }
        }

        public static readonly DependencyProperty EntityTemplateProperty =
           DependencyProperty.Register("EntityTemplate", typeof(DataTemplate), typeof(FileLine), new UIPropertyMetadata(null));
        public DataTemplate EntityTemplate
        {
            get { return (DataTemplate)GetValue(EntityTemplateProperty); }
            set { SetValue(EntityTemplateProperty, value); }
        }

        public static readonly DependencyProperty OpenProperty =
            DependencyProperty.Register("Open", typeof(bool), typeof(FileLine), new FrameworkPropertyMetadata(true, (d, e) => ((FileLine)d).UpdateVisibility()));
        public bool Open
        {
            get { return (bool)GetValue(OpenProperty); }
            set { SetValue(OpenProperty, value); }
        }

        public static readonly DependencyProperty ViewProperty =
            DependencyProperty.Register("View", typeof(bool), typeof(FileLine), new FrameworkPropertyMetadata(true, (d, e) => ((FileLine)d).UpdateVisibility()));
        public bool View
        {
            get { return (bool)GetValue(ViewProperty); }
            set { SetValue(ViewProperty, value); }
        }

        public static readonly DependencyProperty SaveProperty =
            DependencyProperty.Register("Save", typeof(bool), typeof(FileLine), new FrameworkPropertyMetadata(true, (d, e) => ((FileLine)d).UpdateVisibility()));
        public bool Save
        {
            get { return (bool)GetValue(SaveProperty); }
            set { SetValue(SaveProperty, value); }
        }

        public static readonly DependencyProperty RemoveProperty =
            DependencyProperty.Register("Remove", typeof(bool), typeof(FileLine), new FrameworkPropertyMetadata(true, (d, e) => ((FileLine)d).UpdateVisibility()));
        public bool Remove
        {
            get { return (bool)GetValue(RemoveProperty); }
            set { SetValue(RemoveProperty, value); }
        }

        public static readonly DependencyProperty DropFileProperty =
            DependencyProperty.Register("DropFile", typeof(bool), typeof(FileLine), new PropertyMetadata(true));
        public bool DropFile
        {
            get { return (bool)GetValue(DropFileProperty); }
            set { SetValue(DropFileProperty, value); }
        }

        static FileLine()
        {
            Common.ValuePropertySelector.SetDefinition(typeof(FileLine), EntityProperty);
        }

        public delegate void SaveFileDelegate(object entity);
        public delegate void ViewFileDelegate(object entity);
        public delegate object OpenFileDelegate();

        public event Func<IFile> Creating;
        public event Func<object> Opening;
        public event Action<object> Saving;
        public event Action<object> Viewing;
        public event Func<object, bool> Removing;

        public event Action<FileDialog> CustomizeFileDialog;

        Type cleanType;

        public FileLine()
        {
            InitializeComponent();

        }

        public override void OnLoad(object sender, RoutedEventArgs e)
        {
            if (DesignerProperties.GetIsInDesignMode(this))
                return;

            base.OnLoad(sender, e);

            if (this.NotSet(EntityTemplateProperty))
            {
                EntityTemplate = Navigator.FindDataTemplate(this, Type);
            }

            if (Type.IsLite())
            {
                cleanType = Lite.Extract(Type);
            }
            else
            {
                cleanType = Type;
            }

            UpdateVisibility();
        }


        protected virtual void EntityChanged(object oldValue, object newValue)
        {
            UpdateVisibility();
        }

        protected void UpdateVisibility()
        {
            btSave.Visibility = CanSave() ? Visibility.Visible : Visibility.Collapsed;
            btOpen.Visibility = CanOpen() ? Visibility.Visible : Visibility.Collapsed;
            btView.Visibility = CanView() ? Visibility.Visible : Visibility.Collapsed;
            btRemove.Visibility = CanRemove() ? Visibility.Visible : Visibility.Collapsed;
        }

        private bool CanRemove()
        {
            return Remove && Entity != null && !Common.GetIsReadOnly(this);
        }

        private bool CanView()
        {
            return View && Entity != null;
        }

        private bool CanOpen()
        {
            return Open && Entity == null && !Common.GetIsReadOnly(this);
        }

        private bool CanSave()
        {
            return Save && Entity != null;
        }

        private void OnViewing(object entity)
        {
            if (!CanView())
                return;

            if (Viewing != null)
            {
                Viewing(entity);
            }
            else if (typeof(IFile).IsAssignableFrom(cleanType))
            {
                IFile file = (IFile)Server.Convert(entity, cleanType);
                DefaultViewFile(file, OnResolveBinaryFile);
            }
            else
                throw new InvalidOperationException(FileMessage.ViewingHasNotDefaultImplementationFor0.NiceToString()
                    .FormatWith(Type));
        }

        public static void DefaultViewFile(IFile file, Func<IFile, byte[]> resolveBinaryFile = null)
        {
            if (resolveBinaryFile == null)
                resolveBinaryFile = DefaultResolveBinaryFile;

            string filePath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), file.FileName);
            File.WriteAllBytes(filePath, file.BinaryFile ?? resolveBinaryFile(file));
            Process.Start(filePath);
        }

        private void OnSaving(object entity)
        {
            if (!CanSave())
                return;

            if (Saving != null)
            {
                Saving(entity);
            }
            else if (typeof(IFile).IsAssignableFrom(cleanType))
            {
                IFile file = (IFile)Server.Convert(entity, cleanType);

                SaveFileDialog sfd = new SaveFileDialog()
                {
                    FileName = file.FileName
                };
                CustomizeFileDialog?.Invoke(sfd);

                if (sfd.ShowDialog() == true)
                    File.WriteAllBytes(sfd.FileName, file.BinaryFile ?? OnResolveBinaryFile(file));
            }
            else
                throw new NotSupportedException(FileMessage.SavingHasNotDefaultImplementationFor0.NiceToString().FormatWith(Type)); 
        }


        public Func<IFile, byte[]> ResolveBinaryFile;

        private byte[] OnResolveBinaryFile(IFile file)
        {
            if (ResolveBinaryFile != null)
                return ResolveBinaryFile(file);

            return DefaultResolveBinaryFile(file);
        }

        private object OnOpening()
        {
            if (!CanOpen())
                return null;

            if (Opening != null)
                return Opening();

            if (typeof(IFile).IsAssignableFrom(cleanType))
            {
                OpenFileDialog ofd = new OpenFileDialog();
                CustomizeFileDialog?.Invoke(ofd);

                if (ofd.ShowDialog() == true)
                {
                    return CreateFile(ofd.FileName);
                }

                return null;
            }

            throw new NotSupportedException(FileMessage.OpeningHasNotDefaultImplementationFor0.NiceToString().FormatWith(Type)); 
        }

        object CreateFile(string fileName)
        {
            IFile file = Creating != null ? Creating() :
                (IFile)Activator.CreateInstance(cleanType);
            file.FileName = System.IO.Path.GetFileName(fileName);
            file.BinaryFile = File.ReadAllBytes(fileName);

            return Server.Convert(file, Type);
        }

        protected bool OnRemoving(object entity)
        {
            if (!CanRemove())
                return false;

            if (Removing != null)
                return Removing(entity);

            return true;
        }

        private void btRemove_Click(object sender, RoutedEventArgs e)
        {   
            if (OnRemoving(Entity))
                Entity = null;
        }

        private void btView_Click(object sender, RoutedEventArgs e)
        {
            OnViewing(Entity);
        }

        private void btSave_Click(object sender, RoutedEventArgs e)
        {
            OnSaving(Entity);
        }

        private void btOpen_Click(object sender, RoutedEventArgs e)
        {
            object entity = OnOpening();

            if (entity != null)
                Entity = entity;
        }

        public static byte[] DefaultResolveBinaryFile(IFile f)
        {
            if (f.FullWebPath() != null)
            {
                return new WebClient().DownloadData(f.FullWebPath());
            }
            else
            {
                return Server.Return((IFileServer s) => s.GetBinaryFile(f));
            }
        }

        private void fileLine_DragEnter(object sender, DragEventArgs e)
        {
            if (!DropFile)
                return;

            if (!e.Data.GetDataPresent(DataFormats.FileDrop) && !e.CanHandleOutlookAttachment())
            {
                e.Effects = DragDropEffects.None;
                e.Handled = true;
            }
        }

        private void fileLine_Drop(object sender, DragEventArgs e)
        {
            if (!DropFile)
                return;

            string[] files = null;

            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                files = (string[])e.Data.GetData(DataFormats.FileDrop);

                if (files == null || files.IsEmpty())
                    return;

                if (files.Length != 1)
                    throw new ApplicationException(FileMessage.OnlyOneFileIsSupported.NiceToString());
            }
            else if (e.CanHandleOutlookAttachment())
            {
                var attachments = e.DropOutlookAttachment();

                if (attachments.Count != 1)
                    throw new ApplicationException(FileMessage.OnlyOneFileIsSupported.NiceToString());

                var fileContent = attachments.SingleEx();

                int i = 0;
                var tPath = System.IO.Path.GetTempPath();
                while (File.Exists(System.IO.Path.Combine(tPath, fileContent.FileName)))
                {
                    tPath = System.IO.Path.Combine(tPath, i.ToString());
                    if (!Directory.Exists(tPath))
                        Directory.CreateDirectory(tPath);
                    i++;
                }
                string fileName = System.IO.Path.Combine(tPath, fileContent.FileName);
                File.WriteAllBytes(fileName, fileContent.Bytes);

                files = new[] { fileName };
            }
            else
            {
                return;
            }

            object file = CreateFile(files.Single());

            if (file != null)
                this.Entity = file;
        }
    }
}
