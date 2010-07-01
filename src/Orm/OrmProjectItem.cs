using System;
using NHibernate;
using Sage.Platform.Orm;
using Sage.SalesLogix.Orm;
using Sage.SalesLogix.Plugins;

namespace Sage.SalesLogix.Migration.Orm
{
    [Serializable]
    public class OrmProjectItem : EntityBase
    {
        private string _projectItemId;
        private OrmProject _project;
        private Plugin _plugin;

        public virtual string ProjectItemId
        {
            get { return _projectItemId; }
            set
            {
                if (_projectItemId != value)
                {
                    _projectItemId = value;
                    NotifyPropertyChanged("ProjectItemId");
                    ValidateProperty("ProjectItemId", value);
                }
            }
        }

        public virtual OrmProject Project
        {
            get { return GetForeignEntity(ref _project); }
            set
            {
                if (_project != value)
                {
                    _project = value;
                    NotifyPropertyChanged("Project");
                    ValidateProperty("Project", value);
                }
            }
        }

        public virtual Plugin Plugin
        {
            get { return GetForeignEntity(ref _plugin); }
            set
            {
                if (_plugin != value)
                {
                    _plugin = value;
                    NotifyPropertyChanged("Plugin");
                    ValidateProperty("Plugin", value);
                }
            }
        }

        private static T GetForeignEntity<T>(ref T value)
            where T : class
        {
            if (value != null && !NHibernateUtil.IsInitialized(value))
            {
                using (ISession session = new SessionScopeWrapper())
                {
                    session.Lock(value, LockMode.None);

                    try
                    {
                        NHibernateUtil.Initialize(value);
                    }
                    catch (ObjectNotFoundException)
                    {
                        value = null;
                    }
                }
            }

            return value;
        }

        public override bool Equals(object obj)
        {
            if (obj == this)
            {
                return true;
            }
            else if (obj == null)
            {
                return false;
            }
            else
            {
                OrmProjectItem castObj = obj as OrmProjectItem;
                return (castObj != null && ProjectItemId == castObj.ProjectItemId);
            }
        }

        public override int GetHashCode()
        {
            return (ProjectItemId == null ? base.GetHashCode() : ProjectItemId.GetHashCode());
        }
    }
}