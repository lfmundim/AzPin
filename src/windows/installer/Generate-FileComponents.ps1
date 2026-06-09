param(
    [Parameter(Mandatory)][string]$PublishDir,
    [Parameter(Mandatory)][string]$OutputPath
)

$files = Get-ChildItem -Path $PublishDir -Recurse -File
$idx = 0
$components = $files | ForEach-Object {
    $id = 'Cmp{0:D5}' -f ($idx++)
    $fid = 'Fil{0:D5}' -f ($idx - 1)
    "      <Component Id=""$id"" Directory=""INSTALLFOLDER""><File Id=""$fid"" Source=""$($_.FullName)"" /></Component>"
}

$content = @"
<?xml version="1.0" encoding="utf-8"?>
<Wix xmlns="http://wixtoolset.org/schemas/v4/wxs">
  <Fragment>
    <ComponentGroup Id="AzPinFiles">
$($components -join "`n")
    </ComponentGroup>
  </Fragment>
</Wix>
"@

Set-Content -Path $OutputPath -Value $content -Encoding utf8
