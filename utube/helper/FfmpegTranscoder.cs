
using System.Diagnostics;
using System.Text;
using utube.DTOs;
using utube.Enums;

namespace utube.helper
{
    public class FfmpegTranscoder
    {
      
       public async Task TranscodeToCmafAsync(
      string inputPath,
      string outputDir,
      EncodingProfileWithFormatsDto profile,
      string encryptionKey,
      string keyId)
        {
            Directory.CreateDirectory(outputDir);

            var resolutions = profile.Resolutions.Split(',');
            var bitrates = profile.BitratesKbps.Split(',');

            foreach (var format in profile.Formats)
            {
                if (format.FormatType == FormatType.DASH)
                {
                    var dashOutputDir = Path.Combine(outputDir, "dash");
                    Directory.CreateDirectory(dashOutputDir);

                    var dashArgs = new StringBuilder();
                    dashArgs.Append($"-y -i \"{inputPath}\" ");

                    for (int i = 0; i < resolutions.Length; i++)
                    {
                        string res = resolutions[i].Trim();
                        string bitrate = bitrates[i].Trim();
                        dashArgs.Append($"-map 0:v:0 -b:v:{i} {bitrate}k -s:v:{i} {res} ");
                    }

                    dashArgs.Append($"-f dash -seg_duration 4 -use_template 1 -use_timeline 1 ");
                   

                    dashArgs.Append("-init_seg_name init-stream$RepresentationID$.mp4 ");
                    dashArgs.Append("-media_seg_name chunk-stream$RepresentationID$-$Number$.m4s ");
                    dashArgs.Append($"\"{Path.Combine(dashOutputDir, "manifest.mpd")}\"");

                    await RunFfmpegAsync(dashArgs.ToString(), FormatType.DASH, dashOutputDir);
                }

                if (format.FormatType == FormatType.HLS)
                {
                    var hlsOutputDir = Path.Combine(outputDir, "hls");
                    Directory.CreateDirectory(hlsOutputDir);

                    var hlsArgs = new StringBuilder();
                    hlsArgs.Append($"-y -i \"{inputPath}\" ");

                    for (int i = 0; i < resolutions.Length; i++)
                    {
                        string res = resolutions[i].Trim();
                        string bitrate = bitrates[i].Trim();
                        hlsArgs.Append($"-map 0:v:0 -b:v:{i} {bitrate}k -s:v:{i} {res} ");
                    }

                    hlsArgs.Append($"-f hls -hls_time 4 -hls_playlist_type vod ");
                    hlsArgs.Append($"\"{Path.Combine(hlsOutputDir, "master.m3u8")}\"");

                    await RunFfmpegAsync(hlsArgs.ToString(), FormatType.HLS, hlsOutputDir);
                }
            }
        }

        private async Task RunFfmpegAsync(string arguments, FormatType formatType, string? outputDir = null)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var errorOutput = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                throw new Exception($"FFmpeg failed for {formatType}: {errorOutput}");
            }
            // Move chunks/init to dash folder if DASH
            if (formatType == FormatType.DASH && outputDir != null)
            {
                await PostProcessDashFilesAsync(outputDir);
            }
        }

        private async Task PostProcessDashFilesAsync(string dashOutputDir)
        {
            string rootDir = Directory.GetCurrentDirectory(); 
            var chunkFiles = Directory.GetFiles(rootDir, "chunk-stream*.m4s");
            var initFiles = Directory.GetFiles(rootDir, "init-stream*.mp4");

            foreach (var file in chunkFiles.Concat(initFiles))
            {
                var destPath = Path.Combine(dashOutputDir, Path.GetFileName(file));
                File.Move(file, destPath, overwrite: true);
            }

            await Task.CompletedTask;
        }

    }
}
