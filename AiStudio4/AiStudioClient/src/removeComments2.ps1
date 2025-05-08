function Remove-TsComments {
    param (
        [Parameter(Mandatory=$true)]
        [string]$FilePath
    )
    
    try {
        # Read file as a single string to preserve formatting
        $content = [System.IO.File]::ReadAllText($FilePath)
        $originalContent = $content
        
        # Initialize state tracking
        $inSingleQuote = $false
        $inDoubleQuote = $false
        $inTemplate = $false
        $inMultilineComment = $false
        $inRegex = $false
        $escapeNext = $false
        $result = New-Object System.Text.StringBuilder
        
        # Process the file character by character
        for ($i = 0; $i -lt $content.Length; $i++) {
            $char = $content[$i]
            $nextChar = if ($i + 1 -lt $content.Length) { $content[$i + 1] } else { $null }
            
            # Handle escape sequences
            if ($escapeNext) {
                [void]$result.Append($char)
                $escapeNext = $false
                continue
            }
            
            if (($inSingleQuote -or $inDoubleQuote -or $inTemplate -or $inRegex) -and $char -eq '\') {
                [void]$result.Append($char)
                $escapeNext = $true
                continue
            }
            
            # Track string literals
            if ($char -eq "'" -and -not $inDoubleQuote -and -not $inTemplate -and -not $inMultilineComment) {
                $inSingleQuote = -not $inSingleQuote
                [void]$result.Append($char)
                continue
            }
            
            if ($char -eq '"' -and -not $inSingleQuote -and -not $inTemplate -and -not $inMultilineComment) {
                $inDoubleQuote = -not $inDoubleQuote
                [void]$result.Append($char)
                continue
            }
            
            # Track template literals (backticks)
            if ($char -eq '`' -and -not $inSingleQuote -and -not $inDoubleQuote -and -not $inMultilineComment) {
                $inTemplate = -not $inTemplate
                [void]$result.Append($char)
                continue
            }
            
            # Track regex literals
            if ($char -eq '/' -and $nextChar -ne '/' -and $nextChar -ne '*' -and -not $inSingleQuote -and -not $inDoubleQuote -and -not $inTemplate -and -not $inMultilineComment -and -not $inRegex) {
                # Basic regex detection
                $prevChar = if ($i -gt 0) { $content[$i - 1] } else { $null }
                if ($prevChar -eq '=' -or $prevChar -eq '(' -or $prevChar -eq ',' -or $prevChar -eq ':' -or $prevChar -eq '[') {
                    $inRegex = $true
                    [void]$result.Append($char)
                    continue
                }
            }
            
            if ($char -eq '/' -and $inRegex) {
                $inRegex = $false
                [void]$result.Append($char)
                continue
            }
            
            # Handle comments, but only when not in any kind of string or regex
            if (-not $inSingleQuote -and -not $inDoubleQuote -and -not $inTemplate -and -not $inRegex -and -not $inMultilineComment) {
                # Start of single-line comment
                if ($char -eq '/' -and $nextChar -eq '/') {
                    # Skip until end of line
                    while ($i + 1 -lt $content.Length -and $content[$i + 1] -ne "`n") {
                        $i++
                    }
                    continue
                }
                
                # Start of multi-line comment
                if ($char -eq '/' -and $nextChar -eq '*') {
                    $inMultilineComment = $true
                    [void]$result.Append($char)
                    [void]$result.Append($nextChar)
                    $i++
                    continue
                }
            }
            
            # End of multi-line comment
            if ($inMultilineComment -and $char -eq '*' -and $nextChar -eq '/') {
                $inMultilineComment = $false
                [void]$result.Append($char)
                [void]$result.Append($nextChar)
                $i++
                continue
            }
            
            # Add character to result
            [void]$result.Append($char)
        }
        
        $newContent = $result.ToString()
        
        # Only write back if there were changes
        if ($originalContent -ne $newContent) {
            [System.IO.File]::WriteAllText($FilePath, $newContent)
            return $true
        }
        
        return $false
    }
    catch {
        Write-Error "Error processing file $FilePath`: $_"
        return $false
    }
}

# Find and process all TS/TSX files
$count = 0
Get-ChildItem -Recurse -Include *.ts,*.tsx | ForEach-Object {
    $filePath = $_.FullName
    Write-Host "Processing $filePath..." -NoNewline
    
    if (Remove-TsComments -FilePath $filePath) {
        Write-Host " Comments removed!" -ForegroundColor Green
        $count++
    }
    else {
        Write-Host " No changes made." -ForegroundColor Gray
    }
}

Write-Host "`nRemoved comments from $count files." -ForegroundColor Cyan