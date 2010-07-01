using System;
using System.Collections.Generic;
using Sage.Platform.Exceptions;

namespace Sage.SalesLogix.Migration
{
    public sealed class DataPath
    {
        private readonly string _rootTable;
        private readonly List<DataPathJoin> _joins;
        private readonly string _targetField;

        public DataPath(string rootTable, string targetField)
        {
            Guard.ArgumentNotNullOrEmptyString(rootTable, "rootTable");
            Guard.ArgumentNotNullOrEmptyString(targetField, "targetField");

            _rootTable = rootTable;
            _joins = new List<DataPathJoin>();
            _targetField = targetField;
        }

        public string RootTable
        {
            get { return _rootTable; }
        }

        public IList<DataPathJoin> Joins
        {
            get { return _joins; }
        }

        public string TargetField
        {
            get { return _targetField; }
        }

        public string RootField
        {
            get { return (_joins.Count > 0 ? _joins[0].FromField : _targetField); }
        }

        public string TargetTable
        {
            get { return (_joins.Count > 0 ? _joins[_joins.Count - 1].ToTable : _rootTable); }
        }

        public DataPath Reverse()
        {
            DataPath newDataPath = new DataPath(TargetTable, RootField);

            foreach (DataPathJoin join in _joins)
            {
                newDataPath._joins.Insert(0, new DataPathJoin(
                                                 join.ToTable,
                                                 join.ToField,
                                                 join.FromTable,
                                                 join.FromField));
            }

            return newDataPath;
        }

        public static DataPath Parse(string text)
        {
            Guard.ArgumentNotNullOrEmptyString(text, "text");

            string[] parts = text.Split(':');

            if (parts.Length < 2)
            {
                throw new FormatException("String was not recognized as a valid data path");
            }

            string fromTable = parts[0];
            parts = parts[1].Split('!');
            int count = parts.Length;
            DataPath dataPath = new DataPath(fromTable, parts[count - 1]);

            for (int i = 0; i < count - 1; i++)
            {
                DataPathJoin join = DataPathJoin.Parse(fromTable, parts[i]);
                dataPath.Joins.Add(join);
                fromTable = join.ToTable;
            }

            return dataPath;
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
                DataPath castObj = obj as DataPath;
                int joinCount = _joins.Count;

                if (castObj != null &&
                    StringUtils.CaseInsensitiveEquals(_rootTable, castObj._rootTable) &&
                    StringUtils.CaseInsensitiveEquals(_targetField, castObj._targetField) &&
                    joinCount == castObj._joins.Count)
                {
                    for (int i = 0; i < joinCount; i++)
                    {
                        if (!Equals(_joins[i], castObj._joins[i]))
                        {
                            return false;
                        }
                    }

                    return true;
                }

                return false;
            }
        }

        public override int GetHashCode()
        {
            int code = _rootTable.GetHashCode();
            _joins.ForEach(
                delegate(DataPathJoin join)
                    {
                        code ^= join.GetHashCode();
                    });
            return code ^ _targetField.GetHashCode();
        }

        public override string ToString()
        {
            return string.Format(
                "{0}:{1}{2}",
                _rootTable,
                string.Join(
                    string.Empty,
                    _joins.ConvertAll<string>(
                        delegate(DataPathJoin join)
                            {
                                return join + "!";
                            }).ToArray()),
                _targetField);
        }
    }
}