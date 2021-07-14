using System;
using System.IO;
using System.Linq;
using Kiota.Builder.Tests;
using Xunit;

namespace Kiota.Builder.Writers.CSharp.Tests {
    public class CodePropertyWriterTests: IDisposable {
        private const string defaultPath = "./";
        private const string defaultName = "name";
        private readonly StringWriter tw;
        private readonly LanguageWriter writer;
        private readonly CodeProperty property;
        private readonly CodeClass parentClass;
        private const string propertyName = "PropertyName";
        private const string propertyDescription = "some description";
        private const string typeName = "Somecustomtype";
        public CodePropertyWriterTests() {
            writer = LanguageWriter.GetLanguageWriter(GenerationLanguage.CSharp, defaultPath, defaultName);
            tw = new StringWriter();
            writer.SetTextWriter(tw);
            var root = CodeNamespace.InitRootNamespace();
            parentClass = new CodeClass(root) {
                Name = "parentClass"
            };
            root.AddClass(parentClass);
            property = new CodeProperty(parentClass) {
                Name = propertyName,
            };
            property.Type = new CodeType(property) {
                Name = typeName
            };
            parentClass.AddProperty(property);
        }
        public void Dispose() {
            tw?.Dispose();
        }
        private void AddSerializationProperties() {
            var addData = parentClass.AddProperty(new CodeProperty(parentClass) {
                Name = "additionalData",
                PropertyKind = CodePropertyKind.AdditionalData,
            }).First();
            addData.Type = new CodeType(addData) {
                Name = "string"
            };
            var dummyProp = parentClass.AddProperty(new CodeProperty(parentClass) {
                Name = "dummyProp",
            }).First();
            dummyProp.Type = new CodeType(dummyProp) {
                Name = "string"
            };
            var dummyCollectionProp = parentClass.AddProperty(new CodeProperty(parentClass) {
                Name = "dummyColl",
            }).First();
            dummyCollectionProp.Type = new CodeType(dummyCollectionProp) {
                Name = "string",
                CollectionKind = CodeTypeBase.CodeTypeCollectionKind.Array,
            };
            var dummyComplexCollection = parentClass.AddProperty(new CodeProperty(parentClass) {
                Name = "dummyComplexColl"
            }).First();
            dummyComplexCollection.Type = new CodeType(dummyComplexCollection) {
                Name = "Complex",
                CollectionKind = CodeTypeBase.CodeTypeCollectionKind.Array,
                TypeDefinition = new CodeClass(parentClass.Parent) {
                    Name = "SomeComplexType"
                }
            };
            var dummyEnumProp = parentClass.AddProperty(new CodeProperty(parentClass){
                Name = "dummyEnumCollection",
            }).First();
            dummyEnumProp.Type = new CodeType(dummyEnumProp) {
                Name = "SomeEnum",
                TypeDefinition = new CodeEnum(parentClass.Parent) {
                    Name = "EnumType"
                }
            };
        }
        private void AddInheritanceClass() {
            (parentClass.StartBlock as CodeClass.Declaration).Inherits = new CodeType(parentClass) {
                Name = "someParentClass"
            };
        }
        [Fact]
        public void WritesRequestBuilder() {
            property.PropertyKind = CodePropertyKind.RequestBuilder;
            writer.Write(property);
            var result = tw.ToString();
            Assert.Contains("get =>", result);
            Assert.Contains($"new {typeName}", result);
            Assert.Contains("HttpCore", result);
            Assert.Contains("PathSegment", result);
        }
        [Fact]
        public void WritesCustomProperty() {
            property.PropertyKind = CodePropertyKind.Custom;
            writer.Write(property);
            var result = tw.ToString();
            Assert.Contains($"{typeName} {propertyName}", result);
            Assert.Contains("get; set;", result);
        }
        [Fact]
        public void WritesPrivateSetter() {
            property.PropertyKind = CodePropertyKind.Custom;
            property.ReadOnly = true;
            writer.Write(property);
            var result = tw.ToString();
            Assert.Contains("get; private set;", result);
        }
        [Fact]
        public void MapsCustomPropertiesToBackingStore() {
            parentClass.AddBackingStoreProperty();
            property.PropertyKind = CodePropertyKind.Custom;
            writer.Write(property);
            var result = tw.ToString();
            Assert.Contains("get { return BackingStore?.Get<Somecustomtype>(nameof(PropertyName)); }", result);
            Assert.Contains("set { BackingStore?.Set(nameof(PropertyName), value);", result);
        }
        [Fact]
        public void MapsAdditionalDataPropertiesToBackingStore() {
            parentClass.AddBackingStoreProperty();
            property.PropertyKind = CodePropertyKind.AdditionalData;
            writer.Write(property);
            var result = tw.ToString();
            Assert.Contains("get { return BackingStore?.Get<Somecustomtype>(nameof(PropertyName)); }", result);
            Assert.Contains("set { BackingStore?.Set(nameof(PropertyName), value);", result);
        }
    }
}
