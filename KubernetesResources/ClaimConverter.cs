using System;
using System.Security.Claims;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace IdentityServer4K8S.KubernetesResources
{
    public class ClaimConverter : JsonConverter<Claim>
    {
        public override Claim ReadJson(JsonReader reader, Type objectType, Claim existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var obj = JToken.ReadFrom(reader);
            return new Claim((string)obj["type"],(string)obj["type"], (string)obj["type"], (string)obj["type"], (string)obj["type"]);
        }

        public override void WriteJson(JsonWriter writer, Claim value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
        public override bool CanWrite => false;
    }
}