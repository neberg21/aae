using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Module.Agents.Nostr;

using System.Net.WebSockets;
using System.Text;

public class ListenOnMessages : BackgroundService
{
    private readonly ILogger<ListenOnMessages> _logger;

    // Nostr nutzt standardmäßig sichere WebSockets (wss://)
    private readonly Uri _relayUri = new("wss://nostr.neberg.de");

    public ListenOnMessages(ILogger<ListenOnMessages> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ListenOnMessages gestartet.");

        // Die äußere Schleife sorgt dafür, dass sich der Service bei einem Verbindungsabbruch neu verbindet.
        while (!stoppingToken.IsCancellationRequested)
        {
            using var ws = new ClientWebSocket();
            try
            {
                _logger.LogInformation("Verbinde mit {Relay}...", _relayUri);
                await ws.ConnectAsync(_relayUri, stoppingToken);
                _logger.LogInformation("Erfolgreich verbunden!");

                // Subscription ID generieren (kann ein beliebiger String sein)
                string subId = Guid.NewGuid().ToString("N");

                // Wir senden einen Request ("REQ") an den Relay.
                // Der leere Filter {} sagt dem Node: Abonniere ALLES.
                string reqMessage = $"[\"REQ\", \"{subId}\", {{}}]";
                var reqBytes = Encoding.UTF8.GetBytes(reqMessage);

                await ws.SendAsync(
                    new ArraySegment<byte>(reqBytes),
                    WebSocketMessageType.Text,
                    true,
                    stoppingToken);

                _logger.LogInformation("Subscription gesendet. Warte auf Events...");

                // Puffer für eingehende Nachrichten (8 KB ist meistens ausreichend)
                var buffer = new byte[8192];

                // Nachrichten empfangen, solange die Verbindung offen ist
                while (ws.State == WebSocketState.Open && !stoppingToken.IsCancellationRequested)
                {
                    using var ms = new MemoryStream();
                    WebSocketReceiveResult result;

                    // Lese die Nachricht komplett (falls sie größer als der Puffer ist)
                    do
                    {
                        result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), stoppingToken);
                        ms.Write(buffer, 0, result.Count);
                    } while (!result.EndOfMessage);

                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var jsonMessage = Encoding.UTF8.GetString(ms.ToArray());
                        ProcessMessage(jsonMessage);
                    }
                    else if (result.MessageType == WebSocketMessageType.Close)
                    {
                        _logger.LogWarning("Verbindung vom Relay geschlossen.");
                        await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Wird geworfen, wenn der Service beendet wird. Alles in Ordnung.
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler in der WebSocket-Verbindung. Versuche Reconnect in 5 Sekunden...");
            }

            // 5 Sekunden warten, bevor ein neuer Versuch gestartet wird (verhindert Spamming)
            if (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        _logger.LogInformation("ListenOnMessages wird beendet.");
    }

    private void ProcessMessage(string jsonMessage)
    {
        // Nostr-Nachrichten sind JSON-Arrays.
        // Ein Event sieht so aus: ["EVENT", "subId", { "id": "...", "pubkey": "...", "content": "..." }]
        // Ein "End of Stored Events" sieht so aus: ["EOSE", "subId"]

        if (jsonMessage.StartsWith("[\"EVENT\""))
        {
            _logger.LogInformation("Neues Nostr-Event empfangen:\n{Message}", jsonMessage);

            // Hier kannst du System.Text.Json verwenden, um die Nachricht als Objekt zu deserialisieren
            // und z.B. in eine Datenbank zu schreiben.
        }
        else if (jsonMessage.StartsWith("[\"EOSE\""))
        {
            _logger.LogInformation("Alle alten Events geladen. Lausche jetzt auf neue Echtzeit-Events...");
        }
        else
        {
            // Andere Nachrichten (z.B. OK oder NOTICE)
            _logger.LogDebug("Andere Nachricht empfangen: {Message}", jsonMessage);
        }
    }
}