using System;
using Iesi.Collections.Generic;
using NHibernate;
using Sage.Platform.Orm;
using Sage.SalesLogix.Orm;

namespace Sage.SalesLogix.Migration.Orm
{
    [Serializable]
    public class OrmProject : EntityBase
    {
        private string _projectId;
        private string _name;
#pragma warning disable 649
        private ISet<OrmProjectItem> _items;
#pragma warning restore 649

        public virtual string ProjectId
        {
            get { return _projectId; }
            set
            {
                if (_projectId != value)
                {
                    _projectId = value;
                    NotifyPropertyChanged("ProjectId");
                    ValidateProperty("ProjectId", value);
                }
            }
        }

        public virtual string Name
        {
            get { return _name; }
            set
            {
                if (_name != value)
                {
                    _name = value;
                    NotifyPropertyChanged("Name");
                    ValidateProperty("Name", value);
                }
            }
        }

        public virtual ISet<OrmProjectItem> Items
        {
            get { return GetCollection(_items); }
        }

        private T GetCollection<T>(T value)
        {
            if (!NHibernateUtil.IsInitialized(value))
            {
                using (ISession session = new SessionScopeWrapper())
                {
                    session.Lock(this, LockMode.None);
                    NHibernateUtil.Initialize(value);
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
                OrmProject castObj = obj as OrmProject;
                return (castObj != null && ProjectId == castObj.ProjectId);
            }
        }

        public override int GetHashCode()
        {
            return (ProjectId == null ? base.GetHashCode() : ProjectId.GetHashCode());
        }

        public override string ToString()
        {
            return Name;
        }
    }
}