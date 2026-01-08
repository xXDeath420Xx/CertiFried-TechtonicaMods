# Resize mod icons to 256x256 for Thunderstore
Add-Type -AssemblyName System.Drawing

$mods = @('BaseBuilding', 'MechSuit', 'SeamlessWorld', 'MultiplayerFixes')

foreach ($mod in $mods) {
    $path = "C:\Users\crawf\TechtonicaMods\NewMods\$mod\icon.png"

    if (Test-Path $path) {
        $img = [System.Drawing.Image]::FromFile($path)
        $width = $img.Width
        $height = $img.Height

        if ($width -ne 256 -or $height -ne 256) {
            $newImg = New-Object System.Drawing.Bitmap 256, 256
            $graphics = [System.Drawing.Graphics]::FromImage($newImg)
            $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
            $graphics.DrawImage($img, 0, 0, 256, 256)
            $img.Dispose()
            $graphics.Dispose()

            # Save to temp, then replace original
            $tempPath = $path + ".tmp"
            $newImg.Save($tempPath, [System.Drawing.Imaging.ImageFormat]::Png)
            $newImg.Dispose()

            Remove-Item $path -Force
            Rename-Item $tempPath $path

            Write-Host "[OK] Resized $mod icon from ${width}x${height} to 256x256" -ForegroundColor Green
        } else {
            $img.Dispose()
            Write-Host "[SKIP] $mod icon already 256x256" -ForegroundColor Yellow
        }
    } else {
        Write-Host "[MISSING] $mod icon not found" -ForegroundColor Red
    }
}
