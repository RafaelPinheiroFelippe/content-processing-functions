using System.Linq;
using FluentResults;
using Microsoft.Extensions.Logging;

namespace ContentProcessingFunctions.Infrastructure;

public class ResultLogger : IResultLogger
{
    private readonly ILogger _logger;

    public ResultLogger(ILogger logger)
    {
        _logger = logger;
    }
    
    public void Log(string context, string content, ResultBase result, LogLevel logLevel)
    {
        _logger.Log(logLevel, $"Result Log -- Message: {string.Join(", ", result.Reasons.Select(x => x.Message))}. Context: {context}.");
    }

    public void Log<TContext>(string content, ResultBase result, LogLevel logLevel)
    {
        _logger.Log(logLevel, $"Result Log -- Message: {string.Join(", ", result.Reasons.Select(x => x.Message))}. Context: {typeof(TContext)}.");
    }
}