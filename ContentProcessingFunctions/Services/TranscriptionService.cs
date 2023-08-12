using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using ContentProcessingFunctions.Models;
using FluentResults;
using Microsoft.Extensions.Logging;

namespace ContentProcessingFunctions.Services;

public class TranscriptionService
{
    private const string SpeechToTextModel = "whisper-1";
    private const string SpeechToTextUrl = "https://api.openai.com/v1/audio/transcriptions";
    
    private readonly string _openAiToken;
    private readonly HttpClient _httpClient = new(); 

    public TranscriptionService(SecretClient secretClient)
    {
        KeyVaultSecret openAiTokenSecret = secretClient.GetSecret("openAiKey");
        _openAiToken = openAiTokenSecret.Value;
        
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_openAiToken}");
    }

    public async Task<Result<List<TranscriptionChunk>>> TranscribeAudioChunksAsync(List<AudioChunk> audioChunks)
    {
        try
        {
            var transcriptions = new List<TranscriptionChunk>();
            
            foreach (var audioChunk in audioChunks)
            {
                var transcription = await TranscribeChunkAsync(audioChunk.Data);
                transcriptions.Add(new TranscriptionChunk(audioChunk.Name, transcription, audioChunk.Order));
            }

            return transcriptions;
        }
        catch (Exception e)
        {
            return Result.Fail(e.Message).Log<TranscriptionService>(LogLevel.Error);
        }
    }

    private async Task<string> TranscribeChunkAsync(byte[] data)
    {
        using var formData = new MultipartFormDataContent();
        await using var memoryStream = new MemoryStream(data);

        var fileContent = new StreamContent(memoryStream);
        formData.Add(fileContent, "file", "audiofile.mp3"); 
        formData.Add(new StringContent(SpeechToTextModel), "model");
        formData.Add(new StringContent("text"), "response_format");

        var response = await _httpClient.PostAsync(SpeechToTextUrl, formData);
        var responseBody = await response.Content.ReadAsStringAsync();

        return responseBody;
    }

}