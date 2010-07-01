using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Drawing.Imaging;
using System.IO;
using System.Xml.Serialization;
using Sage.Platform.Application;
using Sage.Platform.Configuration;
using Sage.Platform.Data;
using Sage.Platform.Orm.CodeGen;
using Sage.Platform.Projects;
using Sage.Platform.QuickForms;
using Sage.Platform.VirtualFileSystem;
using Sage.Platform.WebPortal.Design;
using Sage.SalesLogix.Migration.Services;

namespace Sage.SalesLogix.Migration.Module.Services
{
    public sealed class PortalService : IPortalService
    {
        private readonly StepHandler[] _steps;
        private MigrationContext _context;
        private ITypeResolutionService _typeResolver;

        public PortalService()
        {
            _steps = new StepHandler[]
                {
                    LinkPackageAssembly,
                    UpdateHibernateConfig,
                    AddMainViewPages,
                    AnalyzeEntityPages,
                    AssociateSmartParts,
                    AddNavigationItems,
                    SavePortal
                };
        }

        [ServiceDependency]
        public IMigrationContextHolderService ContextHolder
        {
            set
            {
                value.ContextChanged += delegate
                    {
                        _context = value.Context;
                    };
                _context = value.Context;
            }
        }

        [ServiceDependency]
        public ITypeResolutionService TypeResolver
        {
            set { _typeResolver = value; }
        }

        private string _assemblyName;
        private IDictionary<Type, PortalPage> _pages;

        #region IPortalService Members

        public void Update()
        {
            int totalSteps = _steps.Length
                             + _context.MainViews.Count
                             + _context.Portal.Pages.Count
                             + _context.Forms.Count;

            using (_context.Status.BeginStep("Updating portal...", totalSteps))
            {
                foreach (StepHandler step in _steps)
                {
                    step();

                    if (!_context.Status.Advance())
                    {
                        break;
                    }
                }
            }

            _pages = null;
        }

        #endregion

