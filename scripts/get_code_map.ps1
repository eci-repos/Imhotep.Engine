param(
    [string]$SolutionRoot = (Get-Location).Path
)

Write-Host "Scanning solution root: $SolutionRoot"

# Get all C# files
$csFiles = Get-ChildItem -Path $SolutionRoot -Recurse -Filter *.cs -ErrorAction SilentlyContinue

# Regex patterns
$namespacePattern = 'namespace\s+([\w\.]+)'
$classPattern     = '(public|private|internal|protected)?\s*(abstract\s+|static\s+|sealed\s+)?class\s+(\w+)'
$interfacePattern = '(public|private|internal|protected)?\s*interface\s+(\w+)'
$enumPattern      = '(public|private|internal|protected)?\s*enum\s+(\w+)'
$methodPattern    = '(public|private|internal|protected)\s+(static\s+)?[\w\<\>\[\],]+\s+(\w+)\s*\('

$result = @()

foreach ($file in $csFiles) {
    try {
        $content = Get-Content $file.FullName -Raw -ErrorAction Stop
    }
    catch {
        Write-Warning "Skipping file: $($file.Name)"
        continue
    }

    $namespaces = [regex]::Matches($content, $namespacePattern)
    $classes    = [regex]::Matches($content, $classPattern)
    $interfaces = [regex]::Matches($content, $interfacePattern)
    $enums      = [regex]::Matches($content, $enumPattern)
    $methods    = [regex]::Matches($content, $methodPattern)

    # Default namespace if none found
    if ($namespaces.Count -eq 0) {
        $namespaces = @()
        $namespaces += [PSCustomObject]@{ Groups = @(@{ Value = "" }, @{ Value = "Global" }) }
    }

    foreach ($ns in $namespaces) {
        $namespaceName = $ns.Groups[1].Value

        # Classes + Methods
        foreach ($class in $classes) {
            $className = $class.Groups[3].Value

            foreach ($method in $methods) {
                $methodName = $method.Groups[3].Value

                $result += [PSCustomObject]@{
                    File       = $file.Name
                    Namespace  = $namespaceName
                    Type       = "Class"
                    Name       = $className
                    MemberType = "Method"
                    MemberName = $methodName
                }
            }
        }

        # Interfaces
        foreach ($interface in $interfaces) {
            $result += [PSCustomObject]@{
                File       = $file.Name
                Namespace  = $namespaceName
                Type       = "Interface"
                Name       = $interface.Groups[2].Value
                MemberType = ""
                MemberName = ""
            }
        }

        # Enums
        foreach ($enum in $enums) {
            $result += [PSCustomObject]@{
                File       = $file.Name
                Namespace  = $namespaceName
                Type       = "Enum"
                Name       = $enum.Groups[2].Value
                MemberType = ""
                MemberName = ""
            }
        }
    }
}

# Remove duplicate rows (very common with regex scanning)
$result = $result | Sort-Object File, Namespace, Type, Name, MemberName -Unique

# Output
$result | Format-Table -AutoSize

# Export CSV
$outputFile = Join-Path $SolutionRoot "CodeMap.csv"
$result | Export-Csv -Path $outputFile -NoTypeInformation

Write-Host "`n✅ Code map exported to: $outputFile"