<#
.SYNOPSIS
    跑 EVEBox 全量单元测试。

.DESCRIPTION
    优先用标准的 dotnet test；如果当前环境不允许（DSH 沙箱下 VSTest 测试宿主
    拿不到父进程句柄会直接崩掉，报 System.Diagnostics.Process.GetOrOpenProcessHandle
    相关异常），自动回退到 xunit 自带控制台运行器 —— 它不经 VSTest 那套回调，可正常执行。

.PARAMETER Filter
    只跑名字匹配的用例，例如 -Filter Recycle。等价于 xunit 的 -method 过滤。

.PARAMETER NoBuild
    跳过 dotnet build（默认会先构建一次，避免跑到旧程序集）。

.EXAMPLE
    .\run-tests.ps1
    .\run-tests.ps1 -Filter BanBenHao

.NOTES
    本机执行策略禁止直接运行 .ps1 时，用：
    powershell -NoProfile -ExecutionPolicy Bypass -File tests\run-tests.ps1
#>
[CmdletBinding()]
param(
    [string]$Filter,
    [switch]$NoBuild
)

$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

$repoRoot = Split-Path -Parent $PSScriptRoot
$testProject = Join-Path $PSScriptRoot 'EVEBox.Tests\EVEBox.Tests.csproj'
$testDll = Join-Path $PSScriptRoot 'EVEBox.Tests\bin\Debug\net8.0-windows\EVEBox.Tests.dll'

# xunit 控制台运行器（仅本机 nuget 缓存，不入库）。缺了会自动还原一次。
$xunitRunnerVersion = '2.5.3'
$xunitRunner = Join-Path $env:USERPROFILE ".nuget\packages\xunit.runner.console\$xunitRunnerVersion\tools\netcoreapp2.0\xunit.console.dll"

if (-not $NoBuild) {
    Write-Host '== 构建 ==' -ForegroundColor Cyan
    dotnet build (Join-Path $repoRoot 'EVEBox.sln') -v q --nologo
    if ($LASTEXITCODE -ne 0) { Write-Host '构建失败' -ForegroundColor Red; exit 1 }
}

if (-not (Test-Path $testDll)) {
    Write-Host "找不到测试程序集：$testDll，请先去掉 -NoBuild 重新运行" -ForegroundColor Red
    exit 1
}

function Invoke-StandardTest {
    Write-Host '== dotnet test ==' -ForegroundColor Cyan
    $env:DOTNET_CLI_UI_LANGUAGE = 'zh-CN'

    # dotnet test 的失败信息走 stderr；在 $ErrorActionPreference='Stop' 下，
    # 2>&1 会把 stderr 当成终止错误直接中断脚本，导致永远走不到回退分支。
    # 这里临时放宽，靠退出码判断结果。
    $savedPreference = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try {
        $output = dotnet test $testProject --no-build --nologo 2>&1
        $exitCode = $LASTEXITCODE
    }
    finally {
        $ErrorActionPreference = $savedPreference
    }

    $output | Select-Object -Last 12 | ForEach-Object { Write-Host $_ }

    if ($exitCode -eq 0) { return $true }

    # 判定"宿主崩溃"而不是"用例失败"：用例失败时 dotnet test 也会非 0 退出，
    # 但输出里会有失败字样，那种情况不该回退（回退只会重复同样的失败）。
    $joined = ($output | Out-String)
    if ($joined -match 'GetOrOpenProcessHandle|VSTest|测试运行已中止|testhost|Testhost') {
        Write-Host ''
        Write-Host 'dotnet test 的测试宿主在本环境不可用，改用 xunit 控制台运行器。' -ForegroundColor Yellow
        return $false
    }
    return $true   # 是用例真的失败，如实返回
}

function Invoke-XunitConsole {
    if (-not (Test-Path $xunitRunner)) {
        Write-Host "正在获取 xunit 控制台运行器 $xunitRunnerVersion ..." -ForegroundColor Yellow
        $tempDir = Join-Path $env:TEMP ("evebox-xunit-" + [guid]::NewGuid().ToString('N'))
        New-Item -ItemType Directory -Path $tempDir -Force | Out-Null
        try {
            dotnet new classlib -n Probe -o $tempDir --force | Out-Null
            Push-Location (Join-Path $tempDir 'Probe')
            try { dotnet add package xunit.runner.console --version $xunitRunnerVersion | Out-Null }
            finally { Pop-Location }
        }
        finally {
            # 临时目录送回收站，不做永久删除（项目规则第 1 条）
            if (Test-Path $tempDir) {
                Add-Type -AssemblyName Microsoft.VisualBasic
                [Microsoft.VisualBasic.FileIO.FileSystem]::DeleteDirectory(
                    $tempDir, 'OnlyErrorDialogs', 'SendToRecycleBin')
            }
        }
        if (-not (Test-Path $xunitRunner)) {
            Write-Host '未能获取 xunit 控制台运行器，无法继续。' -ForegroundColor Red
            exit 1
        }
    }

    Write-Host '== xunit 控制台运行器 ==' -ForegroundColor Cyan
    # 该运行器面向 netcoreapp2.0，需要前滚到本机已有运行时
    $env:DOTNET_ROLL_FORWARD = 'LatestMajor'

    $runnerArgs = @('exec', $xunitRunner, $testDll, '-nologo')
    if ($Filter) { $runnerArgs += @('-method', $Filter) }

    & dotnet @runnerArgs
    return $LASTEXITCODE
}

$standardOk = Invoke-StandardTest
if (-not $standardOk) {
    $fallbackCode = Invoke-XunitConsole
    exit $fallbackCode
}
exit 0
