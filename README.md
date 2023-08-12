# content-processing-functions

## Audio Processing Function

### Overview

This serverless function takes an audio file, slices it using ffmpeg, calls OpenAI's Whisper API to transcribe the audio content, and stores the transcription in FaunaDB. It is hosted on Azure and triggered by a blob.

### Architecture

#### Components

- **Azure Functions**: Serverless compute service that automatically scales.
- **Azure Blob Storage**: Used for storing and triggering audio file processing.
- **FFmpeg**: A complete, cross-platform solution to record, convert, and stream audio and video.
- **Whisper**: OpenAI's automatic speech recognition (ASR) system, is used in this function to transcribe the audio content from the sliced audio files.

#### Workflow

1. Audio file uploaded to Blob Storage.
2. Azure Function triggered by the new blob.
3. Audio file sliced using FFmpeg.
4. API called to transcribe the audio.
5. Result stored in FaunaDB collection.

### Requirements

- Azure KeyVault Access
- FFmpeg binaries
- .NET 6.0
- FaunaDB
- OpenAI Account

### Configuration

Configuration is handled through Azure Key Vault for secrets and `local.settings.json` for local development.

#### Fmpeg

WIP

#### FaunaDB

The index passed into the configuration `FaunaTranscriptionsFileNameIndex` will be created automatically if it doesn't exist.

#### Secrets

- `AzureWebJobsStorage`: Connection string to Azure Storage Account.
- `FaunaKey`: Key for storing the transcriptions on FaunaDB.
- `openAiKey`: Key for accessing the Whisper API.

#### Env Variables

The `local.settings.json` file is used for local development and should contain the following keys:

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "<connection_string>",
    "FaunaTranscriptionsCollection": "COLLECTION_WHERE_TRANSCRIPTIONS_WILL_BE_STORED",
    "FaunaTranscriptionsFileNameIndex": "INDEX_FOR_FILENAME_QUERIES"
  }
}
