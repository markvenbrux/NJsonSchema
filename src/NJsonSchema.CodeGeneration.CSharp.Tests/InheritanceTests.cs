using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NJsonSchema.CodeGeneration.CSharp;
using NJsonSchema.Converters;
using NJsonSchema.Generation;
using Xunit;

namespace NJsonSchema.CodeGeneration.Tests.CSharp
{
    public class InheritanceTests
    {
        public class MyContainer
        {
            public EmptyClassInheritingDictionary CustomDictionary { get; set; }
        }

        /// <summary>
        /// Foobar.
        /// </summary>
        public sealed class EmptyClassInheritingDictionary : Dictionary<string, object>
        {
        }

        [Fact]
        public async void When_empty_class_inherits_from_dictionary_then_allOf_inheritance_still_works()
        {
            //// Arrange
            var schema = JsonSchema.FromType<MyContainer>();
            var data = schema.ToJson();

            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings());

            //// Act
            var code = generator.GenerateFile();

            //// Assert
            var dschema = schema.Definitions["EmptyClassInheritingDictionary"];

            Assert.Equal(0, dschema.AllOf.Count);
            Assert.True(dschema.IsDictionary);
            Assert.Contains("Foobar.", data);
            Assert.Contains("Foobar.", code);

            Assert.DoesNotContain("class CustomDictionary :", code);
            Assert.Contains("public EmptyClassInheritingDictionary CustomDictionary", code);
            Assert.Contains("public partial class EmptyClassInheritingDictionary : System.Collections.Generic.Dictionary<string, object>", code);
        }

        [KnownType(typeof(MyException))]
        [JsonConverter(typeof(JsonInheritanceConverter), "kind")]
        public class ExceptionBase : Exception
        {
            public string Foo { get; set; }
        }

        /// <summary>
        /// Foobar.
        /// </summary>
        public class MyException : ExceptionBase
        {
            public string Bar { get; set; }
        }

        public class ExceptionContainer
        {
            public ExceptionBase Exception { get; set; }
        }
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


        [JsonConverter(typeof(JsonInheritanceConverter), "discriminator")]
        [KnownType(typeof(Label))]
        [KnownType(typeof(LabelSet))]
        public abstract class LabelBase {
            public bool IsCreatedByReader { get; set; }
            public string Id { get; set; }
        }

        public class Label : LabelBase {
            public string MeasurementType { get; set; }
            public string Color { get; set; }
        }

        public class LabelSet : LabelBase {
            public Collection<LabelBase> Labels { get; set; }
        }

        public class Task {
            public LabelSet LabelSet { get; set; }
            public string Id { get; set; }
        }



        [Fact]
        public async void LabelHierarchy() {
            var outputPath = @"..\..\..\..\NJsonSchema.InheritanceDemo\";

            var jsonSchemaGeneratorSettings = new JsonSchemaGeneratorSettings() {
                SchemaType = SchemaType.JsonSchema,
            };

            // Select root type for schema generation
            var schemaRootType = typeof(Task);
          
            // Generic generation of schema and serialization code
            var schema = JsonSchema.FromType(schemaRootType, jsonSchemaGeneratorSettings);
            var schemaData = schema.ToJson();
            File.WriteAllText(outputPath + $"{schemaRootType.Name}.schema.json", schemaData);

                
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings {
                JsonLibrary = CSharpJsonLibrary.SystemTextJson,
                ClassStyle = CSharpClassStyle.Record,
                HandleReferences = true,
                Namespace = "Philips.MyNamespace",
                SchemaType = SchemaType.JsonSchema

            });
            var code = generator.GenerateFile();
            File.WriteAllText(outputPath + $"{schemaRootType.Name}.cs", code);
        
        }


        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////



        [Fact]
        public async void When_definitions_inherit_from_root_schema()
        {
            //// Arrange
            var path = GetTestDirectory() + "/References/Animal.json";

            //// Act
            var schema = await JsonSchema.FromFileAsync(path);
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings { ClassStyle = CSharpClassStyle.Record });

            //// Act
            var code = generator.GenerateFile();

            //// Assert
            Assert.Contains("public abstract partial class Animal", code);
            Assert.Contains("public partial class Cat : Animal", code);
            Assert.Contains("public partial class PersianCat : Cat", code);
            Assert.Contains("[JsonInheritanceAttribute(\"Cat\", typeof(Cat))]", code);
            Assert.Contains("[JsonInheritanceAttribute(\"PersianCat\", typeof(PersianCat))]", code);
        }

        private static string GetTestDirectory()
        {
            var codeBase = Assembly.GetExecutingAssembly().CodeBase;
            var uri = new UriBuilder(codeBase);
            return Path.GetDirectoryName(Uri.UnescapeDataString(uri.Path));
        }
    }
}
