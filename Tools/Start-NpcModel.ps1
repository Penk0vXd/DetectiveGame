param(
    [string]$Runner = 'C:/AI/llama.cpp/llama-server.exe',
    [string]$Model = 'C:/AI/models/Qwen_Qwen3.5-9B-Q6_K.gguf'
)
$ErrorActionPreference = 'Stop'
if (!(Test-Path -LiteralPath $Runner) -or !(Test-Path -LiteralPath $Model)) { throw 'Липсва runner или модел.' }
if (Get-NetTCPConnection -State Listen -LocalPort 8080 -ErrorAction SilentlyContinue) {
    throw 'Порт 8080 вече се използва. Провери /v1/models; не се стартира втори runner.'
}
$npcRoot = Split-Path $PSScriptRoot -Parent
New-Item -ItemType Directory -Path (Join-Path $npcRoot 'Logs') -Force | Out-Null
$npcProcess = Start-Process -FilePath $Runner -ArgumentList @('-m', ('"' + $Model + '"'),
    '--host', '127.0.0.1', '--port', '8080', '--alias', 'detective-npc-prototype',
    '--ctx-size', '8192', '--parallel', '1', '--reasoning', 'off') -WindowStyle Hidden -PassThru `
    -RedirectStandardOutput (Join-Path $npcRoot 'Logs/NpcModel-out.log') `
    -RedirectStandardError (Join-Path $npcRoot 'Logs/NpcModel-error.log')
Write-Output "Стартиран процес $($npcProcess.Id). Изчакай /health да върне status ok. Спиране: Stop-Process -Id $($npcProcess.Id)"
