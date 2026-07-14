using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace otosun.Services
{
    public static class ToolService
    {
        public static string GetToolsDir()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "otosun", "tools");
        }

        public static async Task<string> GetYtDlpVersionAsync(string toolsDir)
        {
            var exePath = Path.Combine(toolsDir, "yt-dlp.exe");
            if (!File.Exists(exePath)) return "未インストール";

            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = exePath,
                    Arguments = "--version",
                    WorkingDirectory = toolsDir,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                using var process = new Process { StartInfo = startInfo };
                process.Start();

                var stdoutTask = process.StandardOutput.ReadToEndAsync();
                var stderrTask = process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();

                var output = await stdoutTask + Environment.NewLine + await stderrTask;
                return output.Trim();
            }
            catch
            {
                return "取得失敗";
            }
        }

        public static async Task<string> GetDenoVersionAsync(string toolsDir)
        {
            var exePath = Path.Combine(toolsDir, "deno.exe");
            if (!File.Exists(exePath)) return "未インストール";

            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = exePath,
                    Arguments = "--version",
                    WorkingDirectory = toolsDir,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                using var process = new Process { StartInfo = startInfo };
                process.Start();

                var stdoutTask = process.StandardOutput.ReadToEndAsync();
                var stderrTask = process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();

                var output = await stdoutTask + Environment.NewLine + await stderrTask;
                var match = Regex.Match(output, @"deno\s+([0-9.]+)", RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    return match.Groups[1].Value.Trim();
                }

                var firstLine = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
                return firstLine?.Replace("deno", "").Trim() ?? "インストール済み";
            }
            catch
            {
                return "取得失敗";
            }
        }

        public static async Task<string> GetFfmpegVersionAsync(string toolsDir)
        {
            var exePath = Path.Combine(toolsDir, "ffmpeg.exe");
            if (!File.Exists(exePath)) return "未インストール";

            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = exePath,
                    Arguments = "-version",
                    WorkingDirectory = toolsDir,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                using var process = new Process { StartInfo = startInfo };
                process.Start();

                var stdoutTask = process.StandardOutput.ReadToEndAsync();
                var stderrTask = process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();

                var output = await stdoutTask + Environment.NewLine + await stderrTask;
                var match = Regex.Match(output, @"ffmpeg version\s+([^\s,]+)", RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    return match.Groups[1].Value.Trim();
                }

                var firstLine = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
                if (firstLine != null && firstLine.Contains("ffmpeg version"))
                {
                    return firstLine.Replace("ffmpeg version", "").Split(' ').FirstOrDefault()?.Trim() ?? "インストール済み";
                }
                return "インストール済み";
            }
            catch
            {
                return "取得失敗";
            }
        }

        public static async Task SetupYtDlpAsync(string toolsDir, Action<string> log, Action<string?, double?, bool?> updateProgress, CancellationToken cancellationToken)
        {
            var ytDlpPath = Path.Combine(toolsDir, "yt-dlp.exe");
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/100.0.0.0 Safari/537.36");

            if (!File.Exists(ytDlpPath))
            {
                updateProgress("yt-dlp をダウンロード中...", 0, false);
                log("yt-dlp.exe が見つかりません。最新版をダウンロードしています...");
                await DownloadFileWithProgressAsync(httpClient, "https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp.exe", ytDlpPath, log, updateProgress, cancellationToken);
                log("yt-dlp.exe のダウンロードが完了しました。");
            }
            else
            {
                updateProgress("yt-dlp をアップデート中...", null, true);
                log("yt-dlp.exe のアップデートをチェックしています...");
                await RunProcessAsync(ytDlpPath, "--update", toolsDir, cancellationToken);
                updateProgress(null, null, false);
                log("yt-dlp.exe のアップデートチェックが完了しました。");
            }
        }

        public static async Task SetupDenoAsync(string toolsDir, Action<string> log, Action<string?, double?, bool?> updateProgress, CancellationToken cancellationToken)
        {
            var denoPath = Path.Combine(toolsDir, "deno.exe");
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/100.0.0.0 Safari/537.36");

            if (!File.Exists(denoPath))
            {
                updateProgress("Deno をダウンロード中...", 0, false);
                log("deno.exe が見つかりません。最新版をダウンロードしています...");
                var denoZip = Path.Combine(toolsDir, "deno.zip");
                await DownloadFileWithProgressAsync(httpClient, "https://github.com/denoland/deno/releases/latest/download/deno-x86_64-pc-windows-msvc.zip", denoZip, log, updateProgress, cancellationToken);
                
                updateProgress("Deno を展開中...", null, true);
                log("deno.zip を展開しています...");
                await Task.Run(() => ZipFile.ExtractToDirectory(denoZip, toolsDir, overwriteFiles: true), cancellationToken);
                File.Delete(denoZip);
                updateProgress(null, null, false);
                log("deno.exe の展開が完了しました。");
            }
            else
            {
                updateProgress("Deno をアップデート中...", null, true);
                log("deno.exe のアップデートをチェックしています...");
                await RunProcessAsync(denoPath, "upgrade", toolsDir, cancellationToken);
                updateProgress(null, null, false);
                log("deno.exe のアップデートチェックが完了しました。");
            }
        }

        public static async Task SetupFfmpegAsync(string toolsDir, Action<string> log, Action<string?, double?, bool?> updateProgress, CancellationToken cancellationToken)
        {
            var ffmpegPath = Path.Combine(toolsDir, "ffmpeg.exe");
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/100.0.0.0 Safari/537.36");

            if (!File.Exists(ffmpegPath))
            {
                updateProgress("FFmpeg をダウンロード中...", 0, false);
                log("ffmpeg.exe が見つかりません。Essentialsビルドをダウンロードしています (約100MB)...");
                var ffmpegZip = Path.Combine(toolsDir, "ffmpeg.zip");
                await DownloadFileWithProgressAsync(httpClient, "https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.zip", ffmpegZip, log, updateProgress, cancellationToken);

                updateProgress("FFmpeg を展開中...", null, true);
                log("ffmpeg.zip を展開しています...");
                
                var tempExtractDir = Path.Combine(toolsDir, "ffmpeg_temp");
                if (Directory.Exists(tempExtractDir))
                {
                    Directory.Delete(tempExtractDir, true);
                }
                Directory.CreateDirectory(tempExtractDir);

                await Task.Run(() => ZipFile.ExtractToDirectory(ffmpegZip, tempExtractDir), cancellationToken);

                var ffmpegExeSource = Directory.GetFiles(tempExtractDir, "ffmpeg.exe", SearchOption.AllDirectories).FirstOrDefault();
                var ffprobeExeSource = Directory.GetFiles(tempExtractDir, "ffprobe.exe", SearchOption.AllDirectories).FirstOrDefault();

                if (ffmpegExeSource != null)
                {
                    File.Move(ffmpegExeSource, ffmpegPath, overwrite: true);
                }
                if (ffprobeExeSource != null)
                {
                    File.Move(ffprobeExeSource, Path.Combine(toolsDir, "ffprobe.exe"), overwrite: true);
                }

                try
                {
                    Directory.Delete(tempExtractDir, true);
                    File.Delete(ffmpegZip);
                }
                catch { /* ignore */ }
                updateProgress(null, null, false);
                log("ffmpeg.exe および ffprobe.exe の配置が完了しました。");
            }
        }

        public static async Task<string> SetupToolsAsync(string toolsDir, Action<string> log, Action<string?, double?, bool?> updateProgress, CancellationToken cancellationToken)
        {
            if (!Directory.Exists(toolsDir))
            {
                Directory.CreateDirectory(toolsDir);
            }

            log("専用ツールのセットアップ状況を確認しています...");
            await SetupYtDlpAsync(toolsDir, log, updateProgress, cancellationToken);
            await SetupDenoAsync(toolsDir, log, updateProgress, cancellationToken);
            await SetupFfmpegAsync(toolsDir, log, updateProgress, cancellationToken);
            log("専用ツールのセットアップ状況チェックが完了しました。");

            return toolsDir;
        }

        private static async Task DownloadFileWithProgressAsync(HttpClient httpClient, string url, string destinationPath, Action<string> log, Action<string?, double?, bool?> updateProgress, CancellationToken cancellationToken)
        {
            using var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength;
            using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

            var buffer = new byte[8192];
            var totalReadBytes = 0L;
            int readBytes;

            updateProgress(null, 0, !totalBytes.HasValue);

            while ((readBytes = await contentStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
            {
                await fileStream.WriteAsync(buffer, 0, readBytes, cancellationToken);
                totalReadBytes += readBytes;

                if (totalBytes.HasValue)
                {
                    var progress = (double)totalReadBytes / totalBytes.Value * 100;
                    var totalMb = totalBytes.Value / 1024.0 / 1024.0;
                    var currentMb = totalReadBytes / 1024.0 / 1024.0;
                    var status = $"{Path.GetFileName(destinationPath)} をダウンロード中... {progress:F1}% ({currentMb:F1}MB / {totalMb:F1}MB)";
                    updateProgress(status, progress, false);
                }
            }
        }

        private static async Task RunProcessAsync(string fileName, string arguments, string workingDirectory, CancellationToken cancellationToken)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                WorkingDirectory = workingDirectory,
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using var process = new Process { StartInfo = startInfo };
            process.Start();

            try
            {
                await process.WaitForExitAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                process.Kill(true);
                throw;
            }
        }

        public static async Task<string> GetVideoTitleAsync(string toolsDir, string url, Action<string> log, CancellationToken cancellationToken)
        {
            var ytDlpPath = Path.Combine(toolsDir, "yt-dlp.exe");
            var denoPath = Path.Combine(toolsDir, "deno.exe");

            log($"[yt-dlp] メタ情報の取得を開始: {url}");
            var arguments = $"--js-runtimes \"deno:{denoPath}\" --ffmpeg-location \"{toolsDir}\" --no-playlist --get-title \"{url}\"";

            var startInfo = new ProcessStartInfo
            {
                FileName = ytDlpPath,
                Arguments = arguments,
                WorkingDirectory = toolsDir,
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.GetEncoding(932),
                StandardErrorEncoding = Encoding.GetEncoding(932)
            };

            using var process = new Process { StartInfo = startInfo };
            process.Start();

            var errorLogBuilder = new StringBuilder();
            var readErrorTask = Task.Run(async () =>
            {
                using var reader = process.StandardError;
                while (true)
                {
                    var line = await reader.ReadLineAsync(cancellationToken);
                    if (line == null) break;
                    log($"[yt-dlp-err] {line}");
                    errorLogBuilder.AppendLine(line);
                }
            }, cancellationToken);

            var titleBuilder = new StringBuilder();
            var readOutputTask = Task.Run(async () =>
            {
                using var reader = process.StandardOutput;
                while (true)
                {
                    var line = await reader.ReadLineAsync(cancellationToken);
                    if (line == null) break;
                    log($"[yt-dlp-meta] {line}");
                    titleBuilder.AppendLine(line);
                }
            }, cancellationToken);

            try
            {
                await process.WaitForExitAsync(cancellationToken);
                await Task.WhenAll(readOutputTask, readErrorTask);
            }
            catch (OperationCanceledException)
            {
                try
                {
                    process.Kill(true);
                }
                catch { /* ignore */ }
                throw;
            }

            if (process.ExitCode != 0)
            {
                var errorMsg = errorLogBuilder.ToString();
                throw new Exception($"動画のメタ情報取得に失敗しました。動画が存在しないか、URLが正しくありません。\n{errorMsg}");
            }

            var title = titleBuilder.ToString();
            var trimmedTitle = title.Trim();
            
            if (string.IsNullOrEmpty(trimmedTitle))
            {
                return "download";
            }

            log($"[yt-dlp] メタ情報取得完了 (タイトル: {trimmedTitle})");
            return trimmedTitle;
        }

        public static string SanitizeFileName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                return "download";
            }

            foreach (char c in Path.GetInvalidFileNameChars())
            {
                fileName = fileName.Replace(c.ToString(), "_");
            }
            
            if (fileName.Length > 200)
            {
                fileName = fileName.Substring(0, 200);
            }

            return fileName.Trim();
        }

        public static async Task<string> DownloadVideoAsync(string toolsDir, string url, string tempFileBasePattern, bool isAudioOnly, Action<string> log, Action<double> progressAction, CancellationToken cancellationToken)
        {
            var ytDlpPath = Path.Combine(toolsDir, "yt-dlp.exe");
            var denoPath = Path.Combine(toolsDir, "deno.exe");

            log($"[yt-dlp] ダウンロード処理を開始します (音声のみ: {isAudioOnly})");
            var outputTemplate = tempFileBasePattern + ".%(ext)s";
            var formatSelector = isAudioOnly ? "bestaudio/best" : "bestvideo+bestaudio/best";
            var arguments = $"--js-runtimes \"deno:{denoPath}\" --ffmpeg-location \"{toolsDir}\" --no-playlist -f \"{formatSelector}\" -o \"{outputTemplate}\" \"{url}\"";

            var startInfo = new ProcessStartInfo
            {
                FileName = ytDlpPath,
                Arguments = arguments,
                WorkingDirectory = toolsDir,
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.GetEncoding(932),
                StandardErrorEncoding = Encoding.GetEncoding(932)
            };

            using var process = new Process { StartInfo = startInfo };
            process.Start();

            var progressRegex = new Regex(@"\[download\]\s+([0-9.]+)%", RegexOptions.Compiled);

            var errorLogBuilder = new StringBuilder();
            var readErrorTask = Task.Run(async () =>
            {
                using var reader = process.StandardError;
                while (true)
                {
                    var line = await reader.ReadLineAsync(cancellationToken);
                    if (line == null) break;
                    log($"[yt-dlp-err] {line}");
                    errorLogBuilder.AppendLine(line);
                }
            }, cancellationToken);

            var readOutputTask = Task.Run(async () =>
            {
                using var reader = process.StandardOutput;
                while (true)
                {
                    var line = await reader.ReadLineAsync(cancellationToken);
                    if (line == null) break;

                    log($"[yt-dlp] {line}");

                    var match = progressRegex.Match(line);
                    if (match.Success && double.TryParse(match.Groups[1].Value, out var percent))
                    {
                        progressAction(percent);
                    }
                }
            }, cancellationToken);

            try
            {
                await process.WaitForExitAsync(cancellationToken);
                await Task.WhenAll(readOutputTask, readErrorTask);
            }
            catch (OperationCanceledException)
            {
                try
                {
                    process.Kill(true);
                }
                catch { /* ignore */ }
                throw;
            }

            if (process.ExitCode != 0)
            {
                var errorLog = errorLogBuilder.ToString();
                throw new Exception($"yt-dlp の実行に失敗しました (ExitCode: {process.ExitCode})。\n{errorLog}");
            }

            var directory = Path.GetDirectoryName(tempFileBasePattern) ?? toolsDir;
            var searchPattern = Path.GetFileName(tempFileBasePattern) + ".*";
            var matchFile = Directory.GetFiles(directory, searchPattern).FirstOrDefault();

            if (matchFile == null)
            {
                throw new FileNotFoundException("yt-dlp によってダウンロードされた一時ファイルが見つかりません。");
            }

            return matchFile;
        }

        private static async Task<double> GetVideoDurationAsync(string toolsDir, string sourceFile, Action<string> log, CancellationToken cancellationToken)
        {
            var ffprobePath = Path.Combine(toolsDir, "ffprobe.exe");
            if (!File.Exists(ffprobePath))
            {
                return 0;
            }

            log($"[ffprobe] 動画の再生時間を取得中: {Path.GetFileName(sourceFile)}");
            var arguments = $"-v error -show_entries format=duration -of default=noprint_wrappers=1:nokey=1 \"{sourceFile}\"";
            var startInfo = new ProcessStartInfo
            {
                FileName = ffprobePath,
                Arguments = arguments,
                WorkingDirectory = toolsDir,
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = false
            };

            using var process = new Process { StartInfo = startInfo };
            process.Start();

            var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);

            var output = await outputTask;
            var trimmedOutput = output.Trim();
            log($"[ffprobe] 再生時間取得完了: {trimmedOutput}秒");

            if (double.TryParse(trimmedOutput, out var duration))
            {
                return duration;
            }
            return 0;
        }

        private static async Task<string> GetStreamCodecAsync(string toolsDir, string sourceFile, string streamType, Action<string> log, CancellationToken cancellationToken)
        {
            var ffprobePath = Path.Combine(toolsDir, "ffprobe.exe");
            if (!File.Exists(ffprobePath))
            {
                return string.Empty;
            }

            log($"[ffprobe] コーデック情報を確認中 ({streamType}): {Path.GetFileName(sourceFile)}");
            var arguments = $"-v error -select_streams {streamType} -show_entries stream=codec_name -of default=noprint_wrappers=1:nokey=1 \"{sourceFile}\"";
            var startInfo = new ProcessStartInfo
            {
                FileName = ffprobePath,
                Arguments = arguments,
                WorkingDirectory = toolsDir,
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = false
            };

            using var process = new Process { StartInfo = startInfo };
            process.Start();

            var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);

            var output = await outputTask;
            var codec = output.Trim().ToLowerInvariant();
            log($"[ffprobe] 取得したコーデック ({streamType}): {codec}");
            return codec;
        }

        public static async Task ConvertFileAsync(string toolsDir, string sourceFile, string destinationFile, bool isMp3, Action<string> log, Action<string?, double?, bool?> updateProgress, CancellationToken cancellationToken)
        {
            var ffmpegPath = Path.Combine(toolsDir, "ffmpeg.exe");

            log("[ffmpeg] 変換処理の初期化を開始...");
            var duration = await GetVideoDurationAsync(toolsDir, sourceFile, log, cancellationToken);
            
            string arguments;
            string statusMsgPrefix;

            if (isMp3)
            {
                arguments = $"-y -progress pipe:1 -i \"{sourceFile}\" -c:a libmp3lame -q:a 2 \"{destinationFile}\"";
                statusMsgPrefix = "音声をMP3フォーマットに変換中...";
                log("[ffmpeg] MP3変換パラメータ設定: -c:a libmp3lame -q:a 2");
            }
            else
            {
                var videoCodec = await GetStreamCodecAsync(toolsDir, sourceFile, "v:0", log, cancellationToken);
                var audioCodec = await GetStreamCodecAsync(toolsDir, sourceFile, "a:0", log, cancellationToken);

                bool needsVideoConvert = videoCodec != "h264" && videoCodec != "hevc";
                bool needsAudioConvert = audioCodec != "aac" && audioCodec != "mp3";

                var videoArg = needsVideoConvert ? "-c:v libx264" : "-c:v copy";
                var audioArg = needsAudioConvert ? "-c:a aac" : "-c:a copy";

                if (needsVideoConvert && needsAudioConvert)
                {
                    statusMsgPrefix = "動画をMP4フォーマットに変換中 (映像・音声エンコード)...";
                }
                else if (needsVideoConvert)
                {
                    statusMsgPrefix = "動画をMP4フォーマットに変換中 (映像エンコード / 音声コピー)...";
                }
                else if (needsAudioConvert)
                {
                    statusMsgPrefix = "動画をMP4フォーマットに変換中 (映像コピー / 音声エンコード)...";
                }
                else
                {
                    statusMsgPrefix = "動画をMP4にコピー中...";
                }

                arguments = $"-y -progress pipe:1 -i \"{sourceFile}\" {videoArg} {audioArg} \"{destinationFile}\"";
                log($"[ffmpeg] MP4変換パラメータ設定: {videoArg} {audioArg}");
            }

            log($"[ffmpeg] FFmpeg プロセスを起動します: {arguments}");
            var startInfo = new ProcessStartInfo
            {
                FileName = ffmpegPath,
                Arguments = arguments,
                WorkingDirectory = toolsDir,
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            using var process = new Process { StartInfo = startInfo };
            process.Start();

            updateProgress(statusMsgPrefix, 0, duration <= 0);

            var errorLogBuilder = new StringBuilder();
            var readErrorTask = Task.Run(async () =>
            {
                using var reader = process.StandardError;
                while (true)
                {
                    var line = await reader.ReadLineAsync(cancellationToken);
                    if (line == null) break;
                    log($"[ffmpeg-err] {line}");
                    errorLogBuilder.AppendLine(line);
                }
            }, cancellationToken);

            var readOutputTask = Task.Run(async () =>
            {
                using var reader = process.StandardOutput;
                var outTimeUsRegex = new Regex(@"out_time_us=(\d+)", RegexOptions.Compiled);
                var outTimeRegex = new Regex(@"out_time=(\d{2}):(\d{2}):(\d{2})", RegexOptions.Compiled);
                string? currentOutTime = null;

                while (true)
                {
                    var line = await reader.ReadLineAsync(cancellationToken);
                    if (line == null) break;

                    log($"[ffmpeg] {line}");

                    var matchUs = outTimeUsRegex.Match(line);
                    if (matchUs.Success && long.TryParse(matchUs.Groups[1].Value, out var us))
                    {
                        var elapsedSeconds = us / 1000000.0;
                        if (duration > 0)
                        {
                            var percent = (elapsedSeconds / duration) * 100;
                            percent = Math.Min(100.0, Math.Max(0.0, percent));
                            var status = $"{statusMsgPrefix} {percent:F1}% (再生時間: {currentOutTime ?? elapsedSeconds.ToString("F1") + "s"})";
                            updateProgress(status, percent, false);
                        }
                    }

                    var matchTime = outTimeRegex.Match(line);
                    if (matchTime.Success)
                    {
                        currentOutTime = matchTime.Groups[1].Value + ":" + matchTime.Groups[2].Value + ":" + matchTime.Groups[3].Value;
                        if (duration <= 0)
                        {
                            var status = $"{statusMsgPrefix} (再生時間: {currentOutTime})";
                            updateProgress(status, null, true);
                        }
                    }
                }
            }, cancellationToken);

            try
            {
                await process.WaitForExitAsync(cancellationToken);
                await Task.WhenAll(readOutputTask, readErrorTask);
            }
            catch (OperationCanceledException)
            {
                try
                {
                    process.Kill(true);
                }
                catch { /* ignore */ }
                throw;
            }

            if (process.ExitCode != 0)
            {
                throw new Exception($"FFmpeg による変換に失敗しました (ExitCode: {process.ExitCode})。\n{errorLogBuilder}");
            }
        }

        public static async Task ExtractThumbnailAsync(string toolsDir, string videoFilePath, string outputImagePath, Action<string> log, CancellationToken cancellationToken)
        {
            var ffmpegPath = Path.Combine(toolsDir, "ffmpeg.exe");
            if (!File.Exists(ffmpegPath))
            {
                throw new FileNotFoundException("ffmpeg.exe が見つからないため、サムネイルを抽出できません。");
            }

            log($"[ffmpeg] 動画からサムネイルを抽出中: {Path.GetFileName(videoFilePath)}");
            
            // 開始1秒目(-ss 00:00:05)から1フレーム抽出し、画像として書き出す
            var arguments = $"-y -i \"{videoFilePath}\" -ss 00:00:05 -vframes 1 \"{outputImagePath}\"";

            var startInfo = new ProcessStartInfo
            {
                FileName = ffmpegPath,
                Arguments = arguments,
                WorkingDirectory = toolsDir,
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            using var process = new Process { StartInfo = startInfo };
            process.Start();

            var readErrorTask = Task.Run(async () =>
            {
                using var reader = process.StandardError;
                while (true)
                {
                    var line = await reader.ReadLineAsync(cancellationToken);
                    if (line == null) break;
                    log($"[ffmpeg-thumb-err] {line}");
                }
            }, cancellationToken);

            var readOutputTask = Task.Run(async () =>
            {
                using var reader = process.StandardOutput;
                while (true)
                {
                    var line = await reader.ReadLineAsync(cancellationToken);
                    if (line == null) break;
                    log($"[ffmpeg-thumb-out] {line}");
                }
            }, cancellationToken);

            try
            {
                await process.WaitForExitAsync(cancellationToken);
                await Task.WhenAll(readOutputTask, readErrorTask);
            }
            catch (OperationCanceledException)
            {
                try { process.Kill(true); } catch { }
                throw;
            }

            if (process.ExitCode != 0)
            {
                throw new Exception($"FFmpeg によるサムネイル抽出に失敗しました (ExitCode: {process.ExitCode})。");
            }

            log("[ffmpeg] サムネイル抽出が完了しました。");
        }
    }
}
