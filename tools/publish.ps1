<#
.SYNOPSIS
    一步完成 EVEBox 发版：同步版本信息 → 跑测试 → 打包 → 校验产物。

.DESCRIPTION
    版本号的唯一来源是 App\YingYongXinXi.cs 里的 Version 常量（已有测试守护它与 version.json 一致）。
    本脚本以它为准，把其余会漂移的地方一次性同步到位：

      1. App\YingYongXinXi.cs    读取版本、日期、更新说明（唯一来源，脚本只读不写）
      2. version.json            写入 version / downloadUrl / notes
      3. README.md               在「## 更新日志」下插入新一节（若尚无该版本）
      4. README.en.md            在「## Changelog」下插入新一节（若尚无该版本）
      5. dotnet publish          单文件自包含输出到 publish\
      6. Compress-Archive        生成 publish\EVEBOX_v{版本}.zip
      7. 校验                    zip 存在、非空，并打印大小与 SHA256

    只做本地操作，不碰任何远程仓库；push 与建 Release 由人工确认后另行执行。

.PARAMETER SkipTests
    跳过单元测试（不建议；默认必跑）。

.PARAMETER SkipPublish
    只同步版本信息、不打包（用于先看一眼改动）。

.EXAMPLE
    .\publish.ps1                 # 完整发版流程
    .\publish.ps1 -SkipPublish    # 只同步版本信息

.NOTES
    本机执行策略禁止直接运行 .ps1 时，用：
    powershell -NoProfile -ExecutionPolicy Bypass -File tools\publish.ps1
#>
[CmdletBinding()]
param(
    [switch]$SkipTests,
    [switch]$SkipPublish
)

$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

$repoRoot = Split-Path -Parent $PSScriptRoot
$infoFile = Join-Path $repoRoot 'App\YingYongXinXi.cs'
$versionJsonPath = Join-Path $repoRoot 'version.json'
$readmeZhPath = Join-Path $repoRoot 'README.md'
$readmeEnPath = Join-Path $repoRoot 'README.en.md'
$publishDir = Join-Path $repoRoot 'publish'

function Write-Step($text) { Write-Host "== $text ==" -ForegroundColor Cyan }
function Write-Problem($text) { Write-Host $text -ForegroundColor Red }

# ------------------------------------------------------------
# 1. 读取唯一来源
# ------------------------------------------------------------
Write-Step '读取版本信息（来源：App\YingYongXinXi.cs）'

if (-not (Test-Path $infoFile)) { Write-Problem "找不到 $infoFile"; exit 1 }
$infoText = [System.IO.File]::ReadAllText($infoFile, [System.Text.Encoding]::UTF8)

$versionMatch = [regex]::Match($infoText, 'public\s+const\s+string\s+Version\s*=\s*"([^"]+)"')
if (-not $versionMatch.Success) { Write-Problem '没能从 YingYongXinXi.cs 解析出 Version 常量'; exit 1 }
$version = $versionMatch.Groups[1].Value

$dateMatch = [regex]::Match($infoText, 'public\s+const\s+string\s+ReleaseDate\s*=\s*"([^"]+)"')
$releaseDate = if ($dateMatch.Success) { $dateMatch.Groups[1].Value } else { '' }

Write-Host "  版本: $version"
Write-Host "  日期: $releaseDate"

if ($version -notmatch '^v\d+\.\d+') {
    Write-Problem "版本号格式异常：$version（应形如 v6.16）"
    exit 1
}

# 更新说明：ReleaseNotes 由多段字符串拼接而成，逐段取出引号内容再合并
$notesMatch = [regex]::Match($infoText, 'ReleaseNotes\s*=\s*((?:\s*"[^"]*"\s*\+?)+);')
if (-not $notesMatch.Success) { Write-Problem '没能从 YingYongXinXi.cs 解析出 ReleaseNotes'; exit 1 }
$noteParts = [regex]::Matches($notesMatch.Groups[1].Value, '"((?:[^"\\]|\\.)*)"')
$notes = ($noteParts | ForEach-Object { $_.Groups[1].Value }) -join ''
$notes = $notes -replace '\\n', "`n"
Write-Host "  说明行数: $(($notes -split "`n").Count)"

# 代码里的 ReleaseNotes 带 "   - " 项目符号，写进 version.json 时要还原成
# 原来的单行「；」分隔形式（客户端弹窗直接展示这段文字）。
$notesJson = ($notes -split "`n" |
    ForEach-Object { ($_ -replace '^\s*-\s*', '').Trim() } |
    Where-Object { $_ }) -join '；'

# ------------------------------------------------------------
# 2. 同步 version.json
# ------------------------------------------------------------
Write-Step '同步 version.json'

$downloadUrl = "https://gitee.com/minisangel/EVEBox/releases/download/$version/EVEBOX_$version.zip"
$jsonObject = [ordered]@{
    version     = $version
    downloadUrl = $downloadUrl
    url         = 'https://github.com/johngi666/EVEBox/releases/latest'
    notes       = $notesJson
}
$jsonText = ($jsonObject | ConvertTo-Json -Depth 5) -replace "`r`n", "`n"
[System.IO.File]::WriteAllText($versionJsonPath, $jsonText + "`n", (New-Object System.Text.UTF8Encoding($false)))
Write-Host "  已写入 $version"

# ------------------------------------------------------------
# 3. 同步两个 README 的更新日志
# ------------------------------------------------------------
function Add-ChangelogSection {
    param(
        [string]$FilePath,
        [string]$SectionTitle,   # 更新日志小节标题（如「## 更新日志」）
        [string]$VersionTitle,   # 新小节标题（如「### v6.16（2026 年 10 月 2 日）」）
        [string]$Body
    )

    if (-not (Test-Path $FilePath)) { Write-Problem "找不到 $FilePath"; return }

    $content = [System.IO.File]::ReadAllText($FilePath, [System.Text.Encoding]::UTF8)

    # 已经有这个版本的小节就不再插入（用版本号本身判断，避免受日期写法影响）
    if ($content.Contains("### $version")) {
        Write-Host "  $(Split-Path -Leaf $FilePath) 已有 $version 小节，跳过"
        return
    }

    $titleIndex = $content.IndexOf($SectionTitle, [StringComparison]::Ordinal)
    if ($titleIndex -lt 0) { Write-Problem "$FilePath 里找不到「$SectionTitle」，请手工补充更新日志"; return }

    $insertAt = $titleIndex + $SectionTitle.Length
    $result = $content.Insert($insertAt, "`n`n$VersionTitle`n`n$Body")

    # 保留原文件是否带 BOM，不擅自改变编码形态
    $hasBom = ([System.IO.File]::ReadAllBytes($FilePath)[0..2] -join ',') -eq '239,187,191'
    [System.IO.File]::WriteAllText($FilePath, $result, (New-Object System.Text.UTF8Encoding($hasBom)))
    Write-Host "  $(Split-Path -Leaf $FilePath) 已插入 $VersionTitle"
}

Write-Step '同步 README 更新日志'

# 中文日期：2026年10月2日；英文日期：October 2, 2026
$dateZh = if ($releaseDate) { $releaseDate } else { (Get-Date).ToString('yyyy年M月d日') }
$dateEn = if ($releaseDate -match '(\d{4})年(\d{1,2})月(\d{1,2})日') {
    (Get-Date -Year $Matches[1] -Month $Matches[2] -Day $Matches[3]).ToString('MMMM d, yyyy', [System.Globalization.CultureInfo]::InvariantCulture)
} else { (Get-Date).ToString('MMMM d, yyyy', [System.Globalization.CultureInfo]::InvariantCulture) }

$bodyZh = ($notes -split "`n" | ForEach-Object { $_.Trim() } | Where-Object { $_ } |
    ForEach-Object { "- " + ($_ -replace '^-\s*', '') }) -join "`n"
$bodyEn = "See README.md changelog for $version (Chinese only)."

Add-ChangelogSection -FilePath $readmeZhPath -SectionTitle '## 更新日志' `
    -VersionTitle "### $version（$dateZh）" -Body $bodyZh
Add-ChangelogSection -FilePath $readmeEnPath -SectionTitle '## Changelog' `
    -VersionTitle "### $version ($dateEn)" -Body $bodyEn

if ($SkipPublish) {
    Write-Host ''
    Write-Host '已跳过打包（-SkipPublish）。' -ForegroundColor Yellow
    exit 0
}

# ------------------------------------------------------------
# 4. 跑测试
# ------------------------------------------------------------
if (-not $SkipTests) {
    Write-Step '跑全量单元测试'
    $testScript = Join-Path $PSScriptRoot '..\tests\run-tests.ps1'
    & powershell -NoProfile -ExecutionPolicy Bypass -File $testScript -NoBuild
    if ($LASTEXITCODE -ne 0) { Write-Problem '测试未通过，终止发版'; exit 1 }
} else {
    Write-Host '已跳过测试（-SkipTests）' -ForegroundColor Yellow
}

# ------------------------------------------------------------
# 5. 打包
# ------------------------------------------------------------
Write-Step '发布打包（单文件自包含）'
if (-not (Test-Path $publishDir)) { New-Item -ItemType Directory -Path $publishDir -Force | Out-Null }

dotnet publish (Join-Path $repoRoot 'EVEBox.csproj') -c Release -r win-x64 -o $publishDir --nologo
if ($LASTEXITCODE -ne 0) { Write-Problem '发布失败'; exit 1 }

$exePath = Join-Path $publishDir 'EVE BOX.exe'
if (-not (Test-Path $exePath)) { Write-Problem "没有产出 $exePath"; exit 1 }

$zipPath = Join-Path $publishDir "EVEBOX_$version.zip"
if (Test-Path $zipPath) {
    # 旧包送回收站，不做永久删除（项目规则第 1 条）
    Add-Type -AssemblyName Microsoft.VisualBasic
    [Microsoft.VisualBasic.FileIO.FileSystem]::DeleteFile($zipPath, 'OnlyErrorDialogs', 'SendToRecycleBin')
    Write-Host '  已把同名旧压缩包送进回收站'
}

Compress-Archive -Path $exePath -DestinationPath $zipPath -CompressionLevel Optimal

# ------------------------------------------------------------
# 6. 校验
# ------------------------------------------------------------
Write-Step '校验产物'
$zipInfo = Get-Item $zipPath
$sha = (Get-FileHash $zipPath -Algorithm SHA256).Hash.ToLower()

Write-Host ("  exe : {0:N1} MB" -f ((Get-Item $exePath).Length / 1MB))
Write-Host ("  zip : {0:N1} MB ({1:N0} 字节)" -f ($zipInfo.Length / 1MB), $zipInfo.Length)
Write-Host "  SHA256: $sha"
Write-Host ''
Write-Host '发版产物已就绪。' -ForegroundColor Green
Write-Host '后续（需人工确认后才可执行云端写操作）:' -ForegroundColor Yellow
Write-Host '  git push gitee main ; git push origin main'
Write-Host "  在 Gitee / GitHub 创建 $version Release 并上传 $([System.IO.Path]::GetFileName($zipPath))"
