using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace ContentProcessingFunctions.Functions.HealthCheck;

public static class FfmpegVersionCheck
{
    [FunctionName("FfmpegVersionCheck")]
    public static async Task<IActionResult> RunAsync(
        [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log)
    {
        log.LogInformation("C# HTTP trigger function processed a request.");

        var ffmpegLocalPath = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) != null 
            ? "ffmpeg\\windows\\ffmpeg.exe"
            : "ffmpeg/linux/ffmpeg";
        
        var ffmpegAbsolutePath = Path.Combine(Directory.GetCurrentDirectory(), ffmpegLocalPath);
        
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = ffmpegAbsolutePath,
                Arguments = "-version",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            }
        };
        process.Start();
        var output = await process.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync();

        return new OkObjectResult($"FFmpeg version: {output}");
    }
}