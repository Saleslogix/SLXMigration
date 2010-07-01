using Sage.Platform.Orm.Entities;

namespace Sage.SalesLogix.Migration
{
    public sealed class RelationshipInfo
    {
        private readonly OrmRelationship _relationship;
        private readonly bool _isOneToMany;
        private readonly string _propertyName;

        public RelationshipInfo(OrmRelationship relationship, bool isInverted)
        {
            _relationship = relationship;
            _isOneToMany = ((relationship.Cardinality == OrmRelationship.OneToMany) != isInverted);
            _propertyName = (isInverted
                                 ? relationship.ChildProperty
                                 : relationship.ParentProperty).PropertyName;
        }

        public RelationshipInfo(OrmEntity parentEntity, OrmEntity childEntity, bool isInverted)
        {
            _relationship = null;
            _isOneToMany = false;
            _propertyName = (isInverted
                                 ? parentEntity
                                 : childEntity).Name;
        }

        public OrmRelationship Relationship
        {
            get { return _relationship; }
        }

        public bool IsOneToMany
        {
            get { return _isOneToMany; }
        }

        public string PropertyName
        {
            get { return _propertyName; }
        }
    }
}