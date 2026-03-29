#!pwsh

[cmdletbinding()]
param(
    [Parameter(Mandatory = $true, Position = 0, HelpMessage = "Specify the build target")]
    [ValidateSet("publish", "compress", "build", "dev", "new", "watch", "serve")]
    [string]$Target,
    [string]$Essay,
    [switch]$Compress,
    [string]$Root = "source"
)

begin {
    if (($Target -eq "tailwind") -or ($Target -eq "build"))
    {
        # Handle tailwind specially.
        return
    }

    # Set the content root.
    $fullRootPath = Join-Path $(Get-Location) $Root
    if (-not (Test-Path $fullRootPath))
    {
        Write-Error "Content root $fullRootPath not existed."
        exit 1
    }

    Write-Host "Use content from" $fullRootPath
    $env:BLOG__ROOT=$fullRootPath

    Write-Host "Building $Target..."

    if ($Target -eq "publish")
    {
        if ($Essay -eq "")
        {
            Write-Error "No publish target, please add with --essay argument."
            exit 1
        }
    }

    if ($Target -eq "new")
    {
        if ($Essay -eq "")
        {
            Write-Error "No  new name, please add with --essay argument."
            exit 1
        }
    }

    # Set to the current location.
    Push-Location src/YaeBlog
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
        dotnet publish ./src/YaeBlog/YaeBlog.csproj -o out
        Write-Host "Succeed to build blog appliocation."
        podman build . -t ccr.ccs.tencentyun.com/jackfiled/blog --build-arg COMMIT_ID=$commitId `
            -f ./src/YaeBlog/Dockerfile
        Write-Host "Succeed to build ccr.ccs.tencentyun.com/jackfiled/blog image."
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
        "new" {
            dotnet run -- new $Essay
        }
        "watch" {
            dotnet run -- watch
            break
        }
        "serve" {
            dotnet run -- serve
            break
        }
    }
}

end {
    Pop-Location
}
