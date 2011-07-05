using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TemplateWizard;
using EnvDTE;
using System.Windows.Forms;
using System.IO;
using System.Xml;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio;
using System.Reflection;

namespace WizardProjectName
{
    public class ControlWizard : IWizard
    {

        public void BeforeOpeningFile(ProjectItem projectItem)
        {

        }

        public void ProjectFinishedGenerating(Project project)
        {
         
        }

        public void ProjectItemFinishedGenerating(ProjectItem projectItem)
        {
          
        }

        public void RunFinished()
        {

        }


        public void RunStarted(object automationObject, Dictionary<string, string> replacementsDictionary, WizardRunKind runKind, object[] customParams)
        {
            try
            {

                using (TypeDialog dialog = new TypeDialog())
                {
                    dialog.FileImputName = replacementsDictionary["$rootname$"];

                    if (dialog.ShowDialog() == DialogResult.Cancel)
                    {
                        createFile = false;
                        return;
                    }
                    else
                    {
                        createFile = true;
                    }

           
                    replacementsDictionary["$newname$"] = dialog.FileImputName;
                    replacementsDictionary["$entityfields$"] = dialog.WPFEntityControl;
                    replacementsDictionary["$entityname$"] = dialog.TypeName;
                    replacementsDictionary["$entitynamespace$"] = dialog.TypeNamespace;
                    replacementsDictionary["$entityassembly$"] = dialog.AssemblyName;

                    foreach (var item in replacementsDictionary)
                    {
                        File.AppendAllText(@"c:\debugTemplate.txt", string.Format("{0}->{1}\r\n", item.Key, item.Value));
                    }
                }
            }

            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString() +"\r\n" + ex.StackTrace);
                File.AppendAllText(@"c:\debugTemplate.txt", ex.Message);
            }
        }

        bool createFile; 

        public bool ShouldAddProjectItem(string filePath)
        {
            return createFile;
        }

    
    }
}
