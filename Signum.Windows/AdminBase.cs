using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using Signum.Entities;
using Signum.Utilities.DataStructures;
using System.Collections.ObjectModel;
using Signum.Utilities;
using System.Windows.Controls;
using System.ComponentModel;
using Signum.Entities.Reflection;

namespace Signum.Windows
{
    public class AdminBase:Window
    {
        public virtual List<IdentifiableEntity> GetEntities() 
        {
            throw new NotImplementedException();
        }

        public virtual void SetEntities(List<IdentifiableEntity> value)
        {
            throw new NotImplementedException();
        }

        public virtual void RetrieveEntities()
        {
            throw new NotImplementedException();
        }
        
        /// <summary>
        /// Could be called Async
        /// </summary>
        public virtual List<IdentifiableEntity> SaveEntities(List<IdentifiableEntity> value)
        {
            throw new NotImplementedException();
        }

        public virtual void UpdateInterface()
        {
            throw new NotImplementedException();
        }

        public virtual Button GetSaveButton()
        {
            throw new NotImplementedException();
        }

        public void Retrieve()
        {
            RetrieveEntities();
            UpdateInterface();
        }

        public virtual bool HasChanges(DirectedGraph<Modifiable> graph)
        {
            return graph.Any(a => a.SelfModified);
        }

        public DirectedGraph<Modifiable> Graph()
        {
            return DirectedGraph<Modifiable>.Union(GetEntities().Select(e => GraphExplorer.FromRoot(e)));
        }

        protected void Reload_Click(object sender, RoutedEventArgs e)
        {
            if (!HasChanges(Graph()) || MessageBox.Show(Properties.Resources.ThereAreChangesContinue, Properties.Resources.ThereAreChanges,
                MessageBoxButton.OKCancel, MessageBoxImage.Question, MessageBoxResult.OK) == MessageBoxResult.OK)
            {
                Retrieve();
            }
        }

        public bool AssertErrors(DirectedGraph<Modifiable> graph)
        {
            GraphExplorer.PreSaving(graph);
            string error = GraphExplorer.Integrity(graph);

            if (error.HasText())
            {
                MessageBox.Show(Properties.Resources.ImpossibleToSaveIntegrityCheckFailed + error, Properties.Resources.ThereAreErrors, MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            return true;
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            var graph = Graph();
            if (HasChanges(graph))
            {
                var result = MessageBox.Show(Properties.Resources.SaveChanges, Properties.Resources.ThereAreChanges,
                    MessageBoxButton.YesNoCancel, MessageBoxImage.Question, MessageBoxResult.No);

                if (result == MessageBoxResult.Cancel)
                {
                    e.Cancel = true;
                }
                else if (result == MessageBoxResult.Yes)
                {
                    if (AssertErrors(graph))
                    {
                        SetEntities(SaveEntities(GetEntities()));
                        UpdateInterface();
                    }
                    else
                        e.Cancel = true;
                }
                else if (result == MessageBoxResult.No)
                {
                    return; 
                }
            }

            base.OnClosing(e);
        }

        protected void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        protected void Save_Click(object sender, RoutedEventArgs e)
        {
            var graph = Graph();
            if (!HasChanges(graph))
            {
                MessageBox.Show(Properties.Resources.NoChangeHaveBeenFound, Properties.Resources.NoChanges, MessageBoxButton.OK, MessageBoxImage.Hand );
                return;
            }

            if (!AssertErrors(graph))
                return;

            List<IdentifiableEntity> value = GetEntities();
            Button button = GetSaveButton();

            button.IsEnabled = false;
            Async.Do(this,
               () => value = SaveEntities(value),
               () => { SetEntities(value); UpdateInterface(); },
               () => button.IsEnabled = true);
        }
    }
}
