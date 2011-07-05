using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Reflection;
using System.IO;

namespace WizardProjectName
{
    public partial class TypeDialog : Form, IDisposable
    {
        AppDomain realDomain;
        MyLoader myLoader;     

        public TypeDialog()
        {
            InitializeComponent();

            realDomain = AppDomain.CreateDomain("RealDomain", null, new AppDomainSetup { ApplicationBase = AppDomain.CurrentDomain.BaseDirectory });

            myLoader = (MyLoader)realDomain.CreateInstanceAndUnwrap(typeof(MyLoader).Assembly.FullName, typeof(MyLoader).FullName);
        }

        public string WPFEntityControl { get; set; }
        
        public string AssemblyName { get; set; }
        public string TypeFullName { get; set; }
        public string TypeName { get; set; }
        public string TypeNamespace { get; set; }

        public string FileImputName { get; set; }

        string fileName = "LastAssemblies.txt";

        private void TypeDialog_Load(object sender, EventArgs e)
        {
            try
            {
                if (File.Exists(fileName))
                {
                    string[] assemblies = File.ReadAllLines(fileName);
                    foreach (var item in assemblies)
                    {
                        cbAssembly.Items.Add(item);
                    }

                    cbAssembly.SelectedItem = assemblies.FirstOrDefault(); 
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message, "Error reading assemblies file", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void cbAssembly_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                cbType.Items.Clear();
                if (cbAssembly.SelectedItem != null)
                { 
                    string path = (string)cbAssembly.SelectedItem;

                    myLoader.LoadAndSetAssembly(path);
                    AssemblyName = myLoader.GetAssemblyName(); 
                    string[] types = myLoader.CompatibleTypeNames();

                    foreach (var item in types)
                    {
                        cbType.Items.Add(item); 
                    }

                    string type = FileImputName == null ? types.FirstOrDefault() :
                                                       MostSimilar(types, t => t, FileImputName);
                    cbType.SelectedItem = type;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error Opening the Assembly", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public static T MostSimilar<T>(IEnumerable<T> collection, Func<T, string> stringSelector, string pattern)
        {
            StringDistance sd = new StringDistance();
            return WithMin(collection, item => sd.Distance(stringSelector(item), pattern));
        }

        public static T WithMin<T, V>(IEnumerable<T> collection, Func<T, V> valueSelector)
            where V : IComparable<V>
        {
            T result = default(T);
            bool hasMin = false;
            V min = default(V);
            foreach (var item in collection)
            {
                V val = valueSelector(item);
                if (!hasMin || val.CompareTo(min) < 0)
                {
                    hasMin = true;
                    min = val;
                    result = item;
                }
            }

            return result;
        }

        private void btOpenAssembly_Click(object sender, EventArgs e)
        {
            try
            {
                OpenFileDialog ofd = new OpenFileDialog(); 
                ofd.Title = "Open Entity Assembly";  
                ofd.Filter = "Class Library (*.dll)|*.dll|Application (*.exe)|*.exe";
                ofd.FilterIndex = 0;
                if(ofd.ShowDialog() == DialogResult.OK)
                {
                    if (!cbAssembly.Items.Contains(ofd.FileName))
                        cbAssembly.Items.Remove(ofd.FileName);

                    cbAssembly.Items.Insert(0, ofd.FileName);
                    cbAssembly.SelectedItem = ofd.FileName;
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message, "Error Opening the Assembly", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        private void btOk_Click(object sender, EventArgs e)
        {
            if (TypeFullName == null)
            {
                MessageBox.Show("Choose a Type First");
                return;
            }

            try
            {

                string currentAssembly = (string)cbAssembly.SelectedItem;

                List<string> assemblies = File.Exists(fileName) ? File.ReadAllLines(fileName).ToList() : new List<string>();
                if (assemblies.Contains(currentAssembly))
                    assemblies.Remove(currentAssembly);

                assemblies.Insert(0, currentAssembly);

                File.WriteAllLines(fileName, assemblies.ToArray());

                string entityName = TypeFullName.Split('.').Last();
                if (entityName.EndsWith("DN"))
                    entityName =  entityName.Substring(0, entityName.Length -2);

                if (!FileImputName.StartsWith(entityName))
                {
                    if (FileImputName == "Entity" || MessageBox.Show(string.Format("Change item '{0}' to '{1}' to match {2}", FileImputName, entityName, TypeName),
                        "Change Name?", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation) == DialogResult.Yes)
                    {
                        FileImputName = entityName;
                    }
                }

                WPFEntityControl = myLoader.Render(TypeFullName); 

                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error writing assembly file", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }

        private void btCancel_Click(object sender, EventArgs e)
        {
            Close();
        }

        #region IDisposable Members

        void IDisposable.Dispose()
        {
            if (myLoader != null)
                myLoader = null; 

            if (realDomain != null)
                AppDomain.Unload(realDomain);

            base.Dispose();
        }

        #endregion

        private void cbType_SelectedIndexChanged(object sender, EventArgs e)
        {
            TypeFullName = (string)cbType.SelectedItem;
            TypeName = myLoader.GetTypeName(TypeFullName);
            TypeNamespace = myLoader.GetTypeNamespace(TypeFullName); 
        }
    }
}
