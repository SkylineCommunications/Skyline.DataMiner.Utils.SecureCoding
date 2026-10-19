[CmdletBinding()]
param([string]$ResultsDirectory = (Join-Path (Split-Path -Parent $PSScriptRoot) 'TestResults/linux'))

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$results = (New-Item -ItemType Directory -Force -Path $ResultsDirectory).FullName

function Assert-LinuxResults([string]$Directory) {
    $windowsOnly = @(
        'SecureCoding.Test.SecureIO.SecurePathWindowsUnitTests.',
        'SecureCoding.Test.SecureIO.StringExtensionsWindowsTests.IsPathValidUsesCurrentPlatformOnWindows',
        'SecureCoding.Test.SecureReflection.SecureAssemblyLinuxTests.ValidateAssemblySignature_Linux_And_Windows_Agree',
        'SecureCoding.Test.SecureReflection.SecureAssemblySnapshotTests.WindowsNativeVerificationUsesHandleNotReplacedPath',
        'SecureCoding.Test.SecureReflection.SecureAssemblySnapshotTests.WindowsSnapshotDeniesMutationAndIsDeletedAfterVerification',
        'SecureCoding.Test.SecureReflection.SecureAssemblySnapshotTests.WindowsSnapshotFailsClosedAndReleasesInvalidSignature',
        'SecureCoding.Test.SecureReflection.SecureAssemblySnapshotTests.WindowsPinsTheVerifiedSigner',
        'SecureCoding.Test.SecureReflection.SecureAssemblySnapshotTests.WindowsRejectsCorruptCmsSignatureEvenWhenPeDigestIsUnchanged',
        'SecureCoding.Test.SecureReflection.SecureAssemblySnapshotTests.WindowsNativeLayoutsMatchCurrentProcessAbi',
        'SecureCoding.Test.SecureReflection.SecureAssemblySnapshotTests.WindowsConcurrentSnapshotsRemainIndependent'
    )
    $required = [ordered]@{
        library = @(
            'SecureCoding.Test.SecureIO.SecurePathLinuxUnitTests.',
            'SecureCoding.Test.SecureIO.StringExtensionsLinuxTests.',
            'SecureCoding.Test.SecureIO.StringExtensionsWindowsTests.',
            'SecureCoding.Test.SecureIO.PathContractTests.',
            'SecureCoding.Test.SecureIO.PathContractTests.LinuxLexicalValidityDoesNotImplySafeConstruction',
            'SecureCoding.Test.SecureReflection.SecureAssemblyLinuxTests.',
            'SecureCoding.Test.SecureReflection.SecureAssemblyLoaderTests.',
            'SecureCoding.Test.SecureReflection.SecureAssemblySnapshotTests.ValidatedSnapshotSurvivesSourceReplacementOrDeletion',
            'SecureCoding.Test.SecureReflection.SecureAssemblySnapshotTests.PublicLoadersUseSnapshotAndReleaseSource',
            'SecureCoding.Test.SecureReflection.SecureAssemblySnapshotTests.PublicLoadersRejectOversizedImages',
            'SecureCoding.Test.SecureReflection.AuthenticodeHardeningTests.',
            'SecureCoding.Test.SecureReflection.AuthenticodeHardeningTests.LinuxLoadsVerifiedBytesWithoutPathLocation'
        )
        analyzers = @()
    }
    $failures = @()
    foreach ($suite in $required.Keys) {
        $report = Join-Path $Directory "$suite.trx"
        if (-not (Test-Path -LiteralPath $report)) {
            $failures += "Missing report: $report"
            continue
        }
        [xml]$document = Get-Content -LiteralPath $report -Raw
        $ns = New-Object System.Xml.XmlNamespaceManager($document.NameTable)
        $ns.AddNamespace('t', 'http://microsoft.com/schemas/VisualStudio/TeamTest/2010')
        if ($suite -eq 'analyzers') {
            $counters = $document.SelectSingleNode('/t:TestRun/t:ResultSummary/t:Counters', $ns)
            if ($null -eq $counters) {
                $failures += "${suite}: missing ResultSummary/Counters"
                continue
            }
            $counts = @{}
            foreach ($attribute in @('total', 'executed', 'passed', 'failed', 'error', 'timeout', 'aborted')) {
                $count = 0
                if (-not [int]::TryParse($counters.GetAttribute($attribute), [ref]$count) -or $count -lt 0) {
                    $failures += "${suite}: invalid or missing $attribute counter"
                }
                $counts[$attribute] = $count
            }
            if ($counts.total -eq 0 -or $counts.executed -eq 0 -or $counts.passed -eq 0 -or
                $counts.failed -gt 0 -or $counts.error -gt 0 -or $counts.timeout -gt 0 -or $counts.aborted -gt 0) {
                $failures += "${suite}: unsuccessful test counters (total=$($counts.total), executed=$($counts.executed), passed=$($counts.passed), failed=$($counts.failed), error=$($counts.error), timeout=$($counts.timeout), aborted=$($counts.aborted))"
            }
            Write-Host "${suite}: $($counts.passed) passed, $($counts.total) total"
            continue
        }
        $definitions = @{}
        foreach ($test in $document.SelectNodes('/t:TestRun/t:TestDefinitions/t:UnitTest', $ns)) {
            $method = $test.SelectSingleNode('t:TestMethod', $ns)
            if ($null -ne $method) {
                $definitions[$test.GetAttribute('id')] = $method.GetAttribute('className').Split(',')[0] + '.' + $method.GetAttribute('name')
            }
        }
        $passed = @()
        $skipped = 0
        foreach ($result in $document.SelectNodes('/t:TestRun/t:Results/t:UnitTestResult', $ns)) {
            $name = $definitions[$result.GetAttribute('testId')]
            $outcome = $result.GetAttribute('outcome')
            if (-not $name) {
                $failures += "${suite}: result has no test definition"
            } elseif ($outcome -eq 'Passed') {
                $passed += $name
            } elseif ($outcome -eq 'NotExecuted' -and $suite -eq 'library' -and
                @($windowsOnly | Where-Object { $name.StartsWith($_, [StringComparison]::Ordinal) }).Count -gt 0) {
                $skipped++
            } else {
                $failures += "${suite}: unexpected ${outcome}: $name"
            }
        }
        foreach ($prefix in $required[$suite]) {
            if (@($passed | Where-Object { $_.StartsWith($prefix, [StringComparison]::Ordinal) }).Count -eq 0) {
                $failures += "${suite}: no passed test for $prefix"
            }
        }
        $summary = $document.SelectSingleNode('/t:TestRun/t:ResultSummary', $ns)
        if ($null -eq $summary -or $summary.GetAttribute('outcome') -notin @('Completed', 'Passed')) {
            $failures += "${suite}: unsuccessful or missing result summary"
        }
        Write-Host "${suite}: $($passed.Count) passed, $skipped expected Windows-only skips"
    }
    if ($failures.Count) { throw ($failures -join [Environment]::NewLine) }
}

