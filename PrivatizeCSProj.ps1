# Define the path to your .csproj file
$csprojPath = "../../*.csproj"
$csProj = (Get-Item -Path $csprojPath)

# Read the content of the .csproj file
$xmlContent = [xml](Get-Content $csProj)

foreach ($itemGroup in $xmlContent.Project.ItemGroup ) {
    if ($itemGroup.Reference.Count -eq 0) {continue}
    foreach ($reference in $itemGroup.Reference) {
        if ($reference.HintPath -eq $null) {continue}
        if ($reference.Private -eq $null) {
            $newElem = $xmlContent.CreateElement("Private", $reference.NamespaceURI)
            $reference.AppendChild($newElem)
        }
        $reference.Private = "False"
    }
}
foreach ($propGroup in $xmlContent.Project.PropertyGroup) {
    if ($propGroup.DebugSymbols -ne $null)
    {
        $propGroup.DebugSymbols = "false"
    }
    if ($propGroup.DebugType -ne $null)
    {
        $propGroup.DebugType = "none"
    }
}

# Save the modified content back to the .csproj file
$xmlContent.Save($csProj.FullName)