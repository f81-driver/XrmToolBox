using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Reflection;
using XrmToolBox.Extensibility;
using XrmToolBox.Extensibility.Interfaces;

namespace Formula81.XrmToolBox.Tools.AuditGoggles
{
    // Do not forget to update version number and author (company attribute) in AssemblyInfo.cs class
    // To generate Base64 string for Images below, you can use https://www.base64-image.de/
    [Export(typeof(IXrmToolBoxPlugin)),
        ExportMetadata("Name", "Audit Goggles"),
        ExportMetadata("Description", "Protects you sight from the perils of Audit History"),
        // Please specify the base64 content of a 32x32 pixels image        
        ExportMetadata("SmallImageBase64", "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"),
        // Please specify the base64 content of a 80x80 pixels image
        ExportMetadata("BigImageBase64", "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"),
        ExportMetadata("BackgroundColor", "#FF8000"),
        ExportMetadata("PrimaryFontColor", "White"),
        ExportMetadata("SecondaryFontColor", "Black")]
    public class AuditGogglesPlugin : PluginBase, IPayPalPlugin
    {
        internal const string PayPalEmailAccount = "formulaeightyone@gmail.com";
        internal const string PayPalDonationDescription = "Formula 81 Donation";

        public string DonationDescription => PayPalDonationDescription;
        public string EmailAccount => PayPalEmailAccount;

        public override IXrmToolBoxPluginControl GetControl()
        {
            return new AuditGogglesPluginControl();
        }

        /// <summary>
        /// Constructor 
        /// </summary>
        public AuditGogglesPlugin()
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