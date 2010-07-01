using System;
using Sage.SalesLogix.Orm;

namespace Sage.SalesLogix.Migration.Orm
{
    [Serializable]
    public class OrmLookup : EntityBase
    {
        private string _lookupId;
        private string _lookupName;
        private string _mainTable;
        private string _searchField;
        private string _idField;
        private string _nameField;
        private string _layout;

        public virtual string LookupId
        {
            get { return _lookupId; }
            set
            {
                if (_lookupId != value)
                {
                    _lookupId = value;
                    NotifyPropertyChanged("LookupId");
                    ValidateProperty("LookupId", value);
                }
            }
        }

        public virtual string LookupName
        {
            get { return _lookupName; }
            set
            {
                if (_lookupName != value)
                {
                    _lookupName = value;
                    NotifyPropertyChanged("LookupName");
                    ValidateProperty("LookupName", value);
                }
            }
        }

        public virtual string MainTable
        {
            get { return _mainTable; }
            set
            {
                if (_mainTable != value)
                {
                    _mainTable = value;
                    NotifyPropertyChanged("MainTable");
                    ValidateProperty("MainTable", value);
                }
            }
        }

        public virtual string SearchField
        {
            get { return _searchField; }
            set
            {
                if (_searchField != value)
                {
                    _searchField = value;
                    NotifyPropertyChanged("SearchField");
                    ValidateProperty("SearchField", value);
                }
            }
        }

        public virtual string IdField
        {
            get { return _idField; }
            set
            {
                if (_idField != value)
                {
                    _idField = value;
                    NotifyPropertyChanged("IdField");
                    ValidateProperty("IdField", value);
                }
            }
        }

        public virtual string NameField
        {
            get { return _nameField; }
            set
            {
                if (_nameField != value)
                {
                    _nameField = value;
                    NotifyPropertyChanged("NameField");
                    ValidateProperty("NameField", value);
                }
            }
        }

        public virtual string Layout
        {
            get { return _layout; }
            set
            {
                if (_layout != value)
                {
                    _layout = value;
                    NotifyPropertyChanged("LayoutManager");
                    ValidateProperty("Layout", value);
                }
            }
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
                OrmLookup castObj = obj as OrmLookup;
                return (castObj != null && LookupId == castObj.LookupId);
            }
        }

        public override int GetHashCode()
        {
            return (LookupId == null ? base.GetHashCode() : LookupId.GetHashCode());
        }

        public override string ToString()
        {
            return SearchField;
        }
    }
}