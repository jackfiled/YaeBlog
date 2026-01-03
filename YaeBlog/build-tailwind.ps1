[cmdletbinding()]
param(
    [string]$output = "wwwroot"
)

Write-Output "Output directory: $output"
pnpm tailwindcss -i wwwroot/tailwind.css -o $output/tailwind.g.css
