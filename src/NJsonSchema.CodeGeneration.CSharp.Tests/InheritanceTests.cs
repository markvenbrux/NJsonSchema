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
        public async Task When_empty_class_inherits_from_dictionary_then_allOf_inheritance_still_works()
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
            public string Name { get; set; }
        }

        public class Label : LabelBase {
        }

        public class LabelSet : LabelBase {
            public Collection<LabelBase> Labels { get; set; }
        }


        [Fact]
        public async Task LabelHierarchy() {
            //// Arrange
            var jsonSchemaGeneratorSettings = new JsonSchemaGeneratorSettings() {
                SchemaType = SchemaType.JsonSchema,
            };


            var labelBaseSchema = JsonSchema.FromType<LabelBase>(jsonSchemaGeneratorSettings);
            var labelBaseSchemaData = labelBaseSchema.ToJson();
            File.WriteAllText(@"D:\jsonschema\inheritance_demo\NJsonSchemaInheritanceDemo\NJsonSchemaInheritanceDemo\LabelBase.schema.json", labelBaseSchemaData);

            var generator = new CSharpGenerator(labelBaseSchema, new CSharpGeneratorSettings {
                JsonLibrary = CSharpJsonLibrary.SystemTextJson,
                ClassStyle = CSharpClassStyle.Record,
                HandleReferences = true,
                Namespace = "Philips.MyNamespace",
                SchemaType = SchemaType.JsonSchema

            });

            //// Act
            var code = generator.GenerateFile();
            File.WriteAllText(@"D:\jsonschema\inheritance_demo\NJsonSchemaInheritanceDemo\NJsonSchemaInheritanceDemo\Label.cs", code);
        }


        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


        [JsonConverter(typeof(JsonInheritanceConverter), "discriminator")]
        [KnownType(typeof(Cat))]
        public abstract class Animal {
            public string Name { get; set; }
            public Collection<Animal> Children { get; set; }
        }

        [KnownType(typeof(PersianCat))]
        public class Cat : Animal {
        }

        public class PersianCat : Cat {
            public int HairLength { get; set; }
        }


        [Fact]
        public async Task When_class_with_discriminator_has_base_class_then_csharp_is_generated_correctly()
        {
            //// Arrange
            var schema = JsonSchema.FromType<ExceptionContainer>();
            var data = schema.ToJson();



            var jsonSchemaGeneratorSettings = new JsonSchemaGeneratorSettings() { 
                SchemaType = SchemaType.JsonSchema,
            };


            var animalSchema = JsonSchema.FromType<Animal>(jsonSchemaGeneratorSettings);
            var animalSchemaData = animalSchema.ToJson();

            var catSchema = JsonSchema.FromType<Cat>(jsonSchemaGeneratorSettings);
            var catSchemaData = catSchema.ToJson();

            var persianCatSchema = JsonSchema.FromType<PersianCat>(jsonSchemaGeneratorSettings);
            var persianCatSchemaData = persianCatSchema.ToJson();


            var generator = new CSharpGenerator(catSchema, new CSharpGeneratorSettings {
                JsonLibrary = CSharpJsonLibrary.SystemTextJson,
                ClassStyle = CSharpClassStyle.Record,
                HandleReferences = false,
                Namespace = "Philips.MyNamespace",
                SchemaType = SchemaType.JsonSchema
                
            });



            //// Act
            var code = generator.GenerateFile();
            File.WriteAllText(@"D:\jsonschema\inheritance_demo\NJsonSchemaInheritanceDemo\NJsonSchemaInheritanceDemo\Animal.cs", code);

            //// Assert
            Assert.Contains("Foobar.", data);
            Assert.Contains("Foobar.", code);

            Assert.Contains("class ExceptionBase : Exception", code);
            Assert.Contains("class MyException : ExceptionBase", code);
        }

        [Fact]
        public async Task When_property_references_any_schema_with_inheritance_then_property_type_is_correct()
        {
            //// Arrange
            var json = @"{
    ""type"": ""object"",
    ""properties"": {
        ""dog"": {
            ""$ref"": ""#/definitions/Dog""
        }
    },
    ""definitions"": {
        ""Pet"": {
            ""type"": ""object"",
            ""properties"": {
                ""name"": {
                    ""type"": ""string""
                }
            }
        },
        ""Dog"": {
            ""title"": ""Dog"",
            ""description"": """",
            ""allOf"": [
                {
                    ""$ref"": ""#/definitions/Pet""
                },
                {
                    ""type"": ""object""
                }
            ]
        }
    }
}";
            var schema = await JsonSchema.FromJsonAsync(json);
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings { ClassStyle = CSharpClassStyle.Poco });

            //// Act
            var code = generator.GenerateFile();

            //// Assert
            Assert.Contains("public Dog Dog { get; set; }", code);
        }

        [Fact]
        public async Task When_definitions_inherit_from_root_schema()
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
