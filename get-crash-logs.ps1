Get-WinEvent -FilterHashtable @{LogName='Application'; StartTime=(Get-Date).AddHours(-2)} |
    Where-Object { $_.Message -match 'LeaseGateLite|Microsoft\.UI\.Xaml|WindowsAppRuntime|WinUI|CoreCLR|\.NET Runtime' } |
    Select-Object TimeCreated, ProviderName, LevelDisplayName, Id, Message |
    Format-List
