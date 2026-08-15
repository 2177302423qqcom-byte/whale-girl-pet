# fetch-assets.ps1 — 获取鲸鱼娘立绘素材(CC BY-NC-SA 4.0,非商用)
# 素材来自 dsh-deep-whale 皮肤包 + 萌娘百科共享,署名链见 NOTICE.md
$ErrorActionPreference = 'Stop'
$assets = Join-Path $PSScriptRoot '..\WhalePet\assets'
New-Item -ItemType Directory -Force -Path $assets | Out-Null

$skinBase = 'https://raw.githubusercontent.com/Small-tailqwq/dsh-deep-whale/main/maid-atelier/assets/'
$files = @{
  'maid-atelier-maid-left-v5.webp'  = 'maid-left.webp'
  'maid-atelier-maid-right-v6.webp' = 'maid-right.webp'
}

Write-Host '== 1/2 从 dsh-deep-whale 皮肤包下载立绘 ==' -ForegroundColor Cyan
foreach ($remote in $files.Keys) {
  $dest = Join-Path $assets $files[$remote]
  if (Test-Path $dest) { Write-Host "已存在: $($files[$remote])" -ForegroundColor DarkGray; continue }
  Write-Host "下载 $remote ..."
  Invoke-WebRequest -Uri ($skinBase + $remote) -OutFile $dest -UseBasicParsing
  Write-Host "  OK -> $($files[$remote])" -ForegroundColor Green
}

# 转换 webp -> png(需要 node + sharp;没有则给出提示)
$node = Get-Command node -ErrorAction SilentlyContinue
$sharp = 'C:\Users\qwaszx\.dsh\profiles\node_modules\sharp'
$needPng = (Test-Path (Join-Path $assets 'maid-right.png')) -eq $false
if ($needPng -and $node) {
  Write-Host '== 2/2 转换 PNG(立绘 + 头像)==' -ForegroundColor Cyan
  $script = @'
const sharp = require(process.argv[2]);
const fs = require('fs');
const dir = process.argv[3];
(async () => {
  for (const [src, name] of [['maid-right.webp','maid-right'],['maid-left.webp','maid-left']]) {
    const p = dir + '\\' + src;
    if (!fs.existsSync(p)) continue;
    const t = await sharp(p).trim().toBuffer({ resolveWithObject: true });
    const h = 1600;
    const w = Math.round(t.info.width * h / t.info.height);
    await sharp(t.data).resize(w, h).png({ compressionLevel: 9 }).toFile(dir + '\\' + name + '.png');
    console.log(name + ' -> ' + w + 'x' + h);
  }
  // 头像(从 maid-right 裁头部)
  const r = dir + '\\maid-right.png';
  if (fs.existsSync(r)) {
    const m = await sharp(r).metadata();
    const s = Math.round(m.height * 0.34);
    const left = Math.round((m.width - s) / 2);
    const top = Math.round(m.height * 0.07);
    await sharp(r).extract({ left, top, width: s, height: s }).resize(96, 96).png().toFile(dir + '\\avatar.png');
    console.log('avatar -> 96x96');
  }
  console.log('done');
})().catch(e => { console.error('ERR', e.message); process.exit(1); });
'@
  $js = Join-Path $env:TEMP 'whale-convert-assets.js'
  Set-Content -Path $js -Value $script -Encoding utf8
  node $js $sharp $assets
} elseif ($needPng) {
  Write-Host '! 未找到 node/sharp,无法自动转换 PNG。' -ForegroundColor Yellow
  Write-Host '  请手动将 maid-right.webp / maid-left.webp 转换为同名 PNG 放入 WhalePet/assets/'
}

Write-Host ''
Write-Host '完成!素材放好了。构建: cd WhalePet; dotnet build -c Release' -ForegroundColor Green
