using System.Collections.Generic;
using System.Linq;

namespace ContentProcessingFunctions.Models;

public class FullTranscription
{
    public string FileName { get; }
    public string Content { get; }

    public FullTranscription(string fileName, IEnumerable<TranscriptionChunk> chunks)
    {
        FileName = fileName;
        Content = string.Join("", chunks.OrderBy(chunk => chunk.Order).Select(chunk => chunk.Content));
    }
}