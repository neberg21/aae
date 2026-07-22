using NNostr.Client;

namespace Module.Agents.Nostr;

public record NostrProfile
{
    public required string PublicKeyHex { get; init; }
    public required string Name { get; init; }
    public required NostrEvent MetadataEvent { get; init; }
}