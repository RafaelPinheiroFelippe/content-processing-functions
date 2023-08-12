using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure.Security.KeyVault.Secrets;
using ContentProcessingFunctions.Models;
using FaunaDB.Client;
using FaunaDB.Types;
using FluentResults;
using Microsoft.Extensions.Logging;
using static FaunaDB.Query.Language;
using static FluentResults.Result;
using Result = FluentResults.Result;

namespace ContentProcessingFunctions.Services;

public class StorageService
{
    private readonly FaunaClient _faunaClient;
    private readonly string _transcriptionsCollectionName;
    private readonly string _fileNameIndex;

    public StorageService(SecretClient secretClient)
    {
        KeyVaultSecret faunaKeySecret = secretClient.GetSecret("FaunaKey");
        _faunaClient = new FaunaClient(secret: faunaKeySecret.Value);

        _fileNameIndex = Environment.GetEnvironmentVariable("FaunaTranscriptionsFileNameIndex");
        _transcriptionsCollectionName = Environment.GetEnvironmentVariable("FaunaTranscriptionsCollection");
    }

    public async Task<Result> StoreTranscriptionAsync(FullTranscription transcription)
    {
        try
        {
            await _faunaClient.Query(
                expression: Create(
                    Collection(_transcriptionsCollectionName),
                    Obj("data", Encoder.Encode(transcription))
                )
            );

            return Ok();
        }
        catch (Exception e)
        {
            return Fail(e.Message).Log<StorageService>(LogLevel.Error);
        }
    }

    public async Task<Result<bool>> FileNameExistsAsync(string fileName)
    {
        try
        {
            var nameExists = await _faunaClient.Query(
                Exists(
                    Match(
                        Index(_fileNameIndex),
                        fileName
                    )
                )
            );

            return nameExists.To<bool>().Value;
        }
        catch (Exception e)
        {
            return Fail(e.Message).Log<StorageService>(LogLevel.Error);
        }
    }

    public async Task SetupFileNameIndexAsync()
    {
        try
        {
            if (!await IndexExists())
            {
                await _faunaClient.Query(
                    CreateIndex(
                        Obj(
                            "name", _fileNameIndex,
                            "source", Collection(_transcriptionsCollectionName),
                            "terms", Arr(Obj("field", Arr("data", nameof(FullTranscription.FileName))))
                        )
                    )
                );
            }
        }
        catch (Exception e)
        {
            Fail(e.Message).Log<StorageService>(LogLevel.Error);
        }

        return;

        async Task<bool> IndexExists()
        {
            var result = await _faunaClient.Query(
                Paginate(Indexes())
            );

            var data = result.At("data").To<List<RefV>>().Value;
            var indexExists = data.Any(refV => refV.Id == _fileNameIndex);
            return indexExists;
        }
    }
}