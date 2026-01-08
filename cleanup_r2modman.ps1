# Clean up r2modman profile - remove all CertiFried folders for fresh sync
$PluginsPath = "C:\Users\crawf\AppData\Roaming\r2modmanPlus-local\Techtonica\profiles\not default\BepInEx\plugins"

Write-Host "Cleaning CertiFried folders from r2modman profile..." -ForegroundColor Cyan

$folders = Get-ChildItem $PluginsPath -Directory | Where-Object { $_.Name -like "CertiFried-*" }

foreach ($folder in $folders) {
    Remove-Item $folder.FullName -Recurse -Force
    Write-Host "  Removed: $($folder.Name)" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Removed $($folders.Count) CertiFried folders" -ForegroundColor Green
