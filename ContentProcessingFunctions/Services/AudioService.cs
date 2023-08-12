using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using ContentProcessingFunctions.Models;
using FFmpeg.NET;
using FluentResults;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace ContentProcessingFunctions.Services;

public class AudioService
{
    private string FfmpegPath { get; }

    public AudioService(ExecutionContext context)
    {
        FfmpegPath = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? Path.Combine(context.FunctionAppDirectory, "Infrastructure", "ffmpeg", "windows", "ffmpeg.exe")
            : Path.Combine(context.FunctionAppDirectory, "Infrastructure", "ffmpeg", "linux", "ffmpeg");
    }

    public async Task<Result<List<AudioChunk>>> ChunkAudioAsync(Stream audioStream, int minutesPerChunk = 5)
    {
        try
        {
            List<AudioChunk> chunks = new();

            var tempAudioPath = await SaveAudioToTempFile(audioStream);

            var engine = new Engine(FfmpegPath);
            var inputFile = new InputFile(tempAudioPath);
            var totalDuration = (await engine.GetMetaDataAsync(inputFile, default)).Duration;

            var segmentNumber = 1;
            for (var start = TimeSpan.Zero; start < totalDuration; start += TimeSpan.FromMinutes(minutesPerChunk))
            {
                var end = start + TimeSpan.FromMinutes(minutesPerChunk);
                if (end > totalDuration) end = totalDuration;

                var conversionOptions = new ConversionOptions
                {
                    Seek = start,
                    ExtraArguments = $"-t {end - start} -f mp3"
                };

                var chunkData = await ExtractChunkData(engine, inputFile, conversionOptions);

                chunks.Add(new AudioChunk($"segment_{segmentNumber}.mp3", chunkData, segmentNumber));

                segmentNumber++;
            }

            File.Delete(tempAudioPath);
            return chunks;
        }
        catch (Exception e)
        {
            return Result.Fail(e.Message).Log<AudioService>(LogLevel.Error);
        }
    }

    private static async Task<byte[]> ExtractChunkData(Engine engine, InputFile inputFile,
        ConversionOptions conversionOptions)
    {
        var tempOutputPath = Path.GetTempFileName().Replace(".tmp", ".mp3");
        var outputFile = new OutputFile(tempOutputPath);

        await engine.ConvertAsync(inputFile, outputFile, conversionOptions, default);
        var sliceData = await File.ReadAllBytesAsync(tempOutputPath);
        
        File.Delete(tempOutputPath);
        return sliceData;
    }

    private static async Task<string> SaveAudioToTempFile(Stream audioStream)
    {
        var tempAudioPath = Path.GetTempFileName();

        await using var fileStream = File.Create(tempAudioPath);
        audioStream.Seek(0, SeekOrigin.Begin);
        await audioStream.CopyToAsync(fileStream);

        return tempAudioPath;
    }
}