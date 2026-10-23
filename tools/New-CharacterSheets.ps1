# Original geometric character sheets. Colors match JourneyCatalog.cs.
# These are design references, not the in-game mesh.
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$out = Join-Path $root 'Docs\characters'
New-Item -ItemType Directory -Force -Path $out | Out-Null

$people = @(
    @{ Id='tavi'; Name='Tavi Flintstep'; Role='Explorer'; Unlock='Free'; Accessory='Cream scarf and map pouch'; Helmet='#1F8A8A'; Cloth='#2E6B8A'; Accent='#E07020'; Skin='#8D5524'; Hair='#1A1A1A' },
    @{ Id='mira'; Name='Mira Mosswheel'; Role='Cart engineer'; Unlock='Free'; Accessory='Tool pouch and knee pads'; Helmet='#2F6B45'; Cloth='#D4652F'; Accent='#E8C56B'; Skin='#E0AC69'; Hair='#6B3A2A' },
    @{ Id='oren'; Name='Oren Pebblewick'; Role='Geology guide'; Unlock='Free'; Accessory='Sample tubes'; Helmet='#F0E2A0'; Cloth='#7A4A78'; Accent='#D8C07A'; Skin='#F1C27D'; Hair='#4A3728' },
    @{ Id='sola'; Name='Sola Shelfstone'; Role='Shopkeeper'; Unlock='Free'; Accessory='Gem-shaped badge'; Helmet='#F3C2A8'; Cloth='#8AA878'; Accent='#E7B24A'; Skin='#C68642'; Hair='#2B2118' },
    @{ Id='zela'; Name='Zela Copperfern'; Role='Excavation engineer'; Unlock='Own 6 bunkers'; Accessory='Copper gloves'; Helmet='#4A5568'; Cloth='#2A4E9A'; Accent='#C4784A'; Skin='#5C3317'; Hair='#111111' },
    @{ Id='pavi'; Name='Pavi Dewstone'; Role='Washing specialist'; Unlock='Wash one batch'; Accessory='Round splash goggles'; Helmet='#F4E1A8'; Cloth='#2BB8B0'; Accent='#E07A6A'; Skin='#FFDBAC'; Hair='#C45C26' },
    @{ Id='nilo'; Name='Nilo Quartzpatch'; Role='Sorting specialist'; Unlock='Recover 5 diamonds'; Accessory='Hex sorting tray'; Helmet='#A8E0C8'; Cloth='#3D3A8C'; Accent='#F2D48A'; Skin='#A56B45'; Hair='#3A3A3A' },
    @{ Id='etta'; Name='Etta Facetbrook'; Role='Gem cutter'; Unlock='Cut a diamond'; Accessory='Violet glasses'; Helmet='#E7D7C8'; Cloth='#F0A06A'; Accent='#6A4A9A'; Skin='#6B3E26'; Hair='#E6D2B0' },
    @{ Id='rumi'; Name='Rumi Glowgrain'; Role='Polishing specialist'; Unlock='Polish a diamond'; Accessory='Polishing cloth'; Helmet='#F7F1E8'; Cloth='#C8B4E0'; Accent='#D6E8F5'; Skin='#E8B89A'; Hair='#5A463A' },
    @{ Id='kavi'; Name='Kavi Loopforge'; Role='Jewellery maker'; Unlock='Craft a ring'; Accessory='Loop tool holder'; Helmet='#E6D2B5'; Cloth='#8C3048'; Accent='#D4A017'; Skin='#3C2415'; Hair='#1C1C1C' },
    @{ Id='beni'; Name='Beni Trailcart'; Role='Delivery worker'; Unlock='Finish 3 orders'; Accessory='Small delivery cart'; Helmet='#E0A040'; Cloth='#7EB6E0'; Accent='#F4E7C8'; Skin='#D4A574'; Hair='#6A4226' },
    @{ Id='daro'; Name='Daro Rootlamp'; Role='Workshop planner'; Unlock='Find another region'; Accessory='Blueprint tube'; Helmet='#D6A423'; Cloth='#6B7040'; Accent='#2AAAA0'; Skin='#7A4B32'; Hair='#B7B7B7' }
)

function Figure([string]$helm, [string]$cloth, [string]$skin, [string]$hair, [string]$accent, [double]$x, [string]$facing) {
    $body = if ($facing -eq 'side') { "M $($x+18) 78 h 28 v 46 h -28 z" } else { "M $($x+8) 78 h 44 v 48 h -44 z" }
    $head = if ($facing -eq 'side') { "M $($x+22) 40 h 24 v 28 h -24 z" } else { "M $($x+16) 38 h 28 v 30 h -28 z" }
    @"
  <path d="$body" fill="$cloth" stroke="#2B241C" stroke-width="3" stroke-linejoin="round"/>
  <path d="$head" fill="$skin" stroke="#2B241C" stroke-width="3" stroke-linejoin="round"/>
  <path d="M $($x+12) 34 h 36 v 14 h -36 z" fill="$helm" stroke="#2B241C" stroke-width="3"/>
  <circle cx="$($x+30)" cy="128" r="8" fill="$accent"/>
  <path d="M $($x+18) 46 h 10 v 8 h -10 z" fill="$hair"/>
"@
}

$index = New-Object System.Collections.Generic.List[string]
$index.Add('# Character sheets')
$index.Add('')
$index.Add('Original geometric references for the 12 provisional crew designs. The playable figure is the shared procedural puppet in `YardWorld`. These sheets are not a finished skinned model and they are not copied from another game.')
$index.Add('')
$index.Add('| Sheet | Name | Unlock |')
$index.Add('| --- | --- | --- |')

foreach ($person in $people) {
    $svg = @"
<svg xmlns="http://www.w3.org/2000/svg" width="900" height="520" viewBox="0 0 900 520">
  <rect width="900" height="520" fill="#F6F1E6"/>
  <text x="32" y="42" font-family="Segoe UI, sans-serif" font-size="28" fill="#2B241C">$($person.Name)</text>
  <text x="32" y="70" font-family="Segoe UI, sans-serif" font-size="18" fill="#5C5148">$($person.Role) · $($person.Unlock)</text>
  <text x="70" y="110" font-family="Segoe UI, sans-serif" font-size="16" fill="#5C5148">Front</text>
  <text x="250" y="110" font-family="Segoe UI, sans-serif" font-size="16" fill="#5C5148">Side</text>
  <text x="430" y="110" font-family="Segoe UI, sans-serif" font-size="16" fill="#5C5148">Back</text>
  <text x="620" y="110" font-family="Segoe UI, sans-serif" font-size="16" fill="#5C5148">Silhouette</text>
$(Figure $person.Helmet $person.Cloth $person.Skin $person.Hair $person.Accent 40 'front')
$(Figure $person.Helmet $person.Cloth $person.Skin $person.Hair $person.Accent 220 'side')
$(Figure $person.Helmet $person.Cloth $person.Skin $person.Hair $person.Accent 400 'front')
  <path d="M 640 38 h 28 v 30 h -28 z M 632 78 h 44 v 48 h -44 z" fill="#2B241C"/>
  <text x="32" y="200" font-family="Segoe UI, sans-serif" font-size="16" fill="#2B241C">Accessory: $($person.Accessory)</text>
  <rect x="32" y="230" width="48" height="48" fill="$($person.Helmet)" stroke="#2B241C"/>
  <rect x="88" y="230" width="48" height="48" fill="$($person.Cloth)" stroke="#2B241C"/>
  <rect x="144" y="230" width="48" height="48" fill="$($person.Accent)" stroke="#2B241C"/>
  <rect x="200" y="230" width="48" height="48" fill="$($person.Skin)" stroke="#2B241C"/>
  <rect x="256" y="230" width="48" height="48" fill="$($person.Hair)" stroke="#2B241C"/>
  <text x="32" y="310" font-family="Segoe UI, sans-serif" font-size="16" fill="#5C5148">Helmet, clothes, accent, skin, hair. Same adult scale for every crew member.</text>
  <line x1="32" y1="360" x2="32" y2="470" stroke="#2B241C" stroke-width="4"/>
  <line x1="24" y1="360" x2="40" y2="360" stroke="#2B241C" stroke-width="4"/>
  <line x1="24" y1="470" x2="40" y2="470" stroke="#2B241C" stroke-width="4"/>
  <text x="48" y="420" font-family="Segoe UI, sans-serif" font-size="16" fill="#2B241C">About 1.8 m in the yard</text>
  <text x="32" y="500" font-family="Segoe UI, sans-serif" font-size="14" fill="#5C5148">Original project geometry. Provisional name. Not a claim of trademark clearance.</text>
</svg>
"@
    $path = Join-Path $out ($person.Id + '.svg')
    Set-Content -LiteralPath $path -Value $svg -Encoding utf8
    $index.Add('| [' + $person.Id + '.svg](' + $person.Id + '.svg) | ' + $person.Name + ' | ' + $person.Unlock + ' |')
}

Set-Content -LiteralPath (Join-Path $out 'README.md') -Value ($index -join "`r`n") -Encoding utf8
Write-Output "Wrote $($people.Count) sheets"
