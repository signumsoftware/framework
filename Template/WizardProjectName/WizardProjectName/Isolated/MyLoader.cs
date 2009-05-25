using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.IO;

namespace WizardProjectName
{
    public class MyLoader: MarshalByRefObject
    {
        Assembly assembly;

        public MyLoader()
        {
            AppDomain.CurrentDomain.AssemblyResolve+=new ResolveEventHandler(CurrentDomain_AssemblyResolve);
        }

        Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            string path = Path.GetDirectoryName(assembly.Location);
            string name = Path.Combine(path, new AssemblyName(args.Name).Name);
            if(File.Exists(name))
                return Assembly.LoadFile(name); 
            else if (File.Exists(name + ".exe"))
                return Assembly.LoadFile(name + ".exe");
            else if (File.Exists(name + ".dll"))
                return Assembly.LoadFile(name + ".dll");

            return null;
        }

      
        public void LoadAndSetAssembly(string assemblyPath)
        {
            assembly= Assembly.LoadFile(assemblyPath); 
            Reflector.Set(assembly); 
        }

        public string GetAssemblyName()
        {
            return assembly.GetName().Name;
        }

        public string GetTypeNamespace(string typeName)
        {
            return assembly.GetType(typeName).Namespace;
        }

        public string GetTypeName(string typeName)
        {
            return assembly.GetType(typeName).Name;
        }

        public string[] CompatibleTypeNames()
        {
            return assembly.GetTypes().Where(t => Reflector.ModifiableEntity.IsAssignableFrom(t)).Select(a => a.FullName).OrderBy(a => a).ToArray();
        }

        public string Render(string typeName)
        {
            Type type = assembly.GetType(typeName);
            return WPFEntityControls.Render(type); 
        }
    }
}
