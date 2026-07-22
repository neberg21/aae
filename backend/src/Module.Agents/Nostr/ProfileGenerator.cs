namespace Module.Agents.Nostr;

public class ProfileGenerator
{
    private readonly NostrEventService _nostrEventService;

    public ProfileGenerator(NostrEventService nostrEventService)
    {
        _nostrEventService = nostrEventService;
    }

    public async Task<NostrProfile> CreateProfileAsync(NostrKeyPair keyPair, string desiredName)
    {
        var metadataEvent = await _nostrEventService.PublishProfile(keyPair, desiredName);

        return new NostrProfile
        {
            PublicKeyHex = keyPair.PublicKeyHex,
            Name = desiredName,
            MetadataEvent = metadataEvent
        };
    }
}