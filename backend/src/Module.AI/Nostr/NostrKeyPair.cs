using System.Security.Cryptography;
using NBitcoin.Secp256k1;

namespace Module.AI.Nostr;

public class NostrKeyPair
{
    private NostrKeyPair(ECPrivKey privateKey, ECXOnlyPubKey publicKey, string privateKeyHex, string publicKeyHex)
    {
        PrivateKey = privateKey;
        PublicKey = publicKey;
        PrivateKeyHex = privateKeyHex;
        PublicKeyHex = publicKeyHex;
    }

    public ECPrivKey PrivateKey { get; }
    public ECXOnlyPubKey PublicKey { get; }
    public string PrivateKeyHex { get; }
    public string PublicKeyHex { get; }

    public static NostrKeyPair GenerateKeyPair()
    {
        var privateKeyBytes = new byte[32];
        RandomNumberGenerator.Fill(privateKeyBytes);

        return GenerateKeyPairFromPrivateKey(privateKeyBytes);
    }

    public void Deconstruct(out ECPrivKey privateKey, out string pubKeyHex)
    {
        privateKey = PrivateKey;
        pubKeyHex = PublicKeyHex;
    }

    public static NostrKeyPair ParseKeyPair(string privateKeyHex)
    {
        var privateKeyBytes = Convert.FromHexString(privateKeyHex);
        return GenerateKeyPairFromPrivateKey(privateKeyBytes);
    }

    private static NostrKeyPair GenerateKeyPairFromPrivateKey(byte[] privateKeyBytes)
    {
        // 2. Private Key Objekt erstellen
        if (!ECPrivKey.TryCreate(privateKeyBytes, out var privateKey))
        {
            throw new InvalidOperationException("Fehler bei der Private-Key-Generierung.");
        }

        // 3. Den Nostr-spezifischen Public Key (X-Only) ableiten
        var pubKey = privateKey.CreateXOnlyPubKey();

        // Hex-Strings erzeugen (Nostr erwartet Hex in Kleinbuchstaben)
        var privKeyHex = Convert.ToHexString(privateKeyBytes).ToLower();
        var pubKeyHex = Convert.ToHexString(pubKey.ToBytes()).ToLower();

        return new NostrKeyPair(privateKey, pubKey, privKeyHex, pubKeyHex);
    }
}