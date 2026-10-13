using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

// PROTOTYPE CODE: един проверен llama.cpp endpoint; без API keys или remote fallback.
public sealed class LocalNpcModelAdapter : ILocalNpcModel
{
    public const string Endpoint = "http://127.0.0.1:8080/v1/chat/completions";
    public const string ModelId = "detective-npc-prototype";
    private static readonly HttpClient Client = new HttpClient(new HttpClientHandler
        { UseProxy = false, AllowAutoRedirect = false }) { Timeout = TimeSpan.FromSeconds(60) };

    [Serializable] private sealed class Payload
    {
        public string model = ModelId;
        public NpcMessage[] messages;
        public float temperature = 0.2f;
        public int max_tokens = 320;
        public bool stream = false;
    }
    [Serializable] private sealed class Response { public Choice[] choices = null; }
    [Serializable] private sealed class Choice
    { public NpcMessage message = null; public string finish_reason = null; }

    public async Task<string> ReplyAsync(NpcMessage[] messages, CancellationToken cancellation)
    {
        var json = JsonUtility.ToJson(new Payload { messages = messages });
        using (var body = new StringContent(json, Encoding.UTF8, "application/json"))
        using (var response = await Client.PostAsync(Endpoint, body, cancellation))
        {
            response.EnsureSuccessStatusCode();
            string text = await response.Content.ReadAsStringAsync();
            cancellation.ThrowIfCancellationRequested();
            var decoded = JsonUtility.FromJson<Response>(text);
            if (decoded?.choices == null || decoded.choices.Length != 1
                || decoded.choices[0].finish_reason != "stop"
                || decoded.choices[0].message == null || decoded.choices[0].message.role != "assistant")
                throw new InvalidOperationException("Непълен или невалиден отговор от модела.");
            return decoded.choices[0].message.content;
        }
    }
}
