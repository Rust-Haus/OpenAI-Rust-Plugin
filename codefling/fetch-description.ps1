# Fetch current description from Codefling and save to codefling/description.html
# Requires: $env:CODEFLING_FILE_ID and $env:CODEFLING_CREATOR_API_KEY (or load from .env.local)

$ErrorActionPreference = "Stop"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

# Load .env.local if present
$envPath = Join-Path (Split-Path -Parent $scriptDir) ".env.local"
if (Test-Path $envPath) {
    Get-Content $envPath | ForEach-Object {
        if ($_ -match '^\s*([^#][^=]+)=(.*)$') {
            $key = $matches[1].Trim()
            $value = $matches[2].Trim().Trim('"').Trim("'")
            [Environment]::SetEnvironmentVariable($key, $value, "Process")
        }
    }
}

$fileId = $env:CODEFLING_FILE_ID
$apiKey = $env:CODEFLING_CREATOR_API_KEY
if (-not $fileId -or -not $apiKey) {
    Write-Error "Set CODEFLING_FILE_ID and CODEFLING_CREATOR_API_KEY (e.g. in .env.local)"
    exit 1
}

$uri = "https://codefling.com/api/downloads/files/$fileId"
$headers = @{
    "Authorization" = "Bearer $apiKey"
}
$response = Invoke-RestMethod -Uri $uri -Headers $headers -Method Get
$description = $response.description
if (-not $description) {
    Write-Error "No description in API response"
    exit 1
}

$outPath = Join-Path $scriptDir "description.html"
Set-Content -Path $outPath -Value $description -NoNewline -Encoding UTF8
Write-Host "Updated $outPath with current Codefling description."
