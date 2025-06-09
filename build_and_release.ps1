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

        # Increment by 0.01
        $newVersion = [decimal]::Round($version + 0.01, 2)

        # Format the line with new version
        "public const decimal VersionNumber = {0}m;" -f $newVersion
    }
    else {
        $_
    }
}

# Save updated content back to the file
$newContent | Set-Content $filePath

# Git operations
if ($newVersion -ne $null) {
    Write-Host "New version: $newVersion" -ForegroundColor Green
    
    try {
        # Change to the directory containing the file
        $repoPath = Split-Path $filePath -Parent
        Push-Location $repoPath
        
        # Add the modified file to staging
        git add $filePath
        Write-Host "Added file to staging area" -ForegroundColor Yellow
        
        # Commit the changes
        $commitMessage = "Bump version to $newVersion"
        git commit -m $commitMessage
        Write-Host "Committed changes: $commitMessage" -ForegroundColor Yellow
        
        # Create a tag with the version number
        $tagName = $newVersion.ToString()
        git tag $tagName
        Write-Host "Created tag: $tagName" -ForegroundColor Yellow
        
        # Push the commit and tag
        git push
        Write-Host "Pushed commits to remote" -ForegroundColor Yellow
        
        git push origin $tagName
        Write-Host "Pushed tag '$tagName' to remote" -ForegroundColor Green
        
        Write-Host "Version bump and Git operations completed successfully!" -ForegroundColor Green
    }
    catch {
        Write-Error "Git operation failed: $($_.Exception.Message)"
    }
    finally {
        # Return to original location
        Pop-Location
    }
}
else {
    Write-Warning "No version number found or updated in the file"
}

cd .\AiStudio4\AiStudioClient

pnpm run build

cd ..

Remove-Item ".\bin\Debug\net9.0-windows\assets\*" -Recurse -Force

taskkill /f /im aistudio4.exe

dotnet build

cd ..

create-release.bat

