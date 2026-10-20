using System;
using System.Security.Cryptography;
using System.Text;

namespace DiamondMine.Simulation
{
    // Detects accidental corruption. This is an unkeyed checksum, not authentication:
    // a player controlling the device can replace the payload and recompute it.
    public static class SaveEnvelope
    {
        public static string Wrap(string payload)
        {
            var json = new JsonComposer();
            json.BeginObject();
            json.Prop("saveFormat", 1);
            json.Prop("sha256", Digest(payload));
            json.Prop("payload", payload);
            json.EndObject();
            return json.ToString();
        }

        public static string Unwrap(string text)
        {
            JsonValue root = JsonValue.Parse(text);
            if (!root.Has("saveFormat"))
                return text; // Existing v0/v1 saves migrate on the next successful write.
            int format = root.Int("saveFormat", 0);
            if (format > 1)
                throw new NewerSaveException("This save container needs a newer game version.");
            if (format != 1)
                throw new InvalidSaveException("Unsupported save container.");
            string payload = root.Str("payload", "");
            if (!string.Equals(Digest(payload), root.Str("sha256", ""), StringComparison.Ordinal))
                throw new InvalidSaveException("Save checksum does not match.");
            return payload;
        }

        static string Digest(string payload)
        {
            using (SHA256 hash = SHA256.Create())
                return BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(payload))).Replace("-", "").ToLowerInvariant();
        }
    }
}
