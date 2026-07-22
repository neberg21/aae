using System.Security.Cryptography;
using System.Text.Json;
using NBitcoin.Secp256k1;
using NNostr.Client;

namespace Module.Agents.Nostr;

public class NostrProfile
{
    public required string PublicKeyHex { get; set; }
    public required string PrivateKeyHex { get; set; }
    public required string Name { get; set; }
    public required NostrEvent MetadataEvent { get; set; }
}

public class ProfileGenerator
{
    public static async Task<NostrProfile> CreateProfileAsync(string desiredName)
    {
        // 1. Sichere Zufallszahlen für den Private Key generieren (32 Byte)
        var privateKeyBytes = new byte[32];
        RandomNumberGenerator.Fill(privateKeyBytes);

        // 2. Private Key Objekt erstellen
        if (!ECPrivKey.TryCreate(privateKeyBytes, out var privateKey))
        {
            throw new InvalidOperationException("Fehler bei der Private-Key-Generierung.");
        }

        // 3. Den Nostr-spezifischen Public Key (X-Only) ableiten
        var pubKey = privateKey.CreateXOnlyPubKey();

        // Hex-Strings erzeugen (Nostr erwartet Hex in Kleinbuchstaben)
        string privKeyHex = Convert.ToHexString(privateKeyBytes).ToLower();
        string pubKeyHex = Convert.ToHexString(pubKey.ToBytes()).ToLower();

        // 3. Metadaten-Inhalt erstellen
        // Kind 0 erwartet ein JSON-Objekt als String im Content-Feld
        var metadata = new { name = desiredName };
        var contentJson = JsonSerializer.Serialize(metadata);

        // 4. Das Nostr Event (Kind 0) zusammenbauen
        var metadataEvent = new NostrEvent
        {
            Kind = 0,
            PublicKey = pubKeyHex,
            Content = contentJson,
            CreatedAt = DateTimeOffset.UtcNow
        };

        // 5. Event-ID berechnen und mit dem Private Key signieren
        // Die Bibliothek serialisiert das Event, hasht es mit SHA256 und erstellt eine Schnorr-Signatur.
        var signingKey = ECPrivKey.Create(privateKey.ToBytes());
        await metadataEvent.ComputeIdAndSignAsync(signingKey);

        // 6. Das fertige Objekt zurückgeben
        return new NostrProfile
        {
            PublicKeyHex = pubKeyHex,
            PrivateKeyHex = privKeyHex,
            Name = desiredName,
            MetadataEvent = metadataEvent
        };
    }
}