$csprojPath = ".\PriconneTLFixup.csproj"
$pluginClassPath = ".\Plugin.cs"
$pluginClassRegex = '\[BepInPlugin\((?:\s*"(?<id>.+?)")\s*,\s*"(?<name>.+?)"\s*,\s*"(?<version>.+?)"\)\]'

# Get the version from the csproj file
$xml = [xml](Get-Content $csprojPath)
$version = $xml.Project.PropertyGroup.Version

# Update the plugin class with the new version
$pluginClassContent = Get-Content $pluginClassPath -Raw
$updatedPluginClassContent = [regex]::Replace($pluginClassContent, $pluginClassRegex, {
    param($match)
    $id = $match.Groups['id'].Value
    $name = $match.Groups['name'].Value
    "[BepInPlugin(`"$id`", `"$name`", `"$version`")]"  # Note the use of backticks to escape double quotes
}).TrimEnd()
# Save the updated content back to the plugin class file
$updatedPluginClassContent | Set-Content $pluginClassPath