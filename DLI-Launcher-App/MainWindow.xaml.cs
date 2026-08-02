using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using CmlLib.Core;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;

namespace DLI_Launcher_App;

public partial class MainWindow : Window
{
    private string _frontendPath = "";
    private MinecraftLauncher? _minecraftLauncher;
    private List<string>? _cachedVersions;
    private List<(string Name, DateTime ReleaseTime)>? _cachedVersionData;
    private HttpListener? _oauthListener;
    private string? _pkceVerifier;
    private string _discordUsername = "DeliPlayer";
    private const string DiscordClientId = "1524146338403713085";
    private const string DiscordClientSecret = "YeRCIBJnAFZ_uPJY3IKwkCEF16v2WjT0";
    private const string DiscordRedirectUri = "http://localhost:28482/callback";

    private const string CurrentAppVersion = "1.0.8";
    private const string UpdateManifestUrl = "https://github.com/DLI-Studios/DLI-Launcher-Source/releases/latest/download/version.json";
    private UpdateManifestInfo? _cachedUpdateInfo;

    public class UpdateManifestInfo
    {
        public string version { get; set; } = "1.0.0";
        public int buildNumber { get; set; } = 1;
        public string downloadUrl { get; set; } = "";
        public List<string> changelog { get; set; } = new();
        public bool mandatory { get; set; } = false;
    }

