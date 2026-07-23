using System.Text.Json;
using NBitcoin.Secp256k1;
using NNostr.Client;

namespace Module.AI.Nostr;

public class NostrEventService
{
    public async Task<NostrEvent> PublishProfile(NostrKeyPair keyPair, string name)
    {
        var (privateKey, pubKeyHex) = keyPair;

        var metadata = new { name };
        var contentJson = JsonSerializer.Serialize(metadata);
        var metadataEvent = new NostrEvent
        {
            Kind = 0,
            PublicKey = pubKeyHex,
            Content = contentJson,
            CreatedAt = DateTimeOffset.UtcNow
        };
        var signingKey = ECPrivKey.Create(privateKey.ToBytes());

        await metadataEvent.ComputeIdAndSignAsync(signingKey);

        return metadataEvent;
    }
}