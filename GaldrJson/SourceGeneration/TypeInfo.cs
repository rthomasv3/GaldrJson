using System.Collections.Generic;
using System.Reflection;

namespace GaldrJson.SourceGeneration
{
    internal class TypeInfo
    {
        public string FullName { get; set; } = "";
        public string Name { get; set; } = "";
        public string Namespace { get; set; } = "";
        public string ConverterName { get; set; } = "";
        public string FieldName { get; set; } = "";
        public List<PropertyInfo> Properties { get; set; } = new List<PropertyInfo>();
    }
}
