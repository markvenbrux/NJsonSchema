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


            string labelJsonString = JsonSerializer.Serialize<MyNamespace.LabelSet>(labelSet, jsonSerializerOptions);
            File.WriteAllText(@"Label.json", labelJsonString);
            var deserializedLabelSet = JsonSerializer.Deserialize<MyNamespace.LabelBase>(labelJsonString, jsonSerializerOptions);
        }
    }
}
