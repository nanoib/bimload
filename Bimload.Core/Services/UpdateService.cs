using System.Runtime.Versioning;
using Bimload.Core.Logging;
using Bimload.Core.Models;

namespace Bimload.Core.Services;

[SupportedOSPlatform("windows")]
public class UpdateService : IUpdateService
{
    private readonly IWmiService _wmiService;
    private readonly IVersionService _versionService;
    private readonly IHttpClient _httpClient;
    private readonly IProgramInstaller _programInstaller;
    private readonly ILogger _logger;

    public UpdateService(
        IWmiService wmiService,
        IVersionService versionService,
        IHttpClient httpClient,
        IProgramInstaller programInstaller,
        ILogger logger)
    {
        _wmiService = wmiService ?? throw new ArgumentNullException(nameof(wmiService));
        _versionService = versionService ?? throw new ArgumentNullException(nameof(versionService));
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _programInstaller = programInstaller ?? throw new ArgumentNullException(nameof(programInstaller));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<UpdateResult> UpdateAsync(Credentials credentials, Action<long, long?>? downloadProgressCallback = null)
    {
        if (credentials == null)
        {
            throw new ArgumentNullException(nameof(credentials));
        }

        _logger.Log($"Начало обновления для продукта: {credentials.ProductName}");

        // Get installed program
        var installedProgram = _wmiService.GetLatestInstalledProgram(credentials.ProductName ?? string.Empty);
        
        // Calculate pcLatestVersion
        string? pcLatestVersion = null;
        
        if (installedProgram == null)
        {
            _logger.Log("Программа не найдена в системе", LogLevel.Warning);
            _logger.Log("Текущая версия: не установлена");
        }
        else
        {
            _logger.Log($"Найдена программа: {installedProgram.Name}, версия WMI: {installedProgram.Version}");
            
            if (string.IsNullOrWhiteSpace(credentials.ProductVersionPattern))
            {
                _logger.Log("Паттерн productVersionPattern не указан в конфигурации", LogLevel.Warning);
                _logger.Log("Текущая версия: не установлена");
            }
            else
            {
                try
                {
                    // Debug: show what we're working with
                    _logger.Log($"Попытка извлечь версию. WMI версия: '{installedProgram.Version}', паттерн: '{credentials.ProductVersionPattern}'");
                    
                    pcLatestVersion = _versionService.ExtractVersionFromProductVersion(
                        installedProgram.Version ?? string.Empty, 
                        credentials.ProductVersionPattern);
                    
                    if (pcLatestVersion == null)
                    {
                        _logger.Log($"Не удалось извлечь версию из '{installedProgram.Version}' по паттерну '{credentials.ProductVersionPattern}'", LogLevel.Warning);
                        _logger.Log($"Проверьте правильность паттерна. Версия WMI: '{installedProgram.Version}', паттерн: '{credentials.ProductVersionPattern}'", LogLevel.Warning);
                        _logger.Log("Текущая версия: не установлена");
                    }
                    else
                    {
                        _logger.Log($"Текущая версия: {pcLatestVersion}");
                    }
                }
                catch (ArgumentException ex)
                {
                    _logger.Log($"Ошибка в паттерне productVersionPattern: {ex.Message}", LogLevel.Error);
                    _logger.Log("Текущая версия: не установлена");
                }
            }
        }

        // Get latest file from HTTP
        if (string.IsNullOrWhiteSpace(credentials.HttpUrl) || string.IsNullOrWhiteSpace(credentials.HttpPattern))
        {
            return new UpdateResult
            {
                Status = "Ошибка: HTTP URL или паттерн не указаны",
                OldVersion = pcLatestVersion,
                NewVersion = pcLatestVersion
            };
        }

        var latestFile = await _httpClient.GetLatestFileAsync(credentials.HttpUrl, credentials.HttpPattern);
        if (latestFile == null)
        {
            return new UpdateResult
            {
                Status = "Ошибка: Не удалось получить информацию о последней версии",
                OldVersion = pcLatestVersion,
                NewVersion = pcLatestVersion
            };
        }

        _logger.Log($"Найден файл на сервере: {latestFile}");

        // Extract version from filename
        var serverLatestVersion = !string.IsNullOrWhiteSpace(credentials.FileVersionPattern)
            ? _versionService.ExtractVersionFromFileName(latestFile, credentials.FileVersionPattern)
            : null;

        if (serverLatestVersion == null)
        {
            return new UpdateResult
            {
                Status = "Ошибка: Не удалось извлечь версию из имени файла",
                OldVersion = pcLatestVersion,
                NewVersion = pcLatestVersion
            };
        }

        _logger.Log($"Версия на сервере: {serverLatestVersion}");

        // Compare versions
        var updateNeeded = _versionService.CompareVersions(pcLatestVersion, serverLatestVersion);
        if (!updateNeeded)
        {
            _logger.Log("Обновление не требуется");
            return new UpdateResult
            {
                Status = "Обновление не требуется",
                OldVersion = pcLatestVersion,
                NewVersion = pcLatestVersion
            };
        }

        // Download file if needed
        var localFilePath = Path.Combine(credentials.LocalPath ?? string.Empty, latestFile);
        if (!File.Exists(localFilePath))
        {
            _logger.Log($"Скачивание файла: {latestFile}");
            var downloadUrl = new Uri(new Uri(credentials.HttpUrl), latestFile).AbsoluteUri;
            _logger.Log($"URL для загрузки: {downloadUrl}");
            await _httpClient.DownloadFileAsync(downloadUrl, localFilePath, downloadProgressCallback);
            _logger.Log("Файл успешно скачан", LogLevel.Success);
        }
        else
        {
            _logger.Log("Используется уже скачанный файл");
        }

        // Uninstall old version
        if (installedProgram != null)
        {
            _logger.Log($"Удаление старой версии: {installedProgram.Name}");
            await _programInstaller.UninstallProgramAsync(installedProgram);
            _logger.Log("Старая версия удалена", LogLevel.Success);
        }

        // Install new version
        _logger.Log($"Установка новой версии: {localFilePath}");
        await _programInstaller.InstallProgramAsync(localFilePath);
        _logger.Log("Новая версия установлена", LogLevel.Success);

        // Verify installation
        var updatedProgram = _wmiService.GetLatestInstalledProgram(credentials.ProductName ?? string.Empty);
        var newVersion = updatedProgram != null && !string.IsNullOrWhiteSpace(credentials.ProductVersionPattern)
            ? _versionService.ExtractVersionFromProductVersion(updatedProgram.Version ?? string.Empty, credentials.ProductVersionPattern)
            : null;

        var status = DetermineStatus(pcLatestVersion, newVersion, serverLatestVersion);

        return new UpdateResult
        {
            Status = status,
            OldVersion = pcLatestVersion,
            NewVersion = newVersion
        };
    }

    private static string DetermineStatus(string? oldVersion, string? newVersion, string? serverVersion)
    {
        if (newVersion == null && serverVersion != null)
        {
            return "Не найдено. Скорее всего, программа не установилась";
        }

        if (newVersion == serverVersion)
        {
            return "Программа обновлена";
        }

        if (oldVersion != null && newVersion != null && 
            int.TryParse(oldVersion, out var oldInt) && 
            int.TryParse(newVersion, out var newInt) && 
            newInt < oldInt)
        {
            return "Программа не обновилась или обновилась некорректно! Текущая версия ниже новейшей.";
        }

        return "Неизвестный статус";
    }
}