if ($env:OS -eq 'Windows_NT') {
    if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
        throw 'Install Docker Desktop and start its Linux container engine, then retry.'
    }
    $os = & docker info --format '{{.OSType}}'
    if ($LASTEXITCODE -ne 0 -or $os -ne 'linux') {
        throw 'Start Docker Desktop with the Linux container engine, then retry.'
    }
    # Copy uncommitted source without sharing Windows build outputs with Linux.
    $command = 'set -euo pipefail; mkdir /work; tar -C /src --exclude=.git --exclude=.vs --exclude=bin --exclude=obj --exclude=TestResults -cf - . | tar -C /work -xf -; pwsh -NoProfile -File /work/scripts/Test-Linux.ps1 -ResultsDirectory /results'
    & docker run --rm --mount "type=bind,source=$root,target=/src,readonly" --mount "type=bind,source=$results,target=/results" mcr.microsoft.com/dotnet/sdk:10.0 bash -c $command
    if ($LASTEXITCODE -ne 0) { throw "Linux test run failed (exit $LASTEXITCODE). See $results." }
    return
}
if (-not $IsLinux) { throw 'This runner requires Linux or Windows with Linux containers.' }
Get-Command dotnet -ErrorAction Stop | Out-Null
$projects = [ordered]@{
    library = 'SecureCoding.Test/SecureCoding.Test.csproj'
    analyzers = 'Analyzers/Analyzers.Test/Analyzers.Test.csproj'
}
$testFilter = 'FullyQualifiedName~SecureCoding.Test.SecureIO.|FullyQualifiedName~SecureCoding.Test.SecureReflection.'
foreach ($suite in $projects.Keys) {
    Remove-Item -LiteralPath (Join-Path $results "$suite.trx") -Force -ErrorAction SilentlyContinue
}
$failed = $false
Push-Location $root
try {
    foreach ($suite in $projects.Keys) {
        $filterArguments = @()
        if ($suite -eq 'library') { $filterArguments = @('--filter', $testFilter) }
        & dotnet test $projects[$suite] -f net10.0 -c Release @filterArguments --logger "trx;LogFileName=$suite.trx" --results-directory $results -- RunConfiguration.TreatNoTestsAsError=true
        if ($LASTEXITCODE -ne 0) { $failed = $true }
    }
    Assert-LinuxResults $results
    if ($failed) { throw 'One or more dotnet test commands failed.' }
} finally {
    Pop-Location
}
Write-Host "Linux test results: $results"
