using System;
using System.ComponentModel.Composition;

namespace ChromExplorer.Infrastructure.Interfaces
{
    public interface ExactTypeMetadata
    {
        Type ExactType { get; }
    }

    [MetadataAttribute]
    public class ExactTypeMetadataAttribute : Attribute
    {

        public Type ExactType { get; private set; }

        public ExactTypeMetadataAttribute(Type exactType)
        {
            this.ExactType = exactType;
        }
    }
}
