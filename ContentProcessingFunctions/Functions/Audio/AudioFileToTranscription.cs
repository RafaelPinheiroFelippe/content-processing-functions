using System;
using System.IO;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using ContentProcessingFunctions.Infrastructure;
using ContentProcessingFunctions.Models;
using ContentProcessingFunctions.Services;
using FluentResults;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace ContentProcessingFunctions.Functions.Audio;

public static class AudioFileToTranscription
{
    private static StorageService _storageService;
    private static TranscriptionService _transcriptionService;

    [FunctionName("AudioFileToTranscription")]
    public static async Task RunAsync(
        [BlobTrigger("audiocontainer/{name}", Connection = "AzureWebJobsStorage")]
        Stream audioBlobStream,
        string name,
        ILogger log,
        ExecutionContext context)
    {
        log.LogInformation(
            $"--C# Blob trigger function processed audio blob\n Name:{name} \n Size: {audioBlobStream.Length} Bytes");

        if (!ValidateBlob(audioBlobStream, log)) return;

        SetupResultLogger(log);
        
        log.LogInformation($"--Secrets Setup...");
        SetupSecrets();

        log.LogInformation($"--Index Setup...");
        await _storageService.SetupFileNameIndexAsync();

        if (await FileAlreadyTranscribed(name))
        {
            log.LogWarning($"--FileName {name} already exists in collection");
            return;
        }

        log.LogInformation($"--Chunking...");
        var audioChunksResult = await new AudioService(context).ChunkAudioAsync(audioBlobStream, 10);
        if (audioChunksResult.IsFailed) return;

        log.LogInformation($"--Transcribing...");
        var transcriptionChunksResult = await _transcriptionService
            .TranscribeAudioChunksAsync(audioChunksResult.Value);
        if (transcriptionChunksResult.IsFailed) return;

        log.LogInformation($"--Storing...");
        var storeResult = await _storageService
            .StoreTranscriptionAsync(new FullTranscription(name, transcriptionChunksResult.Value));
        if (storeResult.IsFailed) return;

        log.LogInformation(
            $"--C# Blob trigger function succeeded.");
    }

    private static async Task<bool> FileAlreadyTranscribed(string name)
    {
        return (await _storageService.FileNameExistsAsync(name)).ValueOrDefault;
    }

    private static void SetupSecrets()
    {
        try
        {
            var secretClient = new SecretClient(
                vaultUri: new Uri("https://main-content-generation.vault.azure.net/"),
                credential: new DefaultAzureCredential());
            
            _storageService = new StorageService(secretClient);
            _transcriptionService = new TranscriptionService(secretClient);
        }
        catch (Exception e)
        {
            Result.Fail(e.Message).Log(LogLevel.Error);
        }
    }

    private static bool ValidateBlob(Stream audioBlobStream, ILogger log)
    {
        if (audioBlobStream.Length == 0)
        {
            log.LogInformation($"Function processed an error: No file uploaded.");
            return false;
        }

        return true;
    }

    private static void SetupResultLogger(ILogger log)
    {
        Result.Setup(cfg => { cfg.Logger = new ResultLogger(log); });
    }
}