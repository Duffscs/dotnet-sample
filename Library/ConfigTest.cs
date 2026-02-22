using Library;
using Test;
using System.Xml.Serialization;

[GenerateConfigValidation]
public class Config {
    public int Port { get; set; }
}

namespace Library {
    [GenerateConfigValidation]
    [XmlRoot]
    public class AppConfig {
        [XmlAttribute]
        public int Port { get; set; }
        [XmlAttribute]
        public int? Port2 { get; set; }
        [XmlAttribute]
        public bool? Enabled { get; set; }
        [XmlElement]
        public Nested? Nested { get; set; } = new();
        public Config config { get; set; } = new Config();
    }

    [GenerateConfigValidation]
    public class Nested {
        [XmlAttribute]
        public string Name { get; set; } = "";
        [XmlAttribute]
        public int Value { get; set; }
        [XmlArray]
        [XmlArrayItem(ElementName = "Item")]
        public List<Nested2> Items { get; set; } = [];
    }

}

namespace Test {
    [GenerateConfigValidation]
    public class Nested2 {
        [XmlAttribute]
        public string Description { get; set; } = "";
        [XmlElement]
        public List<List<List<string>>> DeepStrings { get; set; } = [];
    }
}
