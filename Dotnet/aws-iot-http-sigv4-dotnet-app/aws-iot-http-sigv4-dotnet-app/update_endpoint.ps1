Import-Module AWSPowerShell

$ScriptPath = $MyInvocation.MyCommand.Path
$ScriptDirectory = Split-Path $ScriptPath

$EndpointName = Get-IOTEndpoint

Write-Output "Replacing placeholder with endpoint $EndpointName.."
Get-ChildItem -Path $ScriptDirectory -Filter *.cs -Recurse -File | ForEach-Object {
    if( (Select-String -Path $_.FullName -Pattern "<<your-iot-endpoint>>") -ne $null) {
        Write-Output "Replacing placeholder in $($_.FullName)"
        (Get-Content $_.FullName).Replace("<<your-iot-endpoint>>", $EndpointName) | Set-Content $_.FullName
    }
}
