#!pwsh

[cmdletbinding()]
param(
    [Parameter(Mandatory = $true, Position = 0, HelpMessage = "Specify the build target")]
    [ValidateSet("tailwind", "watch", "publish", "compress", "build", "dev")]
    [string]$Target,
    [string]$Output = "wwwroot",
    [string]$Essay,
    [switch]$Compress
)

begin {
    Write-Host "Building $Target..."

    if ($Target -eq "publish")
    {
        if ($Essay -eq "")
        {
            Write-Error "No publish target, please add with --essay argument."
            exit 1
        }
    }
}

process {
    function Compress-Image
    {
        Write-Host "Compress image assets..."
        dotnet run -- compress --dry-run
        $confirm = Read-Host "Really compress images? (y/n)"
        if ($confirm -notmatch "^[yY]$")
        {
            Write-Host "Not compress images."
            return
        }

        Write-Host "Do compress image..."
        dotnet run -- compress

        dotnet run -- scan
        $confirm = Read-Host "Really delete unused images? (y/n)"
        if ($confirm -notmatch "^[yY]$")
        {
            Write-Host "Not delete images."
            return
        }
        Write-Host "Do delete unused images.."
        dotnet run -- scan --rm
    }

    function Build-Image
    {
        $commitId = git rev-parse --short=10 HEAD
        dotnet publish
        podman build . -t ccr.ccs.tencentyun.com/jackfiled/blog --build-arg COMMIT_ID=$commitId
    }

    function Start-Develop {
        Write-Host "Start tailwindcss and dotnet watch servers..."
        $pnpmProcess = Start-Process pnpm "tailwindcss -i wwwroot/tailwind.css -o obj/Debug/net10.0/ClientAssets/tailwind.g.css -w" `
            -PassThru

        try
        {
            Write-Host "Started pnpm process exit? " $pnpmProcess.HasExited
            Start-Process dotnet "watch -- serve" -PassThru | Wait-Process
        }
        finally
        {
            if ($pnpmProcess.HasExited)
            {
                 Write-Error "pnpm process has exited!"
                 exit 1
            }
            Write-Host "Kill tailwindcss and dotnet watch servers..."
            $pnpmProcess | Stop-Process
        }
    }

    switch ($Target)
    {
        "tailwind" {
            Write-Host "Build tailwind css into $Output."
            pnpm tailwindcss -i wwwroot/tailwind.css -o $Output/tailwind.g.css
            break
        }
        "watch" {
            dotnet run -- watch
            break
        }
        "publish" {
            Write-Host "Publish essay $Essay..."
            dotnet run -- publish $Essay

            if ($Compress)
            {
                Compress-Image
            }
            break
        }
        "compress" {
            Compress-Image
            break
        }
        "build" {
            Build-Image
            break
        }
        "dev" {
            Start-Develop
            break
        }
    }
}

