using Newtonsoft.Json;

namespace Solutions.SaveSystem.Structures
{
    public struct SerializationMetadata
    {
        [JsonProperty("t")]
        public string[] Types;
        [JsonProperty("g")]
        public string[] Glossary;
    }
}