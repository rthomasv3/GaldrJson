using Microsoft.CodeAnalysis;

namespace GaldrJson.SourceGeneration
{
    internal class PropertyInfo
    {
        public string Name { get; set; } = "";
        public string Type { get; set; } = "";
        public ITypeSymbol TypeSymbol { get; set; } = null;
        public string JsonName { get; set; } = "";
        public bool CanWrite { get; set; }
    }
}