        private void LinkPackageAssembly()
        {
            if (_context.Package != null && _context.Package.Entities.Count > 0)
            {
                _assemblyName = _context.Package.GetAssemblyName();
                string path = @"SupportFiles\Bin";
                string fileName = _assemblyName + ".dll";
                string source = Path.Combine(@"VFS:\Webroot\common\bin", fileName);
                bool exists = CollectionUtils.Contains(
                    _context.Portal.SupportFilesDefinition.Files,
                    delegate(ExternalFileDefinition item)
                        {
                            return (StringUtils.CaseInsensitiveEquals(item.ProjectFolder, path) &&
                                    StringUtils.CaseInsensitiveEquals(item.Source, source));
                        });

                if (!exists)
                {
                    _context.Portal.SupportFilesDefinition.Files.Add(new ExternalFileDefinition(path, source));
                }

                LinkedFolder binFolder = Array.Find(
                    _context.Portal.SupportFiles.GetFolders(),
                    delegate(LinkedFolder folder)
                        {
                            return (StringUtils.CaseInsensitiveEquals(folder.Name, "Bin"));
                        });

                if (binFolder != null)
                {
                    FetchLinkedFile(binFolder, path + @"\" + fileName);
                }
                else
                {
                    LogWarning("Unable to find linked folder 'Bin' in '{0}' portal", _context.Portal);
                }
            }
        }

        private void UpdateHibernateConfig()
        {
            var file = new VirtualFileInfo(string.Format(@"\Webroot\{0}\hibernate.xml", _context.Portal.PortalAlias));
			
            if (_context.Package != null && _context.Package.Entities.Count > 0 && file.Exists)
            {
                XmlSerializer serializer = new XmlSerializer(typeof (HibernateConfiguration));
                HibernateConfiguration config;

                using (Stream stream = new VirtualFileStream(file, VirtualFileMode.Open))
                {
                    config = (HibernateConfiguration) serializer.Deserialize(stream);
                }

                bool exists = CollectionUtils.Contains(
                    config.MappingAssemblies,
                    delegate(string item)
                        {
                            return (StringUtils.CaseInsensitiveEquals(item, _assemblyName));
                        });

                if (!exists)
                {
                    config.MappingAssemblies.Add(_assemblyName);

                    using (Stream stream = new VirtualFileStream(file, VirtualFileMode.Create))
                    {
                        serializer.Serialize(stream, config);
                    }
                }

                FetchLinkedFile(_context.Portal.SupportFiles, @"SupportFiles\hibernate.xml");
            }
        }

        private void AddMainViewPages()
        {
            foreach (MainViewInfo mainView in _context.MainViews.Values)
            {
                PortalPage page = CollectionUtils.Find(
                    _context.Portal.Pages,
                    delegate(PortalPage item)
                        {
                            return StringUtils.CaseInsensitiveEquals(item.PageAlias, mainView.MainTable);
                        });

                if (page == null)
                {
                    page = new PortalPage();
                    _context.Portal.Pages.Add(page);
                }

                page.PageAlias = mainView.MainTable;
                page.PageTitle = mainView.FullName;
                page.PageDescription = mainView.FullName;

                if (mainView.Entity != null)
                {
                    page.InheritsFrom = "Sage.Platform.WebPortal.EntityPage, Sage.Platform.WebPortal";
                    string entityTypeName = string.Format("Sage.Entity.Interfaces.{0}, Sage.Entity.Interfaces", mainView.Entity.InterfaceName);

                    if (page.PageProperties.HasProperty("EntityTypeName"))
                    {
                        page.PageProperties["EntityTypeName"].PropertyValue = entityTypeName;
                    }
                    else
                    {
                        page.PageProperties.Add("EntityTypeName", entityTypeName);
                    }

                    WebModule module = CollectionUtils.Find(
                        page.Modules,
                        delegate(WebModule item)
                            {
                                return StringUtils.CaseInsensitiveEquals(item.ModuleTypeName, "Sage.SalesLogix.Client.GroupBuilder.Modules.GroupViewerModule, Sage.SalesLogix.Client.GroupBuilder.Modules");
                            });

                    if (module == null)
                    {
                        module = new WebModule();
                        page.Modules.Add(module);
                    }

                    module.ModuleTypeName = "Sage.SalesLogix.Client.GroupBuilder.Modules.GroupViewerModule, Sage.SalesLogix.Client.GroupBuilder.Modules";
                    module.Validate();
                    module.Save();

                    SmartPartMapping smartPart = GetSmartPart(page, "LiveGroupViewer");
                    smartPart.SmartPart = @"SmartParts\GroupBuilder\LiveGroupViewer.ascx";
                    smartPart.TargetWorkspace = "MainContent";
                    smartPart.ShowInMode = "List";
                    smartPart.Title = "Entity Group Viewer";
                    smartPart.Validate();
                    smartPart.Save();
                }

                if (mainView.DetailForm != null)
                {
                    AppendSmartPart(page, mainView.DetailForm, "MainContent", mainView.DetailForm.DialogCaption);
                }

                page.Validate();
                page.Save();
            }
        }

        private void AnalyzeEntityPages()
        {
            _pages = new Dictionary<Type, PortalPage>();
            IDictionary<Type, int> smartPartCount = new Dictionary<Type, int>();

            foreach (PortalPage page in _context.Portal.Pages)
            {
                PropertyConfiguration entityTypeNameProperty = page.PageProperties["EntityTypeName"];

                if (entityTypeNameProperty != null)
                {
                    Type entityInterfaceType = _typeResolver.GetType(entityTypeNameProperty.PropertyValue, false, true);

                    if (entityInterfaceType != null)
                    {
                        int count = 0;

                        foreach (SmartPartMapping mapping in page.SmartParts)
                        {
                            if (mapping.ShowInMode == "Detail" && mapping.TargetWorkspace == "TabControl")
                            {
                                count++;
                            }
                        }

                        int existingCount;

                        if (!smartPartCount.TryGetValue(entityInterfaceType, out existingCount) || count > existingCount)
                        {
                            _pages[entityInterfaceType] = page;
                            smartPartCount[entityInterfaceType] = count;
                        }
                    }
                }

                if (!_context.Status.Advance())
                {
                    break;
                }
            }
        }

        private void AssociateSmartParts()
        {
            foreach (FormInfo form in _context.Forms.Values)
            {
                PortalPage page;
                Type type = _typeResolver.GetType(string.Format("Sage.Entity.Interfaces.{0}, Sage.Entity.Interfaces", form.Entity.InterfaceName));

                if (type != null && _pages.TryGetValue(type, out page))
                {
                    if (form.IsDataForm && !form.IsDetail)
                    {
                        AppendSmartPart(page, form, "TabControl", form.TabCaption);
                    }

                    foreach (FormInfo dialogForm in form.DialogForms)
                    {
                        AppendSmartPart(page, dialogForm, "DialogWorkspace", dialogForm.DialogCaption);
                    }

                    page.Validate();
                    page.Save();
                }

                if (!_context.Status.Advance())
                {
                    break;
                }
            }
        }

        private void AddNavigationItems()
        {
            foreach (NavigationInfo navigation in _context.Navigation)
            {
                if (navigation.GroupName == null)
                {
                    MenuDropDownItem menuItem = CollectionUtils.Find(
                        _context.Portal.MenuItems,
                        delegate(MenuDropDownItem item)
                            {
                                return StringUtils.CaseInsensitiveEquals(item.ItemId, navigation.GroupId);
                            });

                    if (menuItem == null)
                    {
                        menuItem = new MenuDropDownItem();
                        _context.Portal.MenuItems.Add(menuItem);
                    }

                    menuItem.ItemId = navigation.Id;
                    menuItem.NavUrl = navigation.NavUrl;
                    menuItem.Title = navigation.Caption ?? string.Empty;

                    if (navigation.Glyph != null)
                    {
                        string extension;

                        if (navigation.Glyph is Metafile) //vector
                        {
                            extension = "emf";
                        }
                        else if (navigation.Glyph.GetFrameCount(FrameDimension.Time) > 1 || //animated
                                 (navigation.Glyph.PixelFormat & PixelFormat.Indexed) == PixelFormat.Indexed) //indexed
                        {
                            extension = "gif";
                        }
                        else if ((navigation.Glyph.PixelFormat & PixelFormat.Alpha) == PixelFormat.Alpha) //transparency
                        {
                            extension = "png";
                        }
                        else
                        {
                            extension = "jpg";
                        }

                        string name = string.Format("{0}_{1}x{2}.{3}", navigation.SafeCaption, navigation.Glyph.Width, navigation.Glyph.Height, extension);
                        _context.GlobalImageResourceManager.AddUpdateResource(name, navigation.Glyph);
                        menuItem.SmallImageUrl = string.Format("[Localization.Global_Images:{0}]", name);
                    }

                    menuItem.Validate();
                    menuItem.Save();

                    navigation.Item = menuItem;
                }
                else
                {
                    NavigationGroup navGroup = CollectionUtils.Find(
                        _context.Portal.NavigationGroups,
                        delegate(NavigationGroup item)
                            {
                                return StringUtils.CaseInsensitiveEquals(item.ItemId, navigation.GroupId);
                            });

                    if (navGroup == null)
                    {
                        navGroup = new NavigationGroup();
                        _context.Portal.NavigationGroups.Add(navGroup);
                    }

                    navGroup.ItemId = navigation.GroupId;
                    navGroup.Title = navigation.GroupName;
                    navGroup.Description = navigation.GroupName;

                    NavigationItem navItem = CollectionUtils.Find(
                        navGroup.NavItems,
                        delegate(NavigationItem item)
                            {
                                return StringUtils.CaseInsensitiveEquals(item.ItemId, navigation.Id);
                            });

                    if (navItem == null)
                    {
                        navItem = new NavigationItem();
                        navGroup.NavItems.Add(navItem);
                    }

                    navItem.ItemId = navigation.Id;
                    navItem.NavUrl = navigation.NavUrl;
                    navItem.Title = navigation.Caption ?? string.Empty;
                    navItem.Description = navigation.Caption ?? string.Empty;

                    if (navigation.Glyph != null)
                    {
                        string extension;

                        if (navigation.Glyph is Metafile) //vector
                        {
                            extension = "emf";
                        }
                        else if (navigation.Glyph.GetFrameCount(FrameDimension.Time) > 1 || //animated
                                 (navigation.Glyph.PixelFormat & PixelFormat.Indexed) == PixelFormat.Indexed) //indexed
                        {
                            extension = "gif";
                        }
                        else if ((navigation.Glyph.PixelFormat & PixelFormat.Alpha) == PixelFormat.Alpha) //transparency
                        {
                            extension = "png";
                        }
                        else
                        {
                            extension = "jpg";
                        }

                        string name = string.Format("{0}_{1}x{2}.{3}", navigation.SafeCaption, navigation.Glyph.Width, navigation.Glyph.Height, extension);
                        _context.GlobalImageResourceManager.AddUpdateResource(name, navigation.Glyph);
                        navItem.LargeImageUrl = string.Format("[Localization.Global_Images:{0}]", name);
                    }

                    navItem.Validate();
                    navItem.Save();

                    navGroup.Validate();
                    navGroup.Save();

                    navigation.Item = navItem;
                }
            }

            _context.GlobalImageResourceManager.SaveAll();
        }

        private void SavePortal()
        {
            _context.Portal.Validate();
            _context.Portal.Save();
        }

        private void FetchLinkedFile(LinkedFolder folder, string projectPath)
        {
            LinkedFile file = Array.Find(
                folder.GetFiles(),
                delegate(LinkedFile item)
                    {
                        return (StringUtils.CaseInsensitiveEquals(item.ProjectPath, projectPath));
                    });

            if (file != null)
            {
                _context.LinkedFiles.Add(file);
            }
            else
            {
                LogWarning("Unable to find linked file '{0}' in '{1}' portal", projectPath, _context.Portal);
            }
        }

        private void AppendSmartPart(PortalPage page, FormInfo form, string targetWorkspace, string caption)
        {
            SmartPartMapping smartPart = GetSmartPart(page, form.SmartPartId);
            smartPart.Title = caption;
            smartPart.ShowInMode = "Detail";
            smartPart.TargetWorkspace = targetWorkspace;
            //smartPart.SmartPart = form.QuickForm.Id.ToString();
            //smartPart.SmartPart = form.QuickForm.Name; - don't! it is already set to the full path
            if (String.IsNullOrEmpty(smartPart.SmartPart)) smartPart.SmartPart = form.QuickForm.Url;
            smartPart.ReferencedModelId = typeof (QuickFormModel).GUID;
            smartPart.Validate();
            smartPart.Save();
            _context.SmartParts.Add(smartPart);
        }

        private static SmartPartMapping GetSmartPart(PortalPage page, string smartPartId)
        {
            SmartPartMapping smartPart = CollectionUtils.Find(
                page.SmartParts,
                delegate(SmartPartMapping item)
                    {
                        return (StringUtils.CaseInsensitiveEquals(item.SmartPartId, smartPartId));
                    });

            if (smartPart == null)
            {
                smartPart = new SmartPartMapping();
                page.SmartParts.Add(smartPart);
            }

            smartPart.SmartPartId = smartPartId;
            return smartPart;
        }

        private void LogWarning(string text, params object[] args)
        {
            if (_context != null && _context.Log != null)
            {
                _context.Log.Warn(text, args);
            }
        }
    }
}