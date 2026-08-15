# build.ps1 — 构建鲸鱼娘桌宠
$ErrorActionPreference = 'Stop'
$proj = Join-Path $PSScriptRoot '..\WhalePet\WhalePet.csproj'
dotnet build $proj -c Release
if ($LASTEXITCODE -eq 0) {
  $out = Join-Path $PSScriptRoot '..\WhalePet\bin\Release\net9.0-windows\WhalePet.exe'
  Write-Host ""
  Write-Host "构建成功: $out" -ForegroundColor Green
  Write-Host "运行前请先执行 scripts/fetch-assets.ps1 获取立绘素材(若 assets 为空)"
}
