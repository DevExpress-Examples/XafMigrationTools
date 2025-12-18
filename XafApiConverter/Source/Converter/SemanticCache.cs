using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Text;

namespace XafApiConverter.Converter {
    class SemanticCache {
        readonly Dictionary<string, Item> _cache = new Dictionary<string, Item>();

        public void Add(string fileName, SemanticModel semanticModel, SyntaxTree syntaxTree, Document document) {
            _cache[fileName] = new Item(semanticModel, syntaxTree, document);
        }

        public Item TryGetValue(string fileName) {
            return _cache.GetValueOrDefault(fileName);
        }

        public class Item {
            public readonly SemanticModel SemanticModel;
            public readonly SyntaxTree SyntaxTree;
            public readonly Microsoft.CodeAnalysis.Document Document;
            public Item(SemanticModel semanticModel, SyntaxTree syntaxTree, Document document) {
                SemanticModel = semanticModel;
                SyntaxTree = syntaxTree;
                Document = document;
            }
        }
    }
}
