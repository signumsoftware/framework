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

namespace WizardProjectName
{
    public class Wizard : IWizard
    {

        public void BeforeOpeningFile(ProjectItem projectItem)
        {

        }

        public void ProjectFinishedGenerating(Project project)
        {
            try
            {
                var serviceProvider = new ServiceProvider(project.DTE as Microsoft.VisualStudio.OLE.Interop.IServiceProvider); 
                var solution = serviceProvider.GetService(typeof(SVsSolution)) as IVsSolution;

                IVsHierarchy hierarchy;
                 
                solution.GetProjectOfUniqueName(project.FullName, out hierarchy);

                Guid projectGuid = Guid.Empty;
                if (hierarchy != null)
                {
                    ErrorHandler.ThrowOnFailure(
                        hierarchy.GetGuidProperty(
                            VSConstants.VSITEMID_ROOT,
                            (int)__VSHPROPID.VSHPROPID_ProjectIDGuid,
                            out projectGuid));
                }

                string post = postNames.SingleOrDefault(a => project.Name.EndsWith(a));
                
                string post2 = post.Replace(".", "guid");

                projectGuids[post2] = projectGuid.ToString(); 

                //StringBuilder sb = new StringBuilder();
                //sb.AppendLine(string.Format("{0}: {1}", post2, projectGuid.ToString()));

                //MessageBox.Show(sb.ToString());
                //File.AppendAllText(@"c:\debugTemplate.txt", sb.ToString());
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        public void ProjectItemFinishedGenerating(ProjectItem projectItem)
        {

        }

        public void RunFinished()
        {

        }

        static Dictionary<string, string> projectGuids = new Dictionary<string,string>(); 

        static string[] postNames = new[] { ".Entities", ".Load", ".Logic", ".Web", ".Windows" };

        public void RunStarted(object automationObject, Dictionary<string, string> replacementsDictionary, WizardRunKind runKind, object[] customParams)
        {
            try
            {
                string projectName = replacementsDictionary["$safeprojectname$"];

                string post = postNames.SingleOrDefault(a => projectName.EndsWith(a));
                if (post != null)
                    projectName = projectName.Remove(projectName.Length - post.Length);

                replacementsDictionary.Add("$custommessage$", projectName);


                foreach (var kvp in projectGuids)
                    replacementsDictionary.Add("$" + kvp.Key + "$", kvp.Value);

                //StringBuilder sb = new StringBuilder();
                //foreach (var kvp in replacementsDictionary)
                //    sb.AppendFormat("{0}: {1}\r\n", kvp.Key, kvp.Value); 

                //MessageBox.Show(sb.ToString());
                //File.AppendAllText(@"c:\debugTemplate.txt", sb.ToString());

            }

            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        public bool ShouldAddProjectItem(string filePath)
        {
            return true;
        }
    }
}
