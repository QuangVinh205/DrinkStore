param(
    [string]$Server = '.\SQLEXPRESS',
    [Security.SecureString]$ManagerPassword
)

$ErrorActionPreference = 'Stop'
$projectPath = Join-Path $PSScriptRoot '../DrinkStore/DrinkStore.csproj'

if (-not $ManagerPassword) {
    $ManagerPassword = Read-Host 'Mật khẩu quản lý demo (ít nhất 8 ký tự, có chữ hoa, chữ thường và số)' -AsSecureString
}

$previousConnection = $env:ConnectionStrings__DrinkStoreConnection
$previousDemo = $env:Demo__Enabled
$previousPassword = $env:Demo__ManagerPassword

try {
    $env:ConnectionStrings__DrinkStoreConnection = "Server=$Server;Database=DrinkStoreDemo;Trusted_Connection=True;TrustServerCertificate=True;"
    $env:Demo__Enabled = 'true'
    $env:Demo__ManagerPassword = [Net.NetworkCredential]::new('', $ManagerPassword).Password

    Write-Host 'Database: DrinkStoreDemo | Web: http://localhost:5062'
    Write-Host 'Quản lý demo: manager@drinkstore.example'
    & dotnet run --project $projectPath --launch-profile http
    if ($LASTEXITCODE -ne 0) { throw 'Không chạy được demo. Kiểm tra .NET SDK, SQL Server và mật khẩu demo.' }
}
finally {
    $env:ConnectionStrings__DrinkStoreConnection = $previousConnection
    $env:Demo__Enabled = $previousDemo
    $env:Demo__ManagerPassword = $previousPassword
}
