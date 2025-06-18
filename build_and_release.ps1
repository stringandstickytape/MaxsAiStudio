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

# Path to the file
$filePath = ".\AiStudio4\app.xaml.cs"

# Read all lines
$content = Get-Content $filePath

# Regex pattern to match the version line
$pattern = 'public const decimal VersionNumber = ([0-9]*\.?[0-9]+)m;'

# Variable to store the new version
$newVersion = $null

# Process lines
$newContent = $content | ForEach-Object {
    if ($_ -match $pattern) {
        # Extract current version number
        $version = [decimal]$matches[1]

        # Increment by 0.00 (!)
        $newVersion = [decimal]::Round($version + 0.00, 2)

        # Format the line with new version
        # "public const decimal VersionNumber = {0}m;" -f $newVersion
    }
    else {
        $_
    }
}

# Save updated content back to the file
#$newContent | Set-Content $filePath

cd .\AiStudio4\AiStudioClient

pnpm run build

cd ..

Remove-Item ".\bin\Debug\net9.0-windows\assets\*" -Recurse -Force

taskkill /f /im aistudio4.exe

dotnet build

cd ..

create-release.bat

