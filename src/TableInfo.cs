using System;
using System.Collections.Generic;
using Iesi.Collections.Generic;
using Sage.Platform.Exceptions;
using Sage.SalesLogix.Migration.Collections;

namespace Sage.SalesLogix.Migration
{
    public sealed class TableInfo
    {
        private readonly string _name;
        private readonly ISet<string> _columns;
        private readonly IDictionary<DataPathJoin, TableInfo> _joins;
        private bool _exists;

        public TableInfo(string name)
        {
            Guard.ArgumentNotNullOrEmptyString(name, "name");

            _name = name;
            _columns = new ComparisonSet<string>(StringComparer.InvariantCultureIgnoreCase);
            _joins = new Dictionary<DataPathJoin, TableInfo>();
        }

        public string Name
        {
            get { return _name; }
        }

        public ISet<string> Columns
        {
            get { return _columns; }
        }

        public IDictionary<DataPathJoin, TableInfo> Joins
        {
            get { return _joins; }
        }

        public bool Exists
        {
            get { return _exists; }
            set { _exists = value; }
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
                TableInfo castObj = obj as TableInfo;
                return (castObj != null && _name == castObj._name);
            }
        }

        public override int GetHashCode()
        {
            return _name.GetHashCode();
        }

        public override string ToString()
        {
            return _name;
        }
    }
}