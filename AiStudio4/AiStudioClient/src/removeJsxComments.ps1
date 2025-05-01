# Process TSX files with proper JSX comment handling
Get-ChildItem -Path . -Include *.tsx -Recurse | ForEach-Object {
    $content = Get-Content -Path $_.FullName -Raw
    $lines = $content -split "`n"
    $firstLine = $lines[0]
    $restLines = ($lines | Select-Object -Skip 1) -join "`n"
    
    # Remove JSX comments completely (including the {} braces)
    $restLines = [regex]::Replace($restLines, '{/\*[\s\S]*?\*/}', '')
    
    # Remove regular single-line comments (but not JSX comments)
    $restLines = [regex]::Replace($restLines, '(?<![:<])//.*', '')
    
    # Remove regular multi-line comments
    $restLines = [regex]::Replace($restLines, '/\*[\s\S]*?\*/', '')
    
    # Clean up consecutive empty lines
    $restLines = [regex]::Replace($restLines, '(\r?\n){3,}', "`n`n")
    
    Set-Content -Path $_.FullName -Value ($firstLine + "`n" + $restLines)
}