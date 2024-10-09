using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Kesa.Japanese.ThirdParty.Anki;

public class AnkiConnectClient
{
    private HttpClient _client;

    public AnkiConnectClient()
    {
        _client = new HttpClient();
    }

    public Task<AnkiConnectResponse<TResponse>> MakeRequestAsync<TResponse>(string action)
        => MakeRequestAsync<TResponse>(new AnkiConnectRequest() { Action = action });

    public Task<AnkiConnectResponse<TResponse>> MakeRequestAsync<TResponse>(string action, string paramKey, object paramValue)
        => MakeRequestAsync<TResponse>(new AnkiConnectRequest(action, paramKey, paramValue));

    public async Task<AnkiConnectResponse<TResponse>> MakeRequestAsync<TResponse>(AnkiConnectRequest request)
    {
        var url = $"http://127.0.0.1:8765/";
        var json = JsonSerializer.Serialize(request, new JsonSerializerOptions() { WriteIndented = true });
        var response = await _client.PostAsync(url, new StringContent(json));
        return await response.Content.ReadFromJsonAsync<AnkiConnectResponse<TResponse>>();
    }

    public Task<AnkiConnectResponse<AnkiConnectResponse<TResponse>[]>> MakeMultiRequestAsync<TResponse>(AnkiConnectRequest[] requests)
    {
        var multiRequest = new AnkiConnectRequest()
        {
            Action = "multi",
            Params = new Dictionary<string, object>()
        {
            { "actions", requests }
        }
        };

        return MakeRequestAsync<AnkiConnectResponse<TResponse>[]>(multiRequest);
    }

    public async Task<AnkiConnectResponse<string[]>> GetDeckNamesAsync()
    {
        return await MakeRequestAsync<string[]>("deckNames");
    }

    public async Task<AnkiConnectResponse<Dictionary<string, int>>> GetDeckNamesAndIdsAsync()
    {
        return await MakeRequestAsync<Dictionary<string, int>>("deckNamesAndIds");
    }

    public async Task<AnkiConnectResponse<int>> CreateDeckAsync(string deckName)
    {
        return await MakeRequestAsync<int>("createDeck", "deck", deckName);
    }
}

public class AnkiConnectRequest
{
    [JsonPropertyName("action")]
    public string Action { get; set; }

    [JsonPropertyName("version")]
    public int Version { get; set; } = 6;

    [JsonPropertyName("params")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, object> Params { get; set; }

    public AnkiConnectRequest() { }

    public AnkiConnectRequest(string action, string paramKey, object paramValue)
    {
        Action = action;

        if (paramKey != null && paramValue != null)
        {
            Params = new Dictionary<string, object>()
        {
            { paramKey, paramValue },
        };
        }
    }
}

public class AnkiConnectUpdateNoteRequest
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("fields")]
    public Dictionary<string, string> Fields { get; set; } = [];

    [JsonPropertyName("tags")]
    public List<string> Tags { get; set; } = [];
}

public class AnkiConnectAddNoteRequest
{
    [JsonPropertyName("deckName")]
    public string DeckName { get; set; }

    [JsonPropertyName("modelName")]
    public string ModelName { get; set; }

    [JsonPropertyName("fields")]
    public Dictionary<string, string> Fields { get; set; }
}

public class AnkiConnectResponse<T>
{
    [JsonPropertyName("result")]
    public T Result { get; set; }

    [JsonPropertyName("error")]
    public string Error { get; set; }
}

public class AnkiConnectNoteInfo
{
    [JsonPropertyName("noteId")]
    public long NoteId { get; set; }

    [JsonPropertyName("tags")]
    public List<string> Tags { get; set; }

    [JsonPropertyName("fields")]
    public Dictionary<string, AnkiConnectFieldInfo> Fields { get; set; }

    [JsonPropertyName("modelName")]
    public string ModelName { get; set; }

    [JsonPropertyName("cards")]
    public long[] Cards { get; set; }
}

public class AnkiConnectCardInfo
{
    [JsonPropertyName("cardId")]
    public long CardId { get; set; }

    [JsonPropertyName("fields")]
    public Dictionary<string, AnkiConnectFieldInfo> Fields { get; set; }

    [JsonPropertyName("fieldOrder")]
    public int FieldOrder { get; set; }

    [JsonPropertyName("question")]
    public string Question { get; set; }

    [JsonPropertyName("answer")]
    public string Answer { get; set; }

    [JsonPropertyName("modelName")]
    public string ModelName { get; set; }

    [JsonPropertyName("ord")]
    public int Ord { get; set; }

    [JsonPropertyName("deckName")]
    public string DeckName { get; set; }

    [JsonPropertyName("css")]
    public string Css { get; set; }

    [JsonPropertyName("factor")]
    public int Factor { get; set; }

    [JsonPropertyName("interval")]
    public int Interval { get; set; }

    [JsonPropertyName("note")]
    public long Note { get; set; }

    [JsonPropertyName("type")]
    public int Type { get; set; }

    [JsonPropertyName("queue")]
    public int Queue { get; set; }

    [JsonPropertyName("due")]
    public int Due { get; set; }

    [JsonPropertyName("reps")]
    public int Reps { get; set; }

    [JsonPropertyName("lapses")]
    public int Lapses { get; set; }

    [JsonPropertyName("left")]
    public int Left { get; set; }

    [JsonPropertyName("mod")]
    public int Mod { get; set; }
}

public class AnkiConnectFieldInfo
{
    [JsonPropertyName("order")]
    public int Order { get; set; }

    [JsonPropertyName("value")]
    public string Value { get; set; }
}
