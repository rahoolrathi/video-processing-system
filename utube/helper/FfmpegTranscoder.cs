using System.Diagnostics;
using System.Text;
using utube.DTOs;
using utube.Enums;

namespace utube.helper
{
    public class FfmpegTranscoder
    {
        public async Task TranscodeToCmafAsync(string inputPath, string outputDir, EncodingProfileWithFormatsDto profile)
        {
            Directory.CreateDirectory(outputDir);

            var resolutions = profile.Resolutions.Split(',');
            var bitrates = profile.BitratesKbps.Split(',');

            var encryptionKey = "0123456789abcdef0123456789abcdef"; // 32-char hex
            var keyId = "89abcdef-0123-4567-89ab-cdef01234567";     // UUID

            // Prepare output for DASH or HLS individually
            foreach (var format in profile.Formats)
            {
                var outputSubDir = Path.Combine(outputDir, format.FormatType.ToString());
                Directory.CreateDirectory(outputSubDir);

                var arguments = new StringBuilder();
                arguments.Append($"-y -i \"{inputPath}\" ");

                for (int i = 0; i < resolutions.Length; i++)
                {
                    string res = resolutions[i].Trim();
                    string bitrate = bitrates[i].Trim();

                    arguments.Append($"-map 0:v:0 -b:v:{i} {bitrate}k -s:v:{i} {res} ");
                }

                arguments.Append($"-encryption_scheme cenc-aes-ctr ");
                arguments.Append($"-encryption_key {encryptionKey} ");
                arguments.Append($"-encryption_kid {keyId} ");

                if (format.FormatType == FormatType.DASH )
                {
                    var dashOutputDir = Path.Combine(outputSubDir, "dash");
                    Directory.CreateDirectory(dashOutputDir);

                    var dashArgs = new StringBuilder();
                    dashArgs.Append($"-y -i \"{inputPath}\" ");
                    for (int i = 0; i < resolutions.Length; i++)
                    {
                        string res = resolutions[i].Trim();
                        string bitrate = bitrates[i].Trim();
                        dashArgs.Append($"-map 0:v:0 -b:v:{i} {bitrate}k -s:v:{i} {res} ");
                    }

                    dashArgs.Append($"-encryption_scheme cenc-aes-ctr ");
                    dashArgs.Append($"-encryption_key {encryptionKey} ");
                    dashArgs.Append($"-encryption_kid {keyId} ");
                    dashArgs.Append($"-f dash -seg_duration 4 -use_template 1 -use_timeline 1 ");
                    dashArgs.Append($"-init_seg_name {Path.Combine(dashOutputDir, "init-stream$RepresentationID$.mp4")} ");
                    dashArgs.Append($"-media_seg_name {Path.Combine(dashOutputDir, "chunk-stream$RepresentationID$-$Number$.m4s")} ");
                    dashArgs.Append($"\"{Path.Combine(dashOutputDir, "manifest.mpd")}\"");


                    await RunFfmpegAsync(dashArgs.ToString(), FormatType.DASH);
                }

                if (format.FormatType == FormatType.HLS )
                {
                    var hlsOutputDir = Path.Combine(outputSubDir, "hls");
                    Directory.CreateDirectory(hlsOutputDir);

                    var hlsArgs = new StringBuilder();
                    hlsArgs.Append($"-y -i \"{inputPath}\" ");
                    for (int i = 0; i < resolutions.Length; i++)
                    {
                        string res = resolutions[i].Trim();
                        string bitrate = bitrates[i].Trim();
                        hlsArgs.Append($"-map 0:v:0 -b:v:{i} {bitrate}k -s:v:{i} {res} ");
                    }

                    hlsArgs.Append($"-encryption_scheme cenc-aes-ctr ");
                    hlsArgs.Append($"-encryption_key {encryptionKey} ");
                    hlsArgs.Append($"-encryption_kid {keyId} ");
                    hlsArgs.Append($"-f hls -hls_time 4 -hls_playlist_type vod ");
                    hlsArgs.Append($"\"{Path.Combine(hlsOutputDir, "master.m3u8")}\"");

                    await RunFfmpegAsync(hlsArgs.ToString(), FormatType.HLS);
                }



            }

        }

        private async Task RunFfmpegAsync(string arguments, FormatType formatType)
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
        }

    }
}
