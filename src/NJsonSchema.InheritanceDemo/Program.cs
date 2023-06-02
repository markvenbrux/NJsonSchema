using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Philips.NJsonSchemaInheritanceDemo {
    internal static class Program {
        static void Main() {
            var jsonSerializerOptions = new JsonSerializerOptions {
                WriteIndented = true,
                TypeInfoResolver = new DefaultJsonTypeInfoResolver(),

            };
            var ls2Labels = new Collection<MyNamespace.LabelBase>() {
                    new MyNamespace.Label("red", "LS1", false, "ContourSet"),
                    new MyNamespace.Label("green", "LS2", false, "ContourSet"),
                };
            var labels = new Collection<MyNamespace.LabelBase>() {
                    new MyNamespace.Label("red", "L1", false, "ContourSet"),
                    new MyNamespace.LabelSet("L2", false, ls2Labels),
                };
            var labelSet = new MyNamespace.LabelSet(
                "LS1",
                false,
                labels
                );

            var myObject = new MyNamespace.Task("BiotelTask", labelSet);


            var myObjectType = myObject.GetType();
            var myObjectName = myObjectType.Name;

            string jsonString = JsonSerializer.Serialize(myObject, myObjectType, jsonSerializerOptions);
            File.WriteAllText($"{myObjectName}.json", jsonString);
            var deserializedJson = JsonSerializer.Deserialize(jsonString, myObjectType, jsonSerializerOptions);
        }
    }
}