    public MainWindow()
    {
        InitializeComponent();
        Loaded += MainWindow_Loaded;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        var exeDir = Path.GetDirectoryName(Environment.ProcessPath) ?? AppDomain.CurrentDomain.BaseDirectory;
        _frontendPath = FindFrontendPath();

        try
        {
            var userDataFolder = Path.Combine(Path.GetTempPath(), "DLI-Launcher-WV2-Cache");
            if (!Directory.Exists(userDataFolder))
            {
                Directory.CreateDirectory(userDataFolder);
            }

            CoreWebView2Environment env;
            try
            {
                env = await CoreWebView2Environment.CreateAsync(null, userDataFolder);
            }
            catch
            {
                env = await CoreWebView2Environment.CreateAsync(null, null);
            }

            await webView.EnsureCoreWebView2Async(env);

            webView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;
            webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
            webView.CoreWebView2.Settings.IsStatusBarEnabled = false;
            webView.CoreWebView2.Settings.IsWebMessageEnabled = true;

            webView.CoreWebView2.SetVirtualHostNameToFolderMapping(
                "dli-frontend",
                _frontendPath,
                CoreWebView2HostResourceAccessKind.Allow);

            var indexPath = Path.Combine(_frontendPath, "index.html");
            if (File.Exists(indexPath))
            {
                webView.CoreWebView2.Navigate("https://dli-frontend/index.html");
            }
            else
            {
                var triedPaths = string.Join("<br>", FindFrontendPathCandidates().Select(p => System.Net.WebUtility.HtmlEncode(p)));
                try { File.AppendAllText(Path.Combine(Path.GetTempPath(), "dli-launcher.log"), $"[{DateTime.Now:HH:mm:ss}] Frontend bulunamadi, exeDir={exeDir}, lastPath={_frontendPath}{Environment.NewLine}"); } catch { }
                var errorHtml = $@"
                    <!DOCTYPE html>
                    <html>
                    <head><meta charset=""UTF-8""></head>
                    <body style='background:#13111c;color:white;font-family:sans-serif;display:flex;align-items:center;justify-content:center;height:100vh;margin:0;'>
                        <div style='text-align:center;'>
                            <h1>DLI Launcher</h1>
                            <p style='color:#888;'>Frontend klasörü bulunamadı</p>
                            <p style='color:#c084fc;'>Denenen yollar:</p>
                            <p style='color:#888;font-size:12px;'>{triedPaths}</p>
                        </div>
                    </body>
                    </html>";
                webView.CoreWebView2.NavigateToString(errorHtml);
            }

            // CmlLib launcher'ı başlat - sürümleri arka planda yükle
            _ = LoadVersionsAsync();

            // Discord OAuth callback listener baslat
            StartOAuthListener();

            // Kalici oturum dosyasindan localStorage'a yukle
            _ = RestoreSessionAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"WebView2 hatasi: {ex.Message}", "DLI Launcher", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private string[] FindFrontendPathCandidates()
    {
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var exeDir = Path.GetDirectoryName(Environment.ProcessPath) ?? baseDir;

        var desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

        return new[]
        {
            Path.Combine(exeDir, "DLI-Launcher"),
            Path.Combine(baseDir, "DLI-Launcher"),
            Path.Combine(baseDir, "..", "..", "..", "..", "launcher", "dist"),
            Path.Combine(baseDir, "..", "..", "..", "..", "launcher"),
            Path.Combine(desktop, "DLI Source", "launcher", "dist"),
            Path.Combine(desktop, "DLI Source", "launcher"),
            Path.Combine(desktop, "DLI-Launcher"),
        };
    }

    private string FindFrontendPath()
    {
        var exeDir = Path.GetDirectoryName(Environment.ProcessPath) ?? AppDomain.CurrentDomain.BaseDirectory;

        foreach (var path in FindFrontendPathCandidates())
        {
            try
            {
                var fullPath = Path.GetFullPath(path);
                var indexPath = Path.Combine(fullPath, "index.html");
                if (File.Exists(indexPath))
                    return fullPath;
            }
            catch { }
        }

        return exeDir;
    }

    private void OnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        var json = e.WebMessageAsJson;

        try
        {
            var msg = JsonSerializer.Deserialize<JsonElement>(json);
            var type = msg.GetProperty("type").GetString() ?? "";
            var id = msg.TryGetProperty("id", out var idProp) ? idProp.GetString() : null;

            var response = HandleMessage(type, msg.TryGetProperty("payload", out var payload) ? payload : null);

            if (id != null)
            {
                var responseJson = JsonSerializer.Serialize(new
                {
                    success = true,
                    data = response,
                    id = id
                });
                webView.CoreWebView2.PostWebMessageAsJson(responseJson);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Bridge error: {ex.Message}");
        }
    }

    private object? HandleMessage(string type, JsonElement? payload)
    {
        switch (type)
        {
            case "LAUNCH_GAME":
                return HandleLaunchGame(payload);
            case "GET_MINECRAFT_VERSIONS":
                return HandleGetVersions();
            case "CHECK_FOR_UPDATES":
            case "GET_UPDATE_STATUS":
                return HandleCheckForUpdates();
            case "START_UPDATE":
                var url = payload?.TryGetProperty("downloadUrl", out var u) == true ? u.GetString() : null;
                _ = StartUpdateAsync(url);
                return new { status = "started" };
            case "MINIMIZE_WINDOW":
                Dispatcher.Invoke(() => WindowState = WindowState.Minimized);
                return null;
            case "MAXIMIZE_WINDOW":
                Dispatcher.Invoke(() =>
                {
                    WindowState = WindowState == WindowState.Maximized
                        ? WindowState.Normal
                        : WindowState.Maximized;
                });
                return null;
            case "CLOSE_WINDOW":
                Dispatcher.Invoke(() => Close());
                return null;
            case "GET_PLAYER_INFO":
                return new
                {
                    username = "DeliPlayer",
                    uuid = "550e8400-e29b-41d4-a716-446655440000",
                    level = 42,
                    xp = 12450,
                    xpMax = 20000,
                    coins = 2450,
                    gems = 850,
                    isPremium = true
                };
            case "GET_VERSION":
                return new { version = CurrentAppVersion, buildNumber = 42 };
            case "DISCORD_LOGIN":
                Dispatcher.Invoke(() =>
                {
                    _pkceVerifier = GenerateCodeVerifier();
                    var codeChallenge = GenerateCodeChallenge(_pkceVerifier);
                    var authUrl = $"https://discord.com/api/oauth2/authorize?client_id={DiscordClientId}&redirect_uri={Uri.EscapeDataString(DiscordRedirectUri)}&response_type=code&scope=identify%20email&code_challenge={Uri.EscapeDataString(codeChallenge)}&code_challenge_method=S256";
                    var psi = new ProcessStartInfo(authUrl) { UseShellExecute = true };
                    Process.Start(psi);
                });
                return new { status = "opening" };
            case "SET_USERNAME":
                var uname = payload?.TryGetProperty("username", out var uProp) == true ? uProp.GetString() : null;
                if (!string.IsNullOrEmpty(uname))
                {
                    _discordUsername = uname;
                    Debug.WriteLine($"[DLI] Username updated to: {_discordUsername}");
                }
                return null;
            case "CLEAR_SESSION":
                ClearSession();
                return null;
            case "OPEN_MODS_FOLDER":
                Dispatcher.Invoke(() =>
                {
                    var modsPath = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                        ".minecraft", "mods");
                    if (!Directory.Exists(modsPath))
                        Directory.CreateDirectory(modsPath);
                    Process.Start(new ProcessStartInfo(modsPath) { UseShellExecute = true });
                });
                return null;
            default:
                return null;
        }
    }

    private object HandleGetVersions()
    {
        // CmlLib'den yüklüyse release tarihine göre sırala
        if (_cachedVersionData != null && _cachedVersionData.Count > 0)
        {
            var sorted = _cachedVersionData
                .OrderByDescending(x => x.ReleaseTime)
                .Select(x => x.Name)
                .ToList();

            return new
            {
                versions = sorted,
                source = "cmllib",
                count = sorted.Count
            };
        }

        // CmlLib henüz yüklenmediyse fallback - tarihe göre sıralı
        var fallback = new List<(string Name, DateTime ReleaseTime)>
        {
            ("1.21.1", new DateTime(2024, 8, 8)), ("1.21", new DateTime(2024, 6, 13)),
            ("1.20.6", new DateTime(2024, 4, 5)), ("1.20.5", new DateTime(2024, 4, 2)),
            ("1.20.4", new DateTime(2023, 12, 7)), ("1.20.3", new DateTime(2023, 12, 5)),
            ("1.20.2", new DateTime(2023, 9, 21)), ("1.20.1", new DateTime(2023, 6, 12)),
            ("1.20", new DateTime(2023, 6, 7)),
            ("1.19.4", new DateTime(2023, 3, 14)), ("1.19.3", new DateTime(2022, 12, 7)),
            ("1.19.2", new DateTime(2022, 8, 5)), ("1.19.1", new DateTime(2022, 6, 12)),
            ("1.19", new DateTime(2022, 6, 7)),
            ("1.18.2", new DateTime(2022, 2, 28)), ("1.18.1", new DateTime(2021, 12, 10)),
            ("1.18", new DateTime(2021, 11, 30)),
            ("1.17.1", new DateTime(2021, 7, 6)), ("1.17", new DateTime(2021, 6, 8)),
            ("1.16.5", new DateTime(2021, 1, 15)), ("1.16.4", new DateTime(2020, 11, 5)),
            ("1.16.3", new DateTime(2020, 9, 10)), ("1.16.2", new DateTime(2020, 9, 10)),
            ("1.16.1", new DateTime(2020, 6, 24)), ("1.16", new DateTime(2020, 6, 23)),
            ("1.15.2", new DateTime(2020, 1, 17)), ("1.15.1", new DateTime(2019, 12, 17)),
            ("1.15", new DateTime(2019, 12, 10)),
            ("1.14.4", new DateTime(2019, 7, 19)), ("1.14.3", new DateTime(2019, 6, 17)),
            ("1.14.2", new DateTime(2019, 5, 31)), ("1.14.1", new DateTime(2019, 5, 18)),
            ("1.14", new DateTime(2019, 4, 23)),
            ("1.13.2", new DateTime(2018, 10, 26)), ("1.13.1", new DateTime(2018, 9, 18)),
            ("1.13", new DateTime(2018, 7, 18)),
            ("1.12.2", new DateTime(2017, 9, 18)), ("1.12.1", new DateTime(2017, 6, 9)),
            ("1.12", new DateTime(2017, 6, 7)),
            ("1.11.2", new DateTime(2016, 12, 21)), ("1.11.1", new DateTime(2016, 11, 14)),
            ("1.11", new DateTime(2016, 11, 14)),
            ("1.10.2", new DateTime(2016, 7, 26)), ("1.10.1", new DateTime(2016, 6, 22)),
            ("1.10", new DateTime(2016, 6, 8)),
            ("1.9.4", new DateTime(2016, 5, 10)), ("1.9.2", new DateTime(2016, 4, 27)),
            ("1.9.1", new DateTime(2016, 3, 31)), ("1.9", new DateTime(2016, 2, 29)),
            ("1.8.9", new DateTime(2015, 12, 3)), ("1.8.8", new DateTime(2015, 11, 4)),
            ("1.8.7", new DateTime(2015, 8, 3)), ("1.8.6", new DateTime(2015, 5, 28)),
            ("1.8.5", new DateTime(2015, 5, 13)), ("1.8.4", new DateTime(2015, 4, 17)),
            ("1.8.3", new DateTime(2015, 3, 11)), ("1.8.2", new DateTime(2015, 2, 20)),
            ("1.8.1", new DateTime(2014, 11, 14)), ("1.8", new DateTime(2014, 9, 2)),
            ("1.7.10", new DateTime(2014, 5, 15)), ("1.7.9", new DateTime(2014, 4, 14)),
            ("1.7.8", new DateTime(2014, 4, 10)), ("1.7.7", new DateTime(2014, 4, 9)),
            ("1.7.6", new DateTime(2014, 4, 9)), ("1.7.5", new DateTime(2014, 3, 24)),
            ("1.7.4", new DateTime(2013, 12, 18)), ("1.7.2", new DateTime(2013, 10, 25)),
            ("1.6.4", new DateTime(2013, 11, 15)), ("1.6.2", new DateTime(2013, 7, 8)),
            ("1.6.1", new DateTime(2013, 6, 26)),
            ("1.5.2", new DateTime(2013, 5, 2)), ("1.5.1", new DateTime(2013, 4, 19)),
            ("1.4.7", new DateTime(2013, 1, 9)), ("1.4.6", new DateTime(2012, 12, 19)),
            ("1.4.5", new DateTime(2012, 12, 17)), ("1.4.4", new DateTime(2012, 12, 14)),
            ("1.4.2", new DateTime(2012, 11, 1)),
            ("1.3.2", new DateTime(2012, 8, 16)), ("1.3.1", new DateTime(2012, 8, 15)),
            ("1.2.5", new DateTime(2012, 4, 4)), ("1.2.4", new DateTime(2012, 3, 22)),
            ("1.2.3", new DateTime(2012, 3, 15)), ("1.2.2", new DateTime(2012, 1, 12)),
            ("1.2.1", new DateTime(2011, 12, 1)),
            ("1.1", new DateTime(2011, 11, 18)), ("1.0", new DateTime(2011, 11, 18)),
        };

        return new
        {
            versions = fallback.OrderByDescending(x => x.ReleaseTime).Select(x => x.Name).ToList(),
            source = "fallback",
            count = fallback.Count
        };
    }

    private object HandleCheckForUpdates()
    {
        try
        {
            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(4);
            var jsonTask = client.GetStringAsync(UpdateManifestUrl);
            jsonTask.Wait();
            var json = jsonTask.Result;

            var manifest = JsonSerializer.Deserialize<UpdateManifestInfo>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (manifest != null)
            {
                _cachedUpdateInfo = manifest;
                bool isNewer = IsVersionNewer(manifest.version, CurrentAppVersion);
                return new
                {
                    available = isNewer,
                    currentVersion = CurrentAppVersion,
                    latestVersion = manifest.version,
                    downloadUrl = manifest.downloadUrl,
                    changelog = manifest.changelog,
                    mandatory = manifest.mandatory
                };
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DLI Update Check Error] {ex.Message}");
        }

        return new
        {
            available = false,
            currentVersion = CurrentAppVersion,
            latestVersion = CurrentAppVersion,
            downloadUrl = "",
            changelog = new List<string>(),
            mandatory = false
        };
    }

    private static bool IsVersionNewer(string remoteVersion, string currentVersion)
    {
        try
        {
            var remote = new Version(remoteVersion);
            var current = new Version(currentVersion);
            return remote > current;
        }
        catch
        {
            return remoteVersion != currentVersion;
        }
    }

    private async Task StartUpdateAsync(string? overrideUrl)
    {
        var downloadUrl = !string.IsNullOrEmpty(overrideUrl) ? overrideUrl : _cachedUpdateInfo?.downloadUrl;
        if (string.IsNullOrEmpty(downloadUrl))
        {
            SendUpdateMessage("UPDATE_ERROR", new { error = "İndirme adresi bulunamadı." });
            return;
        }

        try
        {
            SendUpdateMessage("UPDATE_PROGRESS", new { percent = 5, status = "İndirme başlatılıyor..." });

            var tempPath = Path.Combine(Path.GetTempPath(), "DLI_Launcher_Update");
            if (Directory.Exists(tempPath)) Directory.Delete(tempPath, true);
            Directory.CreateDirectory(tempPath);

            var zipFile = Path.Combine(tempPath, "update.zip");

            using (var client = new HttpClient())
            using (var response = await client.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead))
            {
                response.EnsureSuccessStatusCode();
                var totalBytes = response.Content.Headers.ContentLength ?? -1L;

                using (var contentStream = await response.Content.ReadAsStreamAsync())
                using (var fileStream = new FileStream(zipFile, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
                {
                    var buffer = new byte[8192];
                    var totalRead = 0L;
                    int read;
                    while ((read = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        await fileStream.WriteAsync(buffer, 0, read);
                        totalRead += read;
                        if (totalBytes > 0)
                        {
                            var percent = Math.Round((double)totalRead / totalBytes * 90, 1);
                            SendUpdateMessage("UPDATE_PROGRESS", new { percent = percent, status = $"İndiriliyor... %{percent}" });
                        }
                    }
                }
            }

            SendUpdateMessage("UPDATE_PROGRESS", new { percent = 95, status = "Dosyalar çıkarılıyor..." });
            var extractPath = Path.Combine(tempPath, "extracted");
            ZipFile.ExtractToDirectory(zipFile, extractPath, true);

            SendUpdateMessage("UPDATE_PROGRESS", new { percent = 100, status = "Güncelleme uygulanıyor..." });
            await Task.Delay(1000);

            var batPath = Path.Combine(tempPath, "updater.bat");
            var exePath = Environment.ProcessPath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DLI-Launcher.exe");
            var exeDir = Path.GetDirectoryName(exePath) ?? AppDomain.CurrentDomain.BaseDirectory;

            var batContent = $@"@echo off
chcp 65001 > nul
timeout /t 2 /nobreak > nul
taskkill /f /im DLI-Launcher.exe >nul 2>&1
if not exist ""{exeDir}\DLI-Launcher"" mkdir ""{exeDir}\DLI-Launcher""
xcopy /s /y /q /e ""{extractPath}\DLI-Launcher\*"" ""{exeDir}\DLI-Launcher\"">nul
xcopy /y /q ""{extractPath}\DLI-Launcher.exe"" ""{exeDir}\"">nul
start """" ""{exePath}""
rmdir /s /q ""{tempPath}""
";
            File.WriteAllText(batPath, "\uFEFF" + batContent, new System.Text.UTF8Encoding(false));

            var psi = new ProcessStartInfo
            {
                FileName = batPath,
                UseShellExecute = true,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            Process.Start(psi);
            Dispatcher.Invoke(() => Application.Current.Shutdown());
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Update error: {ex.Message}");
            SendUpdateMessage("UPDATE_ERROR", new { error = ex.Message });
        }
    }

    private void SendUpdateMessage(string type, object data)
    {
        Dispatcher.Invoke(() =>
        {
            var msg = JsonSerializer.Serialize(new { type, data });
            webView.CoreWebView2?.PostWebMessageAsJson(msg);
        });
    }

    private object? HandleLaunchGame(JsonElement? payload)
    {
        var version = "1.21.1";
        if (payload?.TryGetProperty("version", out var v) == true)
            version = v.GetString() ?? "1.21.1";

        var maxRamMb = 4096;
        if (payload?.TryGetProperty("ramMb", out var ramProp) == true && ramProp.ValueKind == JsonValueKind.Number)
            maxRamMb = ramProp.GetInt32();

        var sizeMb = GetDownloadSize(version);

        // CmlLib ile indir + baslat (progress event'leri frontend'e gonder)
        _ = Task.Run(async () =>
        {
            try
            {
                if (_minecraftLauncher == null)
                    _minecraftLauncher = new MinecraftLauncher();

                // Progress event'lerini frontend'e ilet
                _minecraftLauncher.FileProgressChanged += (sender, args) =>
                {
                    var progress = args.TotalTasks > 0
                        ? (double)args.ProgressedTasks / args.TotalTasks * 100
                        : 0;

                    Dispatcher.Invoke(() =>
                    {
                        var msg = JsonSerializer.Serialize(new
                        {
                            type = "DOWNLOAD_PROGRESS",
                            data = new
                            {
                                percent = Math.Round(progress, 1),
                                file = args.Name,
                                task = $"{args.ProgressedTasks}/{args.TotalTasks}",
                                eventType = args.EventType.ToString()
                            }
                        });
                        webView.CoreWebView2.PostWebMessageAsJson(msg);
                    });
                };

                _minecraftLauncher.ByteProgressChanged += (sender, args) =>
                {
                    // Byte progress - daha hassas guncelleme icin
                };

                Debug.WriteLine($"Installing Minecraft {version}...");
                await _minecraftLauncher.InstallAsync(version);

                Debug.WriteLine($"Building process for {version} with {maxRamMb}MB RAM...");
                var versionData = await _minecraftLauncher.GetVersionAsync(version);
                var process = _minecraftLauncher.BuildProcess(versionData, new CmlLib.Core.ProcessBuilder.MLaunchOption
                {
                    Session = CmlLib.Core.Auth.MSession.CreateOfflineSession(_discordUsername),
                    MaximumRamMb = maxRamMb
                });

                // Indirme bitti - frontend'e haber ver
                Dispatcher.Invoke(() =>
                {
                    var msg = JsonSerializer.Serialize(new
                    {
                        type = "DOWNLOAD_COMPLETE",
                        data = new { version }
                    });
                    webView.CoreWebView2.PostWebMessageAsJson(msg);
                });

                // Performans ayarlarini oyunun options.txt dosyasina uygula
                if (payload?.TryGetProperty("settings", out var settings) == true)
                    ApplyGameSettings(settings);

                // Kisa gecikme sonra oyunu baslat
                await Task.Delay(1500);
                process.Start();
                Debug.WriteLine($"Minecraft {version} started with PID {process.Id}");

                // Oyun baslatildi - Launcher'i minimize et (RAM tasarrufu)
                Dispatcher.Invoke(() =>
                {
                    WindowState = WindowState.Minimized;
                    var msg = JsonSerializer.Serialize(new
                    {
                        type = "GAME_STARTED",
                        data = new { version, pid = process.Id }
                    });
                    webView.CoreWebView2.PostWebMessageAsJson(msg);
                });

                // Oyun kapatildiginda haber ver ve Launcher'i geri ac
                process.EnableRaisingEvents = true;
                process.Exited += (sender, args) =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        WindowState = WindowState.Normal;
                        Show();
                        Focus();
                        var msg = JsonSerializer.Serialize(new { type = "GAME_STOPPED" });
                        webView.CoreWebView2.PostWebMessageAsJson(msg);
                    });
                };
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Launch error: {ex.Message}");
                Dispatcher.Invoke(() =>
                {
                    var msg = JsonSerializer.Serialize(new
                    {
                        type = "DOWNLOAD_ERROR",
                        data = new { error = ex.Message }
                    });
                    webView.CoreWebView2.PostWebMessageAsJson(msg);
                });
            }
        });

        return new
        {
            status = "launching",
            version = version,
            downloadSizeMb = sizeMb
        };
    }

    /// <summary>
    /// Kullanıcının ayarlarını oyun başlamadan önce %appdata%\.minecraft\options.txt dosyasına yazar.
    /// Böylece Minecraft bu ayarları gerçekten uygular (render distance, FPS limit, grafikler vb.).
    /// </summary>
    private void ApplyGameSettings(JsonElement settings)
    {
        try
        {
            if (settings.ValueKind != JsonValueKind.Object) return;

            var mcDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".minecraft");
            var optionsPath = Path.Combine(mcDir, "options.txt");
            if (!Directory.Exists(mcDir)) return;
            if (!File.Exists(optionsPath)) File.WriteAllText(optionsPath, "", new UTF8Encoding(false));

            var lines = new List<string>(File.ReadAllLines(optionsPath));

            void Set(string key, string value)
            {
                var index = lines.FindIndex(l => l.StartsWith(key + ":"));
                if (index >= 0) lines[index] = $"{key}:{value}";
                else lines.Add($"{key}:{value}");
            }

            if (settings.TryGetProperty("renderDistance", out var rd) && rd.ValueKind == JsonValueKind.Number)
                Set("renderDistance", rd.GetInt32().ToString());

            if (settings.TryGetProperty("fpsLimit", out var fps) && fps.ValueKind == JsonValueKind.Number)
                Set("fpsLimit", fps.GetInt32() <= 0 ? "260" : fps.GetInt32().ToString());

            if (settings.TryGetProperty("graphics", out var gfx) && gfx.ValueKind == JsonValueKind.String)
                Set("gfxFancy", gfx.GetString() == "fast" ? "0" : "1");

            if (settings.TryGetProperty("particles", out var part) && part.ValueKind == JsonValueKind.String)
            {
                var p = part.GetString();
                Set("particles", p == "all" ? "0" : p == "decreased" ? "1" : "2");
            }

            if (settings.TryGetProperty("vsync", out var vs) && (vs.ValueKind == JsonValueKind.True || vs.ValueKind == JsonValueKind.False))
                Set("enableVsync", vs.GetBoolean() ? "1" : "0");

            if (settings.TryGetProperty("viewBobbing", out var vb) && (vb.ValueKind == JsonValueKind.True || vb.ValueKind == JsonValueKind.False))
                Set("viewBobbing", vb.GetBoolean() ? "1" : "0");

            if (settings.TryGetProperty("clouds", out var cl) && (cl.ValueKind == JsonValueKind.True || cl.ValueKind == JsonValueKind.False))
                Set("clouds", cl.GetBoolean() ? "1" : "0");

            if (settings.TryGetProperty("antialiasing", out var aa) && (aa.ValueKind == JsonValueKind.True || aa.ValueKind == JsonValueKind.False))
                Set("ao", aa.GetBoolean() ? "1" : "0");

            if (settings.TryGetProperty("mipmapLevels", out var mi) && mi.ValueKind == JsonValueKind.Number)
                Set("mipmapLevels", mi.GetInt32().ToString());

            if (settings.TryGetProperty("entityShadows", out var es) && (es.ValueKind == JsonValueKind.True || es.ValueKind == JsonValueKind.False))
                Set("entityShadows", es.GetBoolean() ? "1" : "0");

            if (settings.TryGetProperty("fov", out var fov) && fov.ValueKind == JsonValueKind.Number)
                Set("fov", fov.GetInt32().ToString());

            if (settings.TryGetProperty("sensitivity", out var sens) && sens.ValueKind == JsonValueKind.Number)
                Set("mouseSensitivity", sens.GetDouble().ToString("0.00", System.Globalization.CultureInfo.InvariantCulture));

            if (settings.TryGetProperty("fullscreen", out var full) && (full.ValueKind == JsonValueKind.True || full.ValueKind == JsonValueKind.False))
                Set("fullscreen", full.GetBoolean() ? "1" : "0");

            File.WriteAllLines(optionsPath, lines, new UTF8Encoding(false));
            Debug.WriteLine($"[DLI] Applied game settings to options.txt");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DLI ApplyGameSettings Error] {ex.Message}");
        }
    }

    private static int GetDownloadSize(string version)
    {
        // Yaklaşık boyutlar (MB) - sürümüne göre
        if (version.StartsWith("26.")) return 380;
        if (version.StartsWith("1.21.")) return 350;
        if (version.StartsWith("1.20.")) return 320;
        if (version.StartsWith("1.19.")) return 290;
        if (version.StartsWith("1.18.")) return 270;
        if (version.StartsWith("1.17.")) return 250;
        if (version.StartsWith("1.16.")) return 230;
        if (version.StartsWith("1.15.")) return 210;
        if (version.StartsWith("1.14.")) return 195;
        if (version.StartsWith("1.13.")) return 180;
        if (version.StartsWith("1.12.")) return 160;
        if (version.StartsWith("1.11.")) return 150;
        if (version.StartsWith("1.10.")) return 140;
        if (version.StartsWith("1.9.")) return 130;
        if (version.StartsWith("1.8.")) return 120;
        if (version.StartsWith("1.7.")) return 110;
        if (version.StartsWith("1.6.")) return 95;
        if (version.StartsWith("1.5.")) return 85;
        if (version.StartsWith("1.4.")) return 80;
        if (version.StartsWith("1.3.")) return 70;
        if (version.StartsWith("1.2.")) return 65;
        return 60; // 1.0, 1.1
    }

    private async Task LoadVersionsAsync()
    {
        try
        {
            Debug.WriteLine("CmlLib: Starting version load...");
            _minecraftLauncher = new MinecraftLauncher();
            var versions = await _minecraftLauncher.GetAllVersionsAsync();
            var versionList = versions.ToList();
            Debug.WriteLine($"CmlLib: Got {versionList.Count} total versions");

            var releaseData = new List<(string Name, DateTime ReleaseTime)>();
            foreach (var v in versionList)
            {
                try
                {
                    var typeName = v.Type ?? "";
                    if (typeName.Equals("release", StringComparison.OrdinalIgnoreCase))
                    {
                        releaseData.Add((v.Name, v.ReleaseTime.DateTime));
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"CmlLib: Error reading version {v.Name}: {ex.Message}");
                }
            }

            _cachedVersionData = releaseData;
            _cachedVersions = releaseData.OrderByDescending(x => x.ReleaseTime).Select(x => x.Name).ToList();
            Debug.WriteLine($"CmlLib: {_cachedVersions.Count} release versions loaded (sorted by date)");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"CmlLib load error: {ex.Message}");
            Debug.WriteLine($"CmlLib stack: {ex.StackTrace}");
        }
    }

    #region Discord OAuth

    private static string GetSessionFilePath()
    {
        return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "session.json");
    }

    private async Task RestoreSessionAsync()
    {
        try
        {
            var sessionPath = GetSessionFilePath();
            if (File.Exists(sessionPath))
            {
                var json = File.ReadAllText(sessionPath);
                var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("id", out _))
                {
                    // Discord username'u session'dan yukle
                    var globalName = doc.RootElement.TryGetProperty("global_name", out var gn) ? gn.GetString() : null;
                    var username = doc.RootElement.TryGetProperty("username", out var un) ? un.GetString() : null;
                    if (!string.IsNullOrEmpty(globalName) || !string.IsNullOrEmpty(username))
                    {
                        _discordUsername = globalName ?? username ?? "DeliPlayer";
                        Debug.WriteLine($"[DLI] Username restored: {_discordUsername}");
                    }

                    // WebView2 yuklendikten sonra localStorage'a yaz
                    await Task.Delay(1500);
                    // JSON'u script icin kacarak formatla
                    var escaped = json.Replace("\\", "\\\\").Replace("'", "\\'").Replace("\n", " ").Replace("\r", "");
                    await webView.CoreWebView2.ExecuteScriptAsync(
                        $"localStorage.setItem('dli_discord_user', '{escaped}');");
                    Debug.WriteLine("[DLI] Session restored from file");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DLI] Session restore error: {ex.Message}");
        }
    }

    private void SaveSession(string userDataJson, string token)
    {
        try
        {
            // Direkt user JSON'unu kaydet (frontend'in bekledigi format)
            File.WriteAllText(GetSessionFilePath(), userDataJson);
            Debug.WriteLine("[DLI] Session saved to file");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DLI] Session save error: {ex.Message}");
        }
    }

    private void ClearSession()
    {
        try
        {
            var sessionPath = GetSessionFilePath();
            if (File.Exists(sessionPath)) File.Delete(sessionPath);
        }
        catch { }
    }

    private void StartOAuthListener()
    {
        try
        {
            _oauthListener = new HttpListener();
            _oauthListener.Prefixes.Add("http://localhost:28482/");
            _oauthListener.Start();

            _ = Task.Run(async () =>
            {
                while (_oauthListener.IsListening)
                {
                    try
                    {
                        var context = await _oauthListener.GetContextAsync();
                        _ = HandleOAuthRequest(context);
                    }
                    catch (ObjectDisposedException) { break; }
                    catch (Exception ex) { Debug.WriteLine($"OAuth listener error: {ex.Message}"); }
                }
            });

            Debug.WriteLine("OAuth listener started on port 28482");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"OAuth listener start error: {ex.Message}");
        }
    }

    private async Task HandleOAuthRequest(HttpListenerContext context)
    {
        var request = context.Request;
        var response = context.Response;

        if (request.Url?.AbsolutePath == "/callback")
        {
            var code = request.QueryString["code"];
            if (!string.IsNullOrEmpty(code))
            {
                try
                {
                    var tokenData = await ExchangeCode(code);
                    if (tokenData != null)
                    {
                        var user = await GetDiscordUser(tokenData.AccessToken);
                        if (user != null)
                        {
                            _discordUsername = user.GlobalName ?? user.Username;

                            var userDataJson = JsonSerializer.Serialize(new
                            {
                                id = user.Id,
                                username = user.Username,
                                discriminator = user.Discriminator,
                                avatar = user.Avatar,
                                global_name = user.GlobalName,
                                avatar_decoration = (string?)null
                            });

                            // Direkt WebView2'ye script ile localStorage'a yaz
                            var safeJson = userDataJson.Replace("'", "\\'").Replace("\n", " ");
                            Dispatcher.Invoke(async () =>
                            {
                                await webView.CoreWebView2.ExecuteScriptAsync(
                                    $"localStorage.setItem('dli_discord_user', '{safeJson}'); localStorage.setItem('dli_discord_token', '{tokenData.AccessToken}'); location.reload();");

                                // Bridge mesaji gonder (authService dinleyicisi icin)
                                var bridgeMsg = JsonSerializer.Serialize(new
                                {
                                    type = "USER_LOGGED_IN",
                                    data = new
                                    {
                                        user = new
                                        {
                                            id = user.Id,
                                            username = user.Username,
                                            discriminator = user.Discriminator,
                                            avatar = user.Avatar,
                                            global_name = user.GlobalName,
                                            avatar_decoration = (string?)null
                                        },
                                        token = tokenData.AccessToken
                                    }
                                });
                                webView.CoreWebView2.PostWebMessageAsJson(bridgeMsg);
                            });

                            // Kalici oturum dosyasina da kaydet
                            SaveSession(userDataJson, tokenData.AccessToken);

                            var html = $@"
                                <!DOCTYPE html>
                                <html>
                                <head>
                                    <title>Giris Basarili</title>
                                    <link href='https://fonts.googleapis.com/css2?family=Inter:wght@400;600;700;900&display=swap' rel='stylesheet'>
                                </head>
                                <body style='margin:0;padding:0;background:#0c0a14;font-family:Inter,sans-serif;display:flex;align-items:center;justify-content:center;height:100vh;overflow:hidden;'>
                                    <div style='position:fixed;inset:0;pointer-events:none;'>
                                        <div style='position:absolute;left:50%;top:50%;width:500px;height:500px;transform:translate(-50%,-50%);background:radial-gradient(circle,rgba(168,85,247,0.15),transparent 70%);border-radius:50%;filter:blur(60px);'></div>
                                    </div>
                                    <div style='position:relative;z-index:1;text-align:center;animation:fadeIn 0.6s ease-out;'>
                                        <div style='margin-bottom:28px;'>
                                            <span style='font-size:72px;font-weight:900;font-style:italic;color:white;letter-spacing:-2px;text-shadow:0 0 40px rgba(168,85,247,0.5),0 0 80px rgba(168,85,247,0.2);'>DLI</span>
                                        </div>
                                        <div style='width:80px;height:80px;margin:0 auto 24px;border-radius:50%;background:linear-gradient(135deg,#5865F2,#7289DA);display:flex;align-items:center;justify-content:center;box-shadow:0 0 30px rgba(88,101,242,0.4);'>
                                            <svg width='40' height='40' viewBox='0 0 24 24' fill='white'>
                                                <path d='M20.317 4.37a19.791 19.791 0 0 0-4.885-1.515.074.074 0 0 0-.079.037c-.21.375-.444.864-.608 1.25a18.27 18.27 0 0 0-5.487 0 12.64 12.64 0 0 0-.617-1.25.077.077 0 0 0-.079-.037A19.736 19.736 0 0 0 3.677 4.37a.07.07 0 0 0-.032.027C.533 9.046-.32 13.58.099 18.057a.082.082 0 0 0 .031.057 19.9 19.9 0 0 0 5.993 3.03.078.078 0 0 0 .084-.028c.462-.63.874-1.295 1.226-1.994a.076.076 0 0 0-.041-.106 13.107 13.107 0 0 1-1.872-.892.077.077 0 0 1-.008-.128 10.2 10.2 0 0 0 .372-.292.074.074 0 0 1 .077-.01c3.928 1.793 8.18 1.793 12.062 0a.074.074 0 0 1 .078.01c.12.098.246.198.373.292a.077.077 0 0 1-.006.127 12.299 12.299 0 0 1-1.873.892.077.077 0 0 0-.041.107c.36.698.772 1.362 1.225 1.993a.076.076 0 0 0 .084.028 19.839 19.839 0 0 0 6.002-3.03.077.077 0 0 0 .032-.054c.5-5.177-.838-9.674-3.549-13.66a.061.061 0 0 0-.031-.03zM8.02 15.33c-1.183 0-2.157-1.085-2.157-2.419 0-1.333.956-2.419 2.157-2.419 1.21 0 2.176 1.096 2.157 2.42 0 1.333-.956 2.418-2.157 2.418zm7.975 0c-1.183 0-2.157-1.085-2.157-2.419 0-1.333.955-2.419 2.157-2.419 1.21 0 2.176 1.096 2.157 2.42 0 1.333-.946 2.418-2.157 2.418z'/>
                                            </svg>
                                        </div>
                                        <h2 style='color:#c084fc;font-size:28px;font-weight:900;font-style:italic;letter-spacing:1px;margin:0 0 8px 0;'>Giris Basarili!</h2>
                                        <p style='color:rgba(255,255,255,0.5);font-size:14px;margin:0 0 32px 0;'>Pencereyi kapatabilirsiniz</p>
                                        <div style='display:flex;align-items:center;justify-content:center;gap:8px;'>
                                            <div style='width:6px;height:6px;border-radius:50%;background:#22c55e;animation:pulse 1.5s infinite;'></div>
                                            <span style='color:rgba(255,255,255,0.4);font-size:12px;text-transform:uppercase;letter-spacing:2px;'>Oturum aktif</span>
                                        </div>
                                    </div>
                                    <style>
                                        @keyframes fadeIn {{ from {{ opacity:0; transform:translateY(20px); }} to {{ opacity:1; transform:translateY(0); }} }}
                                        @keyframes pulse {{ 0%,100% {{ opacity:1; }} 50% {{ opacity:0.4; }} }}
                                    </style>
                                    <script>setTimeout(function(){{ window.close(); }}, 3000);</script>
                                </body>
                                </html>";

                            var buffer = Encoding.UTF8.GetBytes(html);
                            response.ContentType = "text/html; charset=utf-8";
                            response.ContentLength64 = buffer.Length;
                            await response.OutputStream.WriteAsync(buffer);
                            response.OutputStream.Close();
                            return;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"OAuth exchange error: {ex.Message}");
                }
            }

            var errHtml = "<html><body style='background:#13111c;color:white;font-family:sans-serif;display:flex;align-items:center;justify-content:center;height:100vh;margin:0;'><h2>Giris hatasi olustu</h2></body></html>";
            var errBuffer = Encoding.UTF8.GetBytes(errHtml);
            response.ContentType = "text/html; charset=utf-8";
            response.ContentLength64 = errBuffer.Length;
            await response.OutputStream.WriteAsync(errBuffer);
            response.OutputStream.Close();
        }
        else
        {
            response.StatusCode = 404;
            response.Close();
        }
    }

    private async Task<DiscordTokenData?> ExchangeCode(string code)
    {
        using var client = new HttpClient();
        var fields = new List<KeyValuePair<string, string>>
        {
            new("client_id", DiscordClientId),
            new("grant_type", "authorization_code"),
            new("code", code),
            new("redirect_uri", DiscordRedirectUri),
        };

        if (!string.IsNullOrEmpty(_pkceVerifier))
        {
            fields.Add(new("code_verifier", _pkceVerifier));
        }
        else
        {
            fields.Add(new("client_secret", DiscordClientSecret));
        }

        var data = new FormUrlEncodedContent(fields);
        var response = await client.PostAsync("https://discord.com/api/oauth2/token", data);
        var json = await response.Content.ReadAsStringAsync();
        Debug.WriteLine($"Discord token response: {json}");
        var doc = JsonDocument.Parse(json);

        if (doc.RootElement.TryGetProperty("access_token", out var tokenProp))
        {
            Debug.WriteLine("[DLI OAuth] Token exchange OK");
            return new DiscordTokenData
            {
                AccessToken = tokenProp.GetString()!,
                TokenType = doc.RootElement.GetProperty("token_type").GetString()!
            };
        }
        Debug.WriteLine($"[DLI OAuth] Token exchange FAILED: {json}");
        return null;
    }

    private async Task<DiscordUserInfo?> GetDiscordUser(string accessToken)
    {
        using var client = new HttpClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        var response = await client.GetAsync("https://discord.com/api/users/@me");
        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);

        if (doc.RootElement.TryGetProperty("id", out var idProp))
        {
            var avatar = doc.RootElement.TryGetProperty("avatar", out var a) ? a.GetString() ?? "" : "";
            var globalName = doc.RootElement.TryGetProperty("global_name", out var g) ? g.GetString() : null;
            Debug.WriteLine($"[DLI OAuth] User: id={idProp.GetString()}, username={doc.RootElement.GetProperty("username").GetString()}, avatar={avatar}, global_name={globalName}");
            return new DiscordUserInfo
            {
                Id = idProp.GetString()!,
                Username = doc.RootElement.GetProperty("username").GetString()!,
                Discriminator = doc.RootElement.TryGetProperty("discriminator", out var d) ? d.GetString()! : "0",
                Avatar = avatar,
                GlobalName = globalName
            };
        }
        Debug.WriteLine($"[DLI OAuth] GetUser FAILED: {json}");
        return null;
    }

    private class DiscordTokenData
    {
        public string AccessToken { get; set; } = "";
        public string TokenType { get; set; } = "";
    }

    private class DiscordUserInfo
    {
        public string Id { get; set; } = "";
        public string Username { get; set; } = "";
        public string Discriminator { get; set; } = "";
        public string Avatar { get; set; } = "";
        public string? GlobalName { get; set; }
    }

    private static string GenerateCodeVerifier()
    {
        var bytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private static string GenerateCodeChallenge(string verifier)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.ASCII.GetBytes(verifier));
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    #endregion

    private void Titlebar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            WindowState = WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;
        }
        else
        {
            DragMove();
        }
    }

    private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

    private void Maximize_Click(object sender, RoutedEventArgs e) =>
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    private void CloseBtn_MouseEnter(object sender, MouseEventArgs e)
    {
        ((Button)sender).Background = new SolidColorBrush(Color.FromRgb(0xE8, 0x11, 0x23));
        ((Button)sender).Foreground = Brushes.White;
    }

    private void CloseBtn_MouseLeave(object sender, MouseEventArgs e)
    {
        ((Button)sender).Background = Brushes.Transparent;
        ((Button)sender).Foreground = new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88));
    }
}
