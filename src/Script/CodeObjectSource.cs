using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Sage.SalesLogix.Migration.Script
{
    public sealed class CodeObjectSource
    {
        //private static readonly IDictionary<CodeObjectSource, CodeObjectSource> _cache = new WeakKeyDictionary<CodeObjectSource, CodeObjectSource>();

        public static CodeObjectSource CreateNullable(CodeObjectSource source)
        {
            if (source == null)
            {
                return null;
            }

            Type type = source.Target as Type;

            if (type != null)
            {
                return InternalCreate(typeof (Nullable<>).MakeGenericType(type), source._arrayRanks, source._type);
            }
            else
            {
                Debug.Assert(false);
                return source;
            }
        }

        public static CodeObjectSource CreateArray(object target, int rank)
        {
            return InternalCreate(target, new int[] {rank}, CodeObjectSourceType.Array);
        }

        public static CodeObjectSource CreateArray(CodeObjectSource source, int rank)
        {
            if (source == null)
            {
                return null;
            }

            int[] ranks;
            CodeObjectSourceType type;

            if (source._type == CodeObjectSourceType.Indexer)
            {
                //Debug.Assert(source._arrayRanks[0] == 1);
                int rankCount = source._arrayRanks.Length - 1;
                ranks = new int[rankCount];

                if (rankCount == 0)
                {
                    type = CodeObjectSourceType.Normal;
                }
                else
                {
                    Array.Copy(source._arrayRanks, 1, ranks, 0, rankCount);
                    type = CodeObjectSourceType.Indexer;
                }
            }
            else
            {
                ranks = new int[source._arrayRanks.Length + 1];
                ranks[0] = rank;
                Array.Copy(source._arrayRanks, 0, ranks, 1, source._arrayRanks.Length);
                type = CodeObjectSourceType.Array;
            }

            return InternalCreate(source._target, ranks, type);
        }

        public static CodeObjectSource CreateIndexer(object target, int rank)
        {
            return InternalCreate(target, new int[] {rank}, CodeObjectSourceType.Indexer);
        }

        public static CodeObjectSource CreateIndexer(CodeObjectSource source, int rank)
        {
            if (source == null)
            {
                return null;
            }

            int[] ranks;
            CodeObjectSourceType type;

            if (source._type == CodeObjectSourceType.Array)
            {
                Debug.Assert(source._arrayRanks[0] == rank);
                int rankCount = source._arrayRanks.Length - 1;
                ranks = new int[rankCount];

                if (rankCount == 0)
                {
                    type = CodeObjectSourceType.Normal;
                }
                else
                {
                    Array.Copy(source._arrayRanks, 1, ranks, 0, rankCount);
                    type = CodeObjectSourceType.Array;
                }
            }
            else
            {
                ranks = new int[source._arrayRanks.Length + 1];
                ranks[0] = rank;
                Array.Copy(source._arrayRanks, 0, ranks, 1, source._arrayRanks.Length);
                type = CodeObjectSourceType.Indexer;
            }

            return InternalCreate(source._target, ranks, type);
        }

        public static CodeObjectSource CreateMerge(CodeObjectSource source, int[] ranks, CodeObjectSourceType type)
        {
            if (type == CodeObjectSourceType.Normal && ranks.Length > 0)
            {
                throw new ArgumentException("Invalid type", "type");
            }

            if (ranks.Length == 0)
            {
                return source;
            }
            else
            {
                CodeObjectSource newSource = source;

                if (type == CodeObjectSourceType.Indexer)
                {
                    for (int i = ranks.Length - 1; i >= 0; i--)
                    {
                        newSource = CreateIndexer(source, ranks[i]);
                    }
                }
                else
                {
                    for (int i = ranks.Length - 1; i >= 0; i--)
                    {
                        newSource = CreateArray(source, ranks[i]);
                    }
                }

                return newSource;
            }
        }

        public static CodeObjectSource CreateMergeXXX(CodeObjectSource source, int[] ranks, CodeObjectSourceType type)
        {
            if (type == CodeObjectSourceType.Normal && ranks.Length > 0)
            {
                throw new ArgumentException("Invalid type", "type");
            }

            if (ranks.Length == 0)
            {
                return source;
            }
            else
            {
                CodeObjectSource newSource = source;

                if (type == CodeObjectSourceType.Array)
                {
                    for (int i = ranks.Length - 1; i >= 0; i--)
                    {
                        newSource = CreateIndexer(source, ranks[i]);
                    }
                }
                else
                {
                    for (int i = ranks.Length - 1; i >= 0; i--)
                    {
                        newSource = CreateArray(source, ranks[i]);
                    }
                }

                return newSource;
            }
        }

        public static CodeObjectSource Create(object target)
        {
            return InternalCreate(target, null, CodeObjectSourceType.Normal);
        }

        private static CodeObjectSource InternalCreate(object target, int[] arrayRanks, CodeObjectSourceType type)
        {
            if (target == null)
            {
                return null;
            }

            Debug.Assert(Utils.IsSource(target));

            if (arrayRanks == null)
            {
                arrayRanks = new int[0];
            }

            Type typeTarget = target as Type;

            if (typeTarget != null && typeTarget.IsArray)
            {
                List<int> ranks = new List<int>();

                while (typeTarget.IsArray)
                {
                    ranks.Add(typeTarget.GetArrayRank());
                    typeTarget = typeTarget.GetElementType();
                }

                target = typeTarget;
                arrayRanks = ranks.ToArray();
            }

            CodeObjectSource source = new CodeObjectSource(target, arrayRanks, type);
            //CodeObjectSource cachedSource;

            //if (_cache.TryGetValue(source, out cachedSource))
            //{
            //    source = cachedSource;
            //}
            //else
            //{
            //    _cache.Add(source, source);
            //}

            return source;
        }

        //=======================================

        private readonly object _target;
        private readonly int[] _arrayRanks;
        private readonly CodeObjectSourceType _type;

        private CodeObjectSource(object target, int[] arrayRanks, CodeObjectSourceType type)
        {
            _target = target;
            _arrayRanks = arrayRanks;
            _type = type;
        }

        public object Target
        {
            get { return _target; }
        }

        public int[] ArrayRanks
        {
            get { return _arrayRanks; }
        }

        public CodeObjectSourceType Type
        {
            get { return _type; }
        }

        public override bool Equals(object obj)
        {
            CodeObjectSource source = obj as CodeObjectSource;
            bool equal = (source != null &&
                          _target == source._target &&
                          _arrayRanks.Length == source._arrayRanks.Length &&
                          _type == source._type);

            if (equal)
            {
                for (int i = 0; i < _arrayRanks.Length; i++)
                {
                    if (_arrayRanks[i] != source._arrayRanks[i])
                    {
                        return false;
                    }
                }
            }

            return equal;
        }

        public override int GetHashCode()
        {
            int code = _target.GetHashCode();

            for (int i = 0; i < _arrayRanks.Length; i++)
            {
                code ^= _arrayRanks[i];
            }

            return code ^ (int) _type;
        }

        public override string ToString()
        {
            return string.Format("{0} [{1}] {2}", _target,
                                 string.Join(", ", Array.ConvertAll<int, string>(
                                                       _arrayRanks,
                                                       delegate(int element)
                                                           {
                                                               return element.ToString();
                                                           })), _type);
        }
    }
}