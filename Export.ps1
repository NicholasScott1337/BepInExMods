$workingDir = Get-Location
$toDir = Get-Item -Path "C:\Users\daymo\AppData\Roaming\Thunderstore Mod Manager\DataFolder\LethalCompany\profiles\Default\BepInEx\plugins\"

$addons = Get-ChildItem -Filter "*NicholaScott.*" | Where-Object -FilterScript {Test-Path -Path (Join-Path -Path $_ -ChildPath "bin/Debug/")}

foreach ($addon in $addons) {
    $addonDest = Join-Path -Path $toDir.FullName -ChildPath $addon.Name

    if (-not (Test-Path -Path $addonDest)) {
        New-Item -Path $addonDest -ItemType "directory"
    }
    $pathToContent = Join-Path -Path $workingDir -ChildPath $addon.Name | Join-Path -ChildPath "bin/Debug/"
    
    Get-ChildItem -Path $pathToContent | Copy-Item -Destination $addonDest -Force
    Write ("[{2}] Copied {0} items for addon {1}" -f (Get-ChildItem -Path $pathToContent).Length, $addon.Name, (Get-Date).TimeOfDay.ToString())
}