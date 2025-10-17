using System;
using System.IO;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHttpClient();
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

var app = builder.Build();

app.MapPost("/hashtags", async (HttpContext context, HttpClient http, ILogger<Program> logger) =>
{
    context.Request.EnableBuffering();

    using var sr = new StreamReader(context.Request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
    var raw = await sr.ReadToEndAsync();
    context.Request.Body.Position = 0;

    logger.LogInformation("----- RAW BODY RECEBIDO (length={len}) -----", raw?.Length ?? 0);
    logger.LogInformation(raw is null or "" ? "(vazio)" : (raw.Length > 1000 ? raw[..1000] + "..." : raw));

    if (string.IsNullOrWhiteSpace(raw))
    {
        return Results.BadRequest(new
        {
            error = "Corpo da requisição vazio. Verifique Content-Type, linha em branco no .http e se o cliente realmente enviou o body.",
            rawLength = 0
        });
    }

    HashtagRequest? request;
    try
    {
        request = JsonSerializer.Deserialize<HashtagRequest>(raw, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }
    catch (JsonException jex)
    {
        return Results.BadRequest(new
        {
            error = "JSON inválido no corpo da requisição.",
            detail = jex.Message,
            preview = raw.Length > 500 ? raw[..500] : raw,
            rawLength = raw.Length
        });
    }

    if (request is null || string.IsNullOrWhiteSpace(request.Text))
        return Results.BadRequest(new { error = "Campo 'text' é obrigatório." });

    try
    {
        int count = request.Count is null ? 10 : Math.Min(request.Count.Value, 30);
        string modelName = string.IsNullOrWhiteSpace(request.Model) ? "llama3.2:3b" : request.Model;

        var prompt = $@"
Gere exatamente {count} hashtags originais e relevantes para o texto abaixo.
Responda apenas em JSON no formato:
{{ ""hashtags"": [""#exemplo1"", ""#exemplo2""] }}

Texto: {request.Text}";

        var ollamaRequest = new
        {
            model = modelName,
            prompt = prompt,
            stream = false,
            format = "json"
        };

        var response = await http.PostAsJsonAsync("http://localhost:11434/api/generate", ollamaRequest);
        if (!response.IsSuccessStatusCode)
            return Results.BadRequest(new { error = "Erro ao comunicar com Ollama.", status = (int)response.StatusCode });

        var result = await response.Content.ReadFromJsonAsync<OllamaResponse>();
        if (result?.Response is null)
            return Results.BadRequest(new { error = "Resposta vazia do modelo." });

        var json = JsonDocument.Parse(result.Response);
        var hashtags = json.RootElement.GetProperty("hashtags").EnumerateArray()
            .Select(e => e.GetString() ?? "")
            .Where(h => !string.IsNullOrWhiteSpace(h))
            .Select(h => h.Trim())
            .Distinct()
            .Take(count)
            .ToArray();

        return Results.Ok(new { model = modelName, count = hashtags.Length, hashtags });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Erro interno");
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.Run();

record HashtagRequest(string Text, int? Count, string? Model);
record OllamaResponse(string Model, string CreatedAt, string Response);
