using System.Collections.Generic;

namespace Formula81.XrmToolBox.Tools.MetaScanner.Models
{
    public class EntityDetail
    {
        public MetaEntity Entity { get; }
        public string EntitySetName { get; }
        public string PrimaryIdAttribute { get; }
        public string PrimaryNameAttribute { get; }
        public string OwnershipType { get; }
        public IEnumerable<MetaAttribute> Attributes { get; }
        public IEnumerable<MetaRelationship> Relationships { get; }
        public IEnumerable<MetaKey> Keys { get; }
        public IEnumerable<MetaPrivilege> Privileges { get; }
        public IEnumerable<MetaProperty> Properties { get; }

        public EntityDetail(MetaEntity entity, string entitySetName, string primaryIdAttribute, string primaryNameAttribute, string ownershipType,
            IEnumerable<MetaAttribute> attributes, IEnumerable<MetaRelationship> relationships, IEnumerable<MetaKey> keys,
            IEnumerable<MetaPrivilege> privileges, IEnumerable<MetaProperty> properties)
        {
            Entity = entity;
            EntitySetName = entitySetName;
            PrimaryIdAttribute = primaryIdAttribute;
            PrimaryNameAttribute = primaryNameAttribute;
            OwnershipType = ownershipType;
            Attributes = attributes;
            Relationships = relationships;
            Keys = keys;
            Privileges = privileges;
            Properties = properties;
        }
    }
}
