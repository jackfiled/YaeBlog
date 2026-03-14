#!/pwsh

[cmdletbinding()]
param(
    [string]$Output = "wwwroot"
)

end {
    Write-Host "Build tailwind css into $Output."
    pnpm tailwindcss -i wwwroot/tailwind.css -o $Output/tailwind.g.css
}
