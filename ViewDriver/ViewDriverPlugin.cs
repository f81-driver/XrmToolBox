using Formula81.XrmToolBox.Shared.Properietary;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Reflection;
using XrmToolBox.Extensibility;
using XrmToolBox.Extensibility.Interfaces;

namespace Formula81.XrmToolBox.Tools.ViewDriver
{
    [Export(typeof(IXrmToolBoxPlugin)),
        ExportMetadata("Name", "View Driver"),
        ExportMetadata("Description", "Tightens your grip on Dataverse views."),
        // Please specify the base64 content of a 32x32 pixels image
        ExportMetadata("SmallImageBase64", "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"),
        // Please specify the base64 content of a 80x80 pixels image
        ExportMetadata("BigImageBase64", "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"),
        ExportMetadata("BackgroundColor", "#FF8000"),
        ExportMetadata("PrimaryFontColor", "White"),
        ExportMetadata("SecondaryFontColor", "Black")]
    public class ViewDriverPlugin : PluginBase, IPayPalPlugin
    {
        public string DonationDescription => Informations.PayPalDonationDescription;
        public string EmailAccount => Informations.PayPalEmailAccount;
    
        public override IXrmToolBoxPluginControl GetControl()
        {
            return new ViewDriverPluginControl();
        }

        /// <summary>
        /// Constructor 
        /// </summary>
        public ViewDriverPlugin()
        {
            // If you have external assemblies that you need to load, uncomment the following to 
            // hook into the event that will fire when an Assembly fails to resolve
            // AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(AssemblyResolveEventHandler);
        }

        /// <summary>
        /// Event fired by CLR when an assembly reference fails to load
        /// Assumes that related assemblies will be loaded from a subfolder named the same as the Plugin
        /// For example, a folder named Sample.XrmToolBox.MyPlugin 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        private Assembly AssemblyResolveEventHandler(object sender, ResolveEventArgs args)
        {
            Assembly loadAssembly = null;
            Assembly currAssembly = Assembly.GetExecutingAssembly();

            // base name of the assembly that failed to resolve
            var argName = args.Name.Substring(0, args.Name.IndexOf(","));

            // check to see if the failing assembly is one that we reference.
            List<AssemblyName> refAssemblies = currAssembly.GetReferencedAssemblies().ToList();
            var refAssembly = refAssemblies.Where(a => a.Name == argName).FirstOrDefault();

            // if the current unresolved assembly is referenced by our plugin, attempt to load
            if (refAssembly != null)
            {
                // load from the path to this plugin assembly, not host executable
                string dir = Path.GetDirectoryName(currAssembly.Location).ToLower();
                string folder = Path.GetFileNameWithoutExtension(currAssembly.Location);
                dir = Path.Combine(dir, folder);

                var assmbPath = Path.Combine(dir, $"{argName}.dll");

                if (File.Exists(assmbPath))
                {
                    loadAssembly = Assembly.LoadFrom(assmbPath);
                }
                else
                {
                    throw new FileNotFoundException($"Unable to locate dependency: {assmbPath}");
                }
            }

            return loadAssembly;
        }
    }
}