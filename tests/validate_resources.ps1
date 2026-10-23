# PowerShell 7 equivalent of the dependency-free resource checks, with issue 2 coverage.
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot
$passed = 0
function Check($condition, [string]$name) {
    if (-not $condition) { throw "FAIL $name" }
    $script:passed++
    Write-Output "PASS $name"
}
function ReadJson([string]$path) { [IO.File]::ReadAllText((Join-Path $root $path)) | ConvertFrom-Json -AsHashtable }
$eng = ReadJson 'MadScientist/localization/eng/cards.json'
$zhs = ReadJson 'MadScientist/localization/zhs/cards.json'
foreach ($file in Get-ChildItem (Join-Path $root 'MadScientist/localization/eng') -Filter '*.json') {
    $en = ReadJson "MadScientist/localization/eng/$($file.Name)"
    $zh = ReadJson "MadScientist/localization/zhs/$($file.Name)"
    Check (-not (Compare-Object @($en.Keys | Sort-Object) @($zh.Keys | Sort-Object))) "$($file.Name): matching language keys"
    foreach ($key in $en.Keys) { Check ($en[$key] -notmatch '[\u4e00-\u9fff]') "English text: $($file.Name):$key" }
}
$classes = @{}
foreach ($file in Get-ChildItem (Join-Path $root 'MadScientistCode/Cards') -Filter '*.cs') {
    $text = [IO.File]::ReadAllText($file.FullName)
    foreach ($match in [regex]::Matches($text, 'public class (\w+) : MadScientistCard(.*?)(?=\npublic class |\z)', 'Singleline')) {
        $name = $match.Groups[1].Value
        $body = $match.Groups[2].Value
        $key = [regex]::Replace($name, '(?<!^)(?=[A-Z])', '_').ToUpperInvariant()
        $vars = @('IfUpgraded')
        foreach ($m in [regex]::Matches($body, 'new (Damage|Block|Cards|Energy)Var\(|new PowerVar<(\w+)>|new DynamicVar\("(\w+)"')) {
            $vars += ($m.Groups | Select-Object -Skip 1 | Where-Object Success | Select-Object -First 1).Value
        }
        $classes[$key] = $vars
        Check ($eng.Contains("MADSCIENTIST-$key.title") -and $zhs.Contains("MADSCIENTIST-$key.description")) "$name has localized title and description"
        Check (Test-Path -LiteralPath (Join-Path $root "MadScientist/images/card_portraits/$($key.ToLowerInvariant()).png")) "$name has card portrait"
    }
}
Check ($classes.Count -eq 63) 'exactly 63 issue card classes'
$classes['FUSED_CARD'] = $classes['FUSED_X_CARD'] = @('Effects')
foreach ($data in @($eng,$zhs)) {
    foreach ($key in $data.Keys | Where-Object { $_ -like 'MADSCIENTIST-*.description' }) {
        $name = $key.Replace('MADSCIENTIST-','').Replace('.description','')
        foreach ($var in [regex]::Matches($data[$key], '\{(\w+)')) {
            Check ($classes[$name] -contains $var.Groups[1].Value) "${key}: dynamic variable $($var.Groups[1].Value)"
        }
    }
}
foreach ($lang in @('eng','zhs')) {
    $data = ReadJson "MadScientist/localization/$lang/cards.json"
    foreach ($key in @('X_RAY','BLASTING_DEVICE')) { Check ($data["MADSCIENTIST-$key.description"] -notmatch 'X\(X\+1\)') "$lang $key does not show X(X+1)" }
    $ancients = ReadJson "MadScientist/localization/$lang/ancients.json"
    $names = @($ancients.Keys | ForEach-Object { ($_ -split '\.')[0] } | Sort-Object -Unique)
    $expected = @('NEOW','DARV','OROBAS','PAEL','TEZCATARA','NONUPEIPE','VAKUU','TANX','THE_ARCHITECT')
    Check (-not (Compare-Object ($expected | Sort-Object) $names)) "$lang has eight ancient groups plus the Architect"
    foreach ($suffix in @('0-0r.char','0-0r.next','0-1r.ancient','0-attack')) {
        $key = "THE_ARCHITECT.talk.MADSCIENTIST-MAD_SCIENTIST.$suffix"
        Check (-not [string]::IsNullOrWhiteSpace($ancients[$key])) "$lang Architect key $suffix"
    }
    Check ($ancients['THE_ARCHITECT.talk.MADSCIENTIST-MAD_SCIENTIST.0-attack'] -eq 'Both') "$lang Architect attack is an enum value, not translated prose"
    foreach ($id in @('FUSED_CARD','FUSED_X_CARD')) {
        Check (-not [string]::IsNullOrWhiteSpace($data["$id.title"])) "$lang $id runtime title"
        Check ($data["$id.description"].Contains('{Effects}')) "$lang $id runtime description includes material effects"
    }
    $powers = ReadJson "MadScientist/localization/$lang/powers.json"
    foreach ($title in @($powers.Keys | Where-Object { $_.EndsWith('.title') })) {
        $id = $title.Replace('.title','')
        Check (-not [string]::IsNullOrWhiteSpace($powers["$id.smartDescription"])) "$lang $id has a smart description"
        foreach ($var in [regex]::Matches($powers["$id.smartDescription"], '\{(\w+)')) {
            Check ($var.Groups[1].Value -in @('OwnerName','Amount')) "$lang $id smart variable $($var.Groups[1].Value) is supplied by PowerModel"
        }
    }
}
foreach ($file in @('images/rooms/rest_site.png','images/ui/hands/multiplayer_hand_madscientist_point.png','images/ui/hands/multiplayer_hand_madscientist_rock.png','images/ui/hands/multiplayer_hand_madscientist_paper.png','images/ui/hands/multiplayer_hand_madscientist_scissors.png')) {
    $bytes = [IO.File]::ReadAllBytes((Join-Path $root "MadScientist/$file"))
    Check ($bytes.Length -gt 24 -and [Convert]::ToHexString($bytes[0..7]) -eq '89504E470D0A1A0A') "$file is a PNG (layout not tested)"
}
[xml]$project = [IO.File]::ReadAllText((Join-Path $root 'MadScientist.csproj'))
foreach ($copy in $project.SelectNodes('//Copy')) { Check ($copy.DestinationFolder.TrimEnd('/') -eq '$(ModOutputDir)') 'build output uses isolated-overridable ModOutputDir' }
Write-Output "$passed static resource assertions passed; no rendering/gameplay/multiplayer claim."
