using System.Collections.Generic;
using Sage.Platform.Orm.Entities;
using Sage.Platform.Projects;
using Sage.Platform.Projects.Interfaces;
using Sage.Platform.QuickForms;
using Sage.Platform.WebPortal.Design;
using Sage.SalesLogix.Migration.Module;
using Sage.SalesLogix.Migration.Module.Services;

namespace Sage.SalesLogix.Migration.Tests
{
    public static class TestManifestService
    {
        public static void Test(MigrationWorkItem workItem)
        {
            //TODO: remove workItem depency, add context holder, uncomment broken bits

            IProject project = workItem.Services.Get<IProjectContextService>().ActiveProject;
            OrmModel orm = project.Models.Get<OrmModel>();
            QuickFormModel qfModel = project.Models.Get<QuickFormModel>();
            MigrationSettings s = new MigrationSettings();
            s.PackageName = "test";
            MigrationContext c = new MigrationContext(
                s,
                orm.Packages[0],
                null,
                null,
                null,
                null,
                null,
                workItem.Log,
                new EmptyOperationStatus(),
                null);

            OrmEntity entity = orm.Packages[1].Entities[0];
            IList<IQuickFormDefinition> forms = qfModel.LoadDefinitions(entity);
            FormInfo form = new FormInfo(null, "0", false, "0", "0", 0, 0);
            form.QuickForm = (IQuickFormDefinition) forms[0];
            c.Forms.Add("0", form);
            c.Forms.Add("0x", form);
            form = new FormInfo(null, "1", false, "1", "1", 1, 1);
            form.QuickForm = (IQuickFormDefinition) forms[1];
            form.Entity = entity;
            c.Forms.Add("1", form);
            c.Forms.Add("1x", form);

            entity = orm.Packages[1].Entities[12];
            forms = qfModel.LoadDefinitions(entity);
            form = new FormInfo(null, "2", false, "2", "2", 2, 2);
            form.QuickForm = (IQuickFormDefinition) forms[0];
            c.Forms.Add("2", form);
            c.Forms.Add("2x", form);
            form = new FormInfo(null, "3", false, "3", "3", 3, 3);
            form.QuickForm = (IQuickFormDefinition) forms[1];
            form.Entity = entity;
            c.Forms.Add("3", form);
            c.Forms.Add("3x", form);

            //c.Relationships.Add("0", new OrmRelationship(orm.Relationships[0], true));
            //c.Relationships.Add("0x", new OrmRelationship(orm.Relationships[0], true));
            //c.Relationships.Add("1", new OrmRelationship(orm.Relationships[1], true));
            //c.Relationships.Add("1x", new OrmRelationship(orm.Relationships[1], true));

            PortalApplication portalApp = PortalApplication.Get("SlxClient");
            portalApp.Model = project.Models.Get<PortalModel>();

            LinkedFile file = portalApp.SupportFiles.GetFiles()[0];
            c.LinkedFiles.Add(file);
            c.LinkedFiles.Add(file);
            file = portalApp.SupportFiles.GetFiles()[1];
            c.LinkedFiles.Add(file);
            c.LinkedFiles.Add(file);
            file = portalApp.SupportFiles.GetFolders()[1].GetFiles()[0];
            c.LinkedFiles.Add(file);
            c.LinkedFiles.Add(file);
            file = portalApp.SupportFiles.GetFolders()[1].GetFiles()[1];
            c.LinkedFiles.Add(file);
            c.LinkedFiles.Add(file);

            //SmartPartMapping part = portalApp.Pages[0].SmartParts[0];
            //c.SmartParts.Add("0", part);
            //c.SmartParts.Add("0x", part);
            //part = portalApp.Pages[0].SmartParts[1];
            //c.SmartParts.Add("1", part);
            //c.SmartParts.Add("1x", part);
            //part = portalApp.Pages[1].SmartParts[0];
            //c.SmartParts.Add("2", part);
            //c.SmartParts.Add("2x", part);
            //part = portalApp.Pages[1].SmartParts[1];
            //c.SmartParts.Add("3", part);
            //c.SmartParts.Add("3x", part);

            IManifestService b = workItem.Services.Get<IManifestService>();
            b.Generate();
        }
    }
}