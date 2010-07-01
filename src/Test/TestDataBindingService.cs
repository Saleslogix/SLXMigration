using System.Diagnostics;
using Sage.SalesLogix.Migration.Forms.Services;
using Sage.SalesLogix.Migration.Module.Services;
using Sage.SalesLogix.Migration.Services;

namespace Sage.SalesLogix.Migration.Tests
{
    public static class TestDataBindingService
    {
        public static void Test1()
        {
            IMigrationContextHolderService holder = CreateContextHolder();
            MigrationContext context = holder.Context;
            DataPathTranslationService dataPathTranslator = new DataPathTranslationService();
            dataPathTranslator.ContextHolder = holder;
            dataPathTranslator.RegisterField(DataPath.Parse("ACCOUNT:ACCOUNT"));
            dataPathTranslator.RegisterField(DataPath.Parse("ACCOUNT:TYPE"));
            dataPathTranslator.RegisterField(DataPath.Parse("ACCOUNT:STATUS"));
            Debug.Assert(context.Tables.Count == 1);
            Debug.Assert(context.Tables.ContainsKey("ACCOUNT"));
            TableInfo t = context.Tables["ACCOUNT"];
            Debug.Assert(t.Joins.Count == 0);
            Debug.Assert(t.Columns.Count == 3);
            Debug.Assert(t.Columns.ContainsAll(new string[] {"ACCOUNT", "TYPE", "STATUS"}));
        }

        public static void Test2()
        {
            IMigrationContextHolderService holder = CreateContextHolder();
            MigrationContext context = holder.Context;
            DataPathTranslationService dataPathTranslator = new DataPathTranslationService();
            dataPathTranslator.ContextHolder = holder;
            dataPathTranslator.RegisterField(DataPath.Parse("CONTACT:ACCID>ACCOUNTID.ACCOUNT!ACCOUNT"));
            dataPathTranslator.RegisterField(DataPath.Parse("CONTACT:ACCID>ACCOUNTID.ACCOUNT!TYPE"));
            dataPathTranslator.RegisterField(DataPath.Parse("CONTACT:ACCID>ACCOUNTID.ACCOUNT!STATUS"));
            Debug.Assert(context.Tables.Count == 2);
            Debug.Assert(context.Tables.ContainsKey("CONTACT"));
            Debug.Assert(context.Tables.ContainsKey("ACCOUNT"));
            TableInfo contact = context.Tables["CONTACT"];
            TableInfo account = context.Tables["ACCOUNT"];

            Debug.Assert(contact.Joins.Count == 1);
            DataPathJoin j = new DataPathJoin("CONTACT", "ACCID", "ACCOUNTID", "ACCOUNT");
            Debug.Assert(contact.Joins.ContainsKey(j));
            Debug.Assert(contact.Joins[j] == account);
            Debug.Assert(contact.Columns.Count == 1);
            Debug.Assert(contact.Columns.Contains("ACCID"));

            Debug.Assert(account.Joins.Count == 0);
            Debug.Assert(account.Columns.Count == 4);
            Debug.Assert(account.Columns.ContainsAll(new string[] {"ACCOUNTID", "ACCOUNT", "TYPE", "STATUS"}));
        }

        public static void Test3()
        {
            IMigrationContextHolderService holder = CreateContextHolder();
            MigrationContext context = holder.Context;
            DataPathTranslationService dataPathTranslator = new DataPathTranslationService();
            dataPathTranslator.ContextHolder = holder;
            dataPathTranslator.RegisterJoin(
                DataPath.Parse("SECCODE:SECCODEID"),
                DataPath.Parse("ACCOUNT:SECID"));
            Debug.Assert(context.Tables.Count == 2);
            Debug.Assert(context.Tables.ContainsKey("ACCOUNT"));
            Debug.Assert(context.Tables.ContainsKey("SECCODE"));
            TableInfo account = context.Tables["ACCOUNT"];
            TableInfo seccode = context.Tables["SECCODE"];

            Debug.Assert(account.Joins.Count == 0);
            Debug.Assert(account.Columns.Count == 1);
            Debug.Assert(account.Columns.Contains("SECID"));

            Debug.Assert(seccode.Joins.Count == 1);
            DataPathJoin j = new DataPathJoin("SECCODE", "SECCODEID", "SECID", "ACCOUNT");
            Debug.Assert(seccode.Joins.ContainsKey(j));
            Debug.Assert(seccode.Joins[j] == account);
            Debug.Assert(seccode.Columns.Count == 1);
            Debug.Assert(seccode.Columns.Contains("SECCODEID"));
        }

        public static void Test4()
        {
            IMigrationContextHolderService holder = CreateContextHolder();
            MigrationContext context = holder.Context;
            DataPathTranslationService dataPathTranslator = new DataPathTranslationService();
            dataPathTranslator.ContextHolder = holder;
            dataPathTranslator.RegisterJoin(
                DataPath.Parse("USERINFO:USERID>ACCESSID.SECCODE!SECCODEID"),
                DataPath.Parse("CONTACT:ACCID>ACCOUNTID.ACCOUNT!SECID"));
            Debug.Assert(context.Tables.Count == 4);
            Debug.Assert(context.Tables.ContainsKey("CONTACT"));
            Debug.Assert(context.Tables.ContainsKey("ACCOUNT"));
            Debug.Assert(context.Tables.ContainsKey("USERINFO"));
            Debug.Assert(context.Tables.ContainsKey("SECCODE"));
            TableInfo contact = context.Tables["CONTACT"];
            TableInfo account = context.Tables["ACCOUNT"];
            TableInfo userinfo = context.Tables["USERINFO"];
            TableInfo seccode = context.Tables["SECCODE"];

            Debug.Assert(contact.Joins.Count == 1);
            DataPathJoin j = new DataPathJoin("CONTACT", "ACCID", "ACCOUNTID", "ACCOUNT");
            Debug.Assert(contact.Joins.ContainsKey(j));
            Debug.Assert(contact.Joins[j] == account);
            Debug.Assert(contact.Columns.Count == 1);
            Debug.Assert(contact.Columns.Contains("ACCID"));

            Debug.Assert(account.Joins.Count == 0);
            Debug.Assert(account.Columns.Count == 2);
            Debug.Assert(account.Columns.ContainsAll(new string[] {"ACCOUNTID", "SECID"}));

            Debug.Assert(userinfo.Joins.Count == 1);
            j = new DataPathJoin("USERINFO", "USERID", "ACCESSID", "SECCODE");
            Debug.Assert(userinfo.Joins.ContainsKey(j));
            Debug.Assert(userinfo.Joins[j] == seccode);
            Debug.Assert(userinfo.Columns.Count == 1);
            Debug.Assert(userinfo.Columns.Contains("USERID"));

            Debug.Assert(seccode.Joins.Count == 1);
            j = new DataPathJoin("SECCODE", "SECCODEID", "SECID", "ACCOUNT");
            Debug.Assert(seccode.Joins.ContainsKey(j));
            Debug.Assert(seccode.Joins[j] == account);
            Debug.Assert(seccode.Columns.Count == 2);
            Debug.Assert(seccode.Columns.ContainsAll(new string[] {"ACCESSID", "SECCODEID"}));
        }

        private static IMigrationContextHolderService CreateContextHolder()
        {
            IMigrationContextHolderService holder = new MigrationContextHolderService();
            holder.Context = new MigrationContext(
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null);
            return holder;
        }
    }
}