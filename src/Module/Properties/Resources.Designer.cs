﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.1
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Sage.SalesLogix.Migration.Module.Properties {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "4.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class Resources {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Resources() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("Sage.SalesLogix.Migration.Module.Properties.Resources", typeof(Resources).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Delete Report.
        /// </summary>
        internal static string DeleteReportMenuItem_Text {
            get {
                return ResourceManager.GetString("DeleteReportMenuItem_Text", resourceCulture);
            }
        }
        
        internal static System.Drawing.Bitmap ReportsIcon {
            get {
                object obj = ResourceManager.GetObject("ReportsIcon", resourceCulture);
                return ((System.Drawing.Bitmap)(obj));
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Previously run migration reports.
        /// </summary>
        internal static string ReportsWindow_Description {
            get {
                return ResourceManager.GetString("ReportsWindow_Description", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Migration Reports.
        /// </summary>
        internal static string ReportsWindow_Title {
            get {
                return ResourceManager.GetString("ReportsWindow_Title", resourceCulture);
            }
        }
        
        internal static System.Drawing.Bitmap ToolIcon {
            get {
                object obj = ResourceManager.GetObject("ToolIcon", resourceCulture);
                return ((System.Drawing.Bitmap)(obj));
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Migration Tool.
        /// </summary>
        internal static string Tools_Migration {
            get {
                return ResourceManager.GetString("Tools_Migration", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Migration Reports.
        /// </summary>
        internal static string View_MigrationReports {
            get {
                return ResourceManager.GetString("View_MigrationReports", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to View Report.
        /// </summary>
        internal static string ViewReportMenuItem_Text {
            get {
                return ResourceManager.GetString("ViewReportMenuItem_Text", resourceCulture);
            }
        }
    }
}
