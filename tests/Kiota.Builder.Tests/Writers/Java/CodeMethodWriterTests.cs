using System;
using System.IO;
using System.Linq;
using Kiota.Builder.Extensions;
using Kiota.Builder.Tests;
using Xunit;

namespace Kiota.Builder.Writers.Java.Tests {
    public class CodeMethodWriterTests : IDisposable {
        private const string defaultPath = "./";
        private const string defaultName = "name";
        private readonly StringWriter tw;
        private readonly LanguageWriter writer;
        private readonly CodeMethod method;
        private readonly CodeClass parentClass;
        private const string methodName = "methodName";
        private const string returnTypeName = "Somecustomtype";
        private const string methodDescription = "some description";
        private const string paramDescription = "some parameter description";
        private const string paramName = "paramName";
        public CodeMethodWriterTests()
        {
            writer = LanguageWriter.GetLanguageWriter(GenerationLanguage.Java, defaultPath, defaultName);
            tw = new StringWriter();
            writer.SetTextWriter(tw);
            var root = CodeNamespace.InitRootNamespace();
            parentClass = new CodeClass(root) {
                Name = "parentClass"
            };
            root.AddClass(parentClass);
            method = new CodeMethod(parentClass) {
                Name = methodName,
            };
            method.ReturnType = new CodeType(method) {
                Name = returnTypeName
            };
            parentClass.AddMethod(method);
        }
        public void Dispose()
        {
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
        private void AddRequestBodyParameters() {
            var stringType = new CodeType(method) {
                Name = "string",
            };
            method.AddParameter(new CodeParameter(method) {
                Name = "h",
                ParameterKind = CodeParameterKind.Headers,
                Type = stringType,
            });
            method.AddParameter(new CodeParameter(method){
                Name = "q",
                ParameterKind = CodeParameterKind.QueryParameter,
                Type = stringType,
            });
            method.AddParameter(new CodeParameter(method){
                Name = "b",
                ParameterKind = CodeParameterKind.RequestBody,
                Type = stringType,
            });
            method.AddParameter(new CodeParameter(method){
                Name = "r",
                ParameterKind = CodeParameterKind.ResponseHandler,
                Type = stringType,
            });
        }
        [Fact]
        public void WritesNullableVoidTypeForExecutor(){
            method.MethodKind = CodeMethodKind.RequestExecutor;
            method.HttpMethod = HttpMethod.Get;
            method.ReturnType = new CodeType(method) {
                Name = "void",
            };
            writer.Write(method);
            var result = tw.ToString();
            Assert.Contains("CompletableFuture<Void>", result);
            AssertExtensions.CurlyBracesAreClosed(result);
        }
        [Fact]
        public void WritesRequestBodiesThrowOnNullHttpMethod() {
            method.MethodKind = CodeMethodKind.RequestExecutor;
            Assert.Throws<InvalidOperationException>(() => writer.Write(method));
            method.MethodKind = CodeMethodKind.RequestGenerator;
            Assert.Throws<InvalidOperationException>(() => writer.Write(method));
        }
        [Fact]
        public void WritesRequestExecutorBody() {
            method.MethodKind = CodeMethodKind.RequestExecutor;
            method.HttpMethod = HttpMethod.Get;
            AddRequestBodyParameters();
            writer.Write(method);
            var result = tw.ToString();
            Assert.Contains("final RequestInfo requestInfo", result);
            Assert.Contains("sendAsync", result);
            Assert.Contains("CompletableFuture.failedFuture(ex)", result);
            AssertExtensions.CurlyBracesAreClosed(result);
        }
        [Fact]
        public void WritesRequestGeneratorBody() {
            method.MethodKind = CodeMethodKind.RequestGenerator;
            method.HttpMethod = HttpMethod.Get;
            AddRequestBodyParameters();
            writer.Write(method);
            var result = tw.ToString();
            Assert.Contains("final RequestInfo requestInfo = new RequestInfo()", result);
            Assert.Contains("httpMethod = HttpMethod.GET", result);
            Assert.Contains("h.accept(requestInfo.headers)", result);
            Assert.Contains("AddQueryParameters", result);
            Assert.Contains("setContentFromParsable", result);
            Assert.Contains("return requestInfo;", result);
            AssertExtensions.CurlyBracesAreClosed(result);
        }
        [Fact]
        public void WritesInheritedDeSerializerBody() {
            method.MethodKind = CodeMethodKind.Deserializer;
            method.IsAsync = false;
            AddSerializationProperties();
            AddInheritanceClass();
            writer.Write(method);
            var result = tw.ToString();
            Assert.Contains("super.methodName()", result);
            AssertExtensions.CurlyBracesAreClosed(result);
        }
        [Fact]
        public void WritesDeSerializerBody() {
            var parameter = new CodeParameter(method){
                Description = paramDescription,
                Name = paramName
            };
            parameter.Type = new CodeType(parameter) {
                Name = "string"
            };
            method.MethodKind = CodeMethodKind.Deserializer;
            method.IsAsync = false;
            AddSerializationProperties();
            writer.Write(method);
            var result = tw.ToString();
            Assert.Contains("getStringValue", result);
            Assert.Contains("getCollectionOfPrimitiveValues", result);
            Assert.Contains("getCollectionOfObjectValues", result);
            Assert.Contains("getEnumValue", result);
            AssertExtensions.CurlyBracesAreClosed(result);
        }
        [Fact]
        public void WritesInheritedSerializerBody() {
            method.MethodKind = CodeMethodKind.Serializer;
            method.IsAsync = false;
            AddSerializationProperties();
            AddInheritanceClass();
            writer.Write(method);
            var result = tw.ToString();
            Assert.Contains("super.serialize", result);
            AssertExtensions.CurlyBracesAreClosed(result);
        }
        [Fact]
        public void WritesSerializerBody() {
            var parameter = new CodeParameter(method){
                Description = paramDescription,
                Name = paramName
            };
            parameter.Type = new CodeType(parameter) {
                Name = "string"
            };
            method.MethodKind = CodeMethodKind.Serializer;
            method.IsAsync = false;
            AddSerializationProperties();
            writer.Write(method);
            var result = tw.ToString();
            Assert.Contains("writeStringValue", result);
            Assert.Contains("writeCollectionOfPrimitiveValues", result);
            Assert.Contains("writeCollectionOfObjectValues", result);
            Assert.Contains("writeEnumValue", result);
            Assert.Contains("writeAdditionalData(this.getAdditionalData());", result);
            AssertExtensions.CurlyBracesAreClosed(result);
        }
        [Fact]
        public void WritesMethodAsyncDescription() {
            
            method.Description = methodDescription;
            var parameter = new CodeParameter(method){
                Description = paramDescription,
                Name = paramName
            };
            parameter.Type = new CodeType(parameter) {
                Name = "string"
            };
            method.AddParameter(parameter);
            writer.Write(method);
            var result = tw.ToString();
            Assert.Contains("/**", result);
            Assert.Contains(methodDescription, result);
            Assert.Contains("@param ", result);
            Assert.Contains(paramName, result);
            Assert.Contains(paramDescription, result); 
            Assert.Contains("@return a CompletableFuture of", result);
            Assert.Contains("*/", result);
            AssertExtensions.CurlyBracesAreClosed(result);
        }
        [Fact]
        public void WritesMethodSyncDescription() {
            
            method.Description = methodDescription;
            method.IsAsync = false;
            var parameter = new CodeParameter(method){
                Description = paramDescription,
                Name = paramName
            };
            parameter.Type = new CodeType(parameter) {
                Name = "string"
            };
            method.AddParameter(parameter);
            writer.Write(method);
            var result = tw.ToString();
            Assert.DoesNotContain("@return a CompletableFuture of", result);
            AssertExtensions.CurlyBracesAreClosed(result);
        }
        [Fact]
        public void Defensive() {
            var codeMethodWriter = new CodeMethodWriter(new JavaConventionService(), false);
            Assert.Throws<ArgumentNullException>(() => codeMethodWriter.WriteCodeElement(null, writer));
            Assert.Throws<ArgumentNullException>(() => codeMethodWriter.WriteCodeElement(method, null));
            var originalParent = method.Parent;
            method.Parent = CodeNamespace.InitRootNamespace();
            Assert.Throws<InvalidOperationException>(() => codeMethodWriter.WriteCodeElement(method, writer));
            method.Parent = originalParent;
            method.ReturnType = null;
            Assert.Throws<InvalidOperationException>(() => codeMethodWriter.WriteCodeElement(method, writer));
        }
        [Fact]
        public void ThrowsIfParentIsNotClass() {
            method.Parent = CodeNamespace.InitRootNamespace();
            Assert.Throws<InvalidOperationException>(() => writer.Write(method));
        }
        [Fact]
        public void ThrowsIfReturnTypeIsMissing() {
            method.ReturnType = null;
            Assert.Throws<InvalidOperationException>(() => writer.Write(method));
        }
        private const string taskPrefix = "CompletableFuture<";
        [Fact]
        public void WritesReturnType() {
            writer.Write(method);
            var result = tw.ToString();
            Assert.Contains($"{taskPrefix}{returnTypeName}> {methodName}", result);// async default
            AssertExtensions.CurlyBracesAreClosed(result);
        }
        [Fact]
        public void DoesNotAddAsyncInformationOnSyncMethods() {
            method.IsAsync = false;
            writer.Write(method);
            var result = tw.ToString();
            Assert.DoesNotContain(taskPrefix, result);
            AssertExtensions.CurlyBracesAreClosed(result);
        }
        [Fact]
        public void WritesPublicMethodByDefault() {
            writer.Write(method);
            var result = tw.ToString();
            Assert.Contains("public ", result);// public default
            AssertExtensions.CurlyBracesAreClosed(result);
        }
        [Fact]
        public void WritesPrivateMethod() {
            method.Access = AccessModifier.Private;
            writer.Write(method);
            var result = tw.ToString();
            Assert.Contains("private ", result);
            AssertExtensions.CurlyBracesAreClosed(result);
        }
        [Fact]
        public void WritesProtectedMethod() {
            method.Access = AccessModifier.Protected;
            writer.Write(method);
            var result = tw.ToString();
            Assert.Contains("protected ", result);
            AssertExtensions.CurlyBracesAreClosed(result);
        }
        [Fact]
        public void WritesIndexer() {
            method.MethodKind = CodeMethodKind.IndexerBackwardCompatibility;
            method.PathSegment = "somePath";
            writer.Write(method);
            var result = tw.ToString();
            Assert.Contains("httpCore", result);
            Assert.Contains("pathSegment", result);
            Assert.Contains("+ id", result);
            Assert.Contains("return new", result);
            Assert.Contains(method.PathSegment, result);
        }
        [Fact]
        public void WritesGetterToBackingStore() {
            parentClass.AddBackingStoreProperty();
            method.AddAccessedProperty();
            method.MethodKind = CodeMethodKind.Getter;
            writer.Write(method);
            var result = tw.ToString();
            Assert.Contains("this.getBackingStore().get(\"someProperty\")", result);
        }
        [Fact]
        public void WritesGetterToBackingStoreWithNonnullProperty() {
            method.AddAccessedProperty();
            parentClass.AddBackingStoreProperty();
            method.AccessedProperty.Type = new CodeType(method.AccessedProperty) {
                Name = "string",
                IsNullable = false,
            };
            var defaultValue = "someDefaultValue";
            method.AccessedProperty.DefaultValue = defaultValue; 
            method.MethodKind = CodeMethodKind.Getter;
            writer.Write(method);
            var result = tw.ToString();
            Assert.Contains("if(value == null)", result);
            Assert.Contains(defaultValue, result);
        }
        [Fact]
        public void WritesSetterToBackingStore() {
            parentClass.AddBackingStoreProperty();
            method.AddAccessedProperty();
            method.MethodKind = CodeMethodKind.Setter;
            writer.Write(method);
            var result = tw.ToString();
            Assert.Contains("this.getBackingStore().set(\"someProperty\", value)", result);
        }
        [Fact]
        public void WritesGetterToField() {
            method.AddAccessedProperty();
            method.MethodKind = CodeMethodKind.Getter;
            writer.Write(method);
            var result = tw.ToString();
            Assert.Contains("this.someProperty", result);
        }
        [Fact]
        public void WritesSetterToField() {
            method.AddAccessedProperty();
            method.MethodKind = CodeMethodKind.Setter;
            writer.Write(method);
            var result = tw.ToString();
            Assert.Contains("this.someProperty = value", result);
        }
        [Fact]
        public void WritesConstructor() {
            method.MethodKind = CodeMethodKind.Constructor;
            var defaultValue = "someVal";
            var propName = "propWithDefaultValue";
            parentClass.AddProperty(new CodeProperty(parentClass) {
                Name = propName,
                DefaultValue = defaultValue,
                PropertyKind = CodePropertyKind.PathSegment,
            });
            writer.Write(method);
            var result = tw.ToString();
            Assert.Contains(parentClass.Name.ToFirstCharacterUpperCase(), result);
            Assert.Contains($"this.{propName} = {defaultValue}", result);
        }
        [Fact]
        public void WritesApiConstructor() {
            method.MethodKind = CodeMethodKind.ClientConstructor;
            var coreProp = parentClass.AddProperty(new CodeProperty(parentClass) {
                Name = "core",
                PropertyKind = CodePropertyKind.HttpCore,
            }).First();
            coreProp.Type = new CodeType(coreProp) {
                Name = "HttpCore",
                IsExternal = true,
            };
            method.AddParameter(new CodeParameter(method) {
                Name = "core",
                ParameterKind = CodeParameterKind.HttpCore,
                Type = coreProp.Type,
            });
            method.DeserializerModules = new() {"com.microsoft.kiota.serialization.Deserializer"};
            method.SerializerModules = new() {"com.microsoft.kiota.serialization.Serializer"};
            writer.Write(method);
            var result = tw.ToString();
            Assert.Contains(parentClass.Name.ToFirstCharacterUpperCase(), result);
            Assert.Contains("registerDefaultSerializer", result);
            Assert.Contains("registerDefaultDeserializer", result);
        }
        [Fact]
        public void WritesApiConstructorWithBackingStore() {
            method.MethodKind = CodeMethodKind.ClientConstructor;
            var coreProp = parentClass.AddProperty(new CodeProperty(parentClass) {
                Name = "core",
                PropertyKind = CodePropertyKind.HttpCore,
            }).First();
            coreProp.Type = new CodeType(coreProp) {
                Name = "HttpCore",
                IsExternal = true,
            };
            method.AddParameter(new CodeParameter(method) {
                Name = "core",
                ParameterKind = CodeParameterKind.HttpCore,
                Type = coreProp.Type,
            });
            var tempWriter = LanguageWriter.GetLanguageWriter(GenerationLanguage.Java, defaultPath, defaultName, true);
            tempWriter.SetTextWriter(tw);
            tempWriter.Write(method);
            var result = tw.ToString();
            Assert.Contains("enableBackingStore", result);
        }
    }
}
