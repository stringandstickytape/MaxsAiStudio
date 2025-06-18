# Check if we're on the main branch first
try {
    $currentBranch = git rev-parse --abbrev-ref HEAD
    if ($currentBranch -ne "main") {
        Write-Error "Not on main branch. Currently on: $currentBranch. Exiting script."
        exit 1
    }
    Write-Host "Confirmed on main branch" -ForegroundColor Green
}
catch {
    Write-Error "Failed to check current Git branch: $($_.Exception.Message)"
    exit 1
}

cd .\AiStudio4\AiStudioClient

Remove-Item ".\bin\Debug\net9.0-windows\assets\*" -Recurse -Force


pnpm run build

cd ..


taskkill /f /im aistudio4.exe

dotnet build

cd ..

create-release.bat

