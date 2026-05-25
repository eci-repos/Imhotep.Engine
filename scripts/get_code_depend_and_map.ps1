param(
    [string]$SolutionRoot = (Get-Location).Path
)

Write-Host "Scanning solution root: $SolutionRoot"

# Get all C# files
$csFiles = Get-ChildItem -Path $SolutionRoot -Recurse -Filter *.cs

# Regex patterns
$namespacePattern  = 'namespace\s+([\w\.]+)'
$classPattern      = '(public|private|internal|protected)?\s*(abstract\s+|static\s+|sealed\s+)?class\s+(\w+)'
$interfacePattern  = '(public|private|internal|protected)?\s*interface\s+(\w+)'
$enumPattern       = '(public|private|internal|protected)?\s*enum\s+(\w+)'
$methodPattern     = '(public|private|internal|protected)\s+(static\s+)?[\w\<\>\[\],]+\s+(\w+)\s*\('

# New dependency patterns
$usingPattern      = 'using\s+([\w\.]+);'
$newPattern        = 'new\s+(\w+)\s*\('
$callPattern       = '(\w+)\.(\w+)\s*\('

$result = @()

foreach ($file in $csFiles) {
    $content = Get-Content $file.FullName -Raw

    $namespaces = :Matches($content, $namespacePattern)
    $classes    = :Matches($content, $classPattern)
    $interfaces = :Matches($content, $interfacePattern)
    $enums      = :Matches($content, $enumPattern)
    $methods    = :Matches($content, $methodPattern)

    $usings     = :Matches($content, $usingPattern)
    $newObjects = :Matches($content, $newPattern)
    $calls      = :Matches($content, $callPattern)

    foreach ($ns in $namespaces) {
        $namespaceName = $ns.Groups[1].Value

        # Classes + methods
        foreach ($class in $classes) {
            $className = $class.Groups[3].Value

            foreach ($method in $methods) {
                $methodName = $method.Groups[3].Value

                $result += [PSCustomObject]@{
                    File            = $file.FullName
                    Namespace       = $namespaceName
                    Type            = "Class"
                    Name            = $className
                    MemberType      = "Method"
                    MemberName      = $methodName
                    DependencyType  = ""
                    DependencyName  = ""
                }
            }
        }

        # Interfaces
        foreach ($interface in $interfaces) {
            $result += [PSCustomObject]@{
                File            = $file.FullName
                Namespace       = $namespaceName
                Type            = "Interface"
                Name            = $interface.Groups[2].Value
                MemberType      = ""
                MemberName      = ""
                DependencyType  = ""
                DependencyName  = ""
            }
        }

        # Enums
        foreach ($enum in $enums) {
            $result += [PSCustomObject]@{
                File            = $file.FullName
                Namespace       = $namespaceName
                Type            = "Enum"
                Name            = $enum.Groups[2].Value
                MemberType      = ""
                MemberName      = ""
                DependencyType  = ""
                DependencyName  = ""
            }
        }

        # Using dependencies
        foreach ($u in $usings) {
            $result += [PSCustomObject]@{
                File            = $file.FullName
                Namespace       = $namespaceName
                Type            = ""
                Name            = ""
                MemberType      = ""
                MemberName      = ""
                DependencyType  = "Using"
                DependencyName  = $u.Groups[1].Value
            }
        }

        # Object instantiation dependencies
        foreach ($n in $newObjects) {
            $result += [PSCustomObject]@{
                File            = $file.FullName
                Namespace       = $namespaceName
                Type            = ""
                Name            = ""
                MemberType      = ""
                MemberName      = ""
                DependencyType  = "New"
                DependencyName  = $n.Groups[1].Value
            }
        }

        # Method call dependencies
        foreach ($c in $calls) {
            $result += [PSCustomObject]@{
                File            = $file.FullName
                Namespace       = $namespaceName
                Type            = ""
                Name            = ""
                MemberType      = ""
                MemberName      = ""
                DependencyType  = "Call"
                DependencyName  = "$($c.Groups[1].Value).$($c.Groups[2].Value)"
            }
        }
    }
}

# Output
$result | Format-Table -AutoSize

# Export CSV
$outputFile = Join-Path $SolutionRoot "CodeMap.csv"
$result | Export-Csv -Path $outputFile -NoTypeInformation

Write-Host "`n✅ Code map exported to: $outputFile"
