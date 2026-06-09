param(
    [Parameter(Mandatory)][string]$PublishDir,
    [Parameter(Mandatory)][string]$OutputPath
)

$PublishDir = $PublishDir.TrimEnd('\', '/')
$files = Get-ChildItem -Path $PublishDir -Recurse -File

# Map relative dir path -> XML ID. '' = INSTALLFOLDER (root).
$dirMap = @{ '' = 'INSTALLFOLDER' }

foreach ($file in $files) {
    $rel     = $file.FullName.Substring($PublishDir.Length + 1)
    $relDir  = [System.IO.Path]::GetDirectoryName($rel) -replace '/', '\'
    if (-not $relDir) { continue }

    $parts = $relDir -split '\\'
    for ($i = 1; $i -le $parts.Count; $i++) {
        $partial = ($parts[0..($i - 1)]) -join '\'
        if (-not $dirMap.ContainsKey($partial)) {
            $dirMap[$partial] = 'Dir_' + ($partial -replace '[^a-zA-Z0-9]', '_')
        }
    }
}

function Get-DirXml($parentKey, $depth) {
    $indent = '    ' + ('  ' * $depth)
    $result = ''
    foreach ($key in $dirMap.Keys) {
        if (-not $key) { continue }
        $parent = [System.IO.Path]::GetDirectoryName($key) -replace '/', '\'
        if ($parent -ne $parentKey) { continue }
        $id   = $dirMap[$key]
        $name = [System.IO.Path]::GetFileName($key)
        $result += "$indent<Directory Id=""$id"" Name=""$name"">`n"
        $result += Get-DirXml $key ($depth + 1)
        $result += "$indent</Directory>`n"
    }
    return $result
}

$idx = 0
$components = foreach ($file in $files) {
    $rel    = $file.FullName.Substring($PublishDir.Length + 1)
    $relDir = [System.IO.Path]::GetDirectoryName($rel) -replace '/', '\'
    if (-not $relDir) { $relDir = '' }
    $dirId  = $dirMap[$relDir]
    $cid    = 'Cmp{0:D5}' -f $idx
    $fid    = 'Fil{0:D5}' -f $idx
    $idx++
    "      <Component Id=""$cid"" Directory=""$dirId""><File Id=""$fid"" Source=""$($file.FullName)"" /></Component>"
}

$dirXml = Get-DirXml '' 0

$content = @"
<?xml version="1.0" encoding="utf-8"?>
<Wix xmlns="http://wixtoolset.org/schemas/v4/wxs">
  <Fragment>
    <DirectoryRef Id="INSTALLFOLDER">
$($dirXml.TrimEnd())`n    </DirectoryRef>
  </Fragment>
  <Fragment>
    <ComponentGroup Id="AzPinFiles">
$($components -join "`n")
    </ComponentGroup>
  </Fragment>
</Wix>
"@

Set-Content -Path $OutputPath -Value $content -Encoding utf8
