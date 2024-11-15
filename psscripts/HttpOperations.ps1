function Save-HttpFile($httpUrl, $httpLatestFile, $localFilePath, $syncHash) {
    $downloadUrl = [Uri]::new([Uri]$httpUrl, $httpLatestFile).AbsoluteUri

    Write-Log -Message "7 - Начало скачивания файла '$downloadUrl'..."

    $job = Start-Job -ScriptBlock {
        param($downloadUrl, $localFilePath)
        
        try {
            $ProgressPreference = 'SilentlyContinue'
            Invoke-WebRequest -Uri $downloadUrl -OutFile $localFilePath
            return @{ Success = $true; Message = "Файл успешно скачан в $localFilePath" }
        }
        catch {
            return @{ Success = $false; Message = "Ошибка при скачивании файла: $_" }
        }
        finally {
            $ProgressPreference = 'Continue'
        }
    } -ArgumentList $downloadUrl, $localFilePath

    while ($job.State -eq 'Running') {
        Start-Sleep -Milliseconds 100
        [System.Windows.Forms.Application]::DoEvents()
    }

    $result = Receive-Job -Job $job
    Remove-Job -Job $job

    if ($result.Success) {
        Write-Log -Message $result.Message -Mode success
    } else {
        Write-Log -Message $result.Message -Mode error
    }
}

function Get-HttpLatestFile {
    param (
        [string]$httpUrl,
        [string]$httpPattern,
        $syncHash
    )

    $job = Start-Job -ScriptBlock {
        param($httpUrl, $httpPattern)
        
        try {
            $webClient = New-Object System.Net.WebClient
            $htmlContent = $webClient.DownloadString($httpUrl)
            $regexMatches = [regex]::Matches($htmlContent, $httpPattern)

            if ($regexMatches.Count -gt 0) {
                $lastMatch = $regexMatches[$regexMatches.Count - 1]
                $latestFile = $lastMatch.Groups[1].Value
                return @{ Success = $true; LatestFile = $latestFile }
            } else {
                return @{ Success = $false; Message = "Не найдено файлов на https!" }
            }
        }
        catch {
            return @{ Success = $false; Message = "Ошибка при загрузке или парсинге html: $_" }
        }
        finally {
            if ($webClient) {
                $webClient.Dispose()
            }
        }
    } -ArgumentList $httpUrl, $httpPattern

    while ($job.State -eq 'Running') {
        Start-Sleep -Milliseconds 100
        [System.Windows.Forms.Application]::DoEvents()
    }

    $result = Receive-Job -Job $job
    Remove-Job -Job $job

    if ($result.Success) {
        Write-Log "Файл на https найден: $($result.LatestFile)"
        return $result.LatestFile
    } else {
        Write-Log $result.Message -Mode error
        return $null
    }
}