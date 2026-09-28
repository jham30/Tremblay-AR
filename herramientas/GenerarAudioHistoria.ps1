<#
    Genera la narración de la historia con ElevenLabs, un .mp3 por fragmento.

    Lee los textos de las tablas de localización del proyecto, limpia lo que no debe
    pronunciarse (etiquetas <color> y marcadores {n} del typewriter), reparte cada
    intervención entre la voz de la niña y la de la bruja según las marcas [Niña] / [Bruja],
    y une los trozos en un solo archivo por fragmento.

    Para que el acento no derive entre archivos, cada petición envía el texto anterior y el
    siguiente como contexto (previous_text / next_text): el modelo sabe de dónde viene y
    adónde va, aunque se sintetice por separado.

    ANTES DE USAR: pon tu clave en una variable de entorno (no se guarda en el repo).
        $env:ELEVENLABS_API_KEY = "tu-clave"

    EJEMPLOS
        # Prueba con dos fragmentos antes de gastar cuota:
        .\GenerarAudioHistoria.ps1 -Idioma es -Solo story.00-intro,story.02

        # Todo el idioma (salta los que ya existen):
        .\GenerarAudioHistoria.ps1 -Idioma es

        # Rehacer uno concreto:
        .\GenerarAudioHistoria.ps1 -Idioma es -Solo story.03 -Forzar
#>

[CmdletBinding()]
param(
    [ValidateSet('es','en','fr')]
    [string]$Idioma = 'es',

    # Claves concretas a generar. Vacío = todas.
    [string[]]$Solo = @(),

    # Regenera aunque el archivo ya exista.
    [switch]$Forzar,

    # Solo muestra qué haría, sin llamar a la API ni gastar cuota.
    [switch]$Simular
)

$ErrorActionPreference = 'Stop'

# ===================== Configuración =====================

$VozNina  = 'hO2yZ8lxM3axUxL8OeKX'
$VozBruja = 'M9RTtrzRACmbUzsEMq8p'

$Modelo = 'eleven_multilingual_v2'   # el más estable para español; no cambiarlo a mitad

$Ajustes = @{
    stability         = 0.55   # más alto = más consistente, menos expresivo
    similarity_boost  = 0.85
    style             = 0.0
    use_speaker_boost = $true
}

$Formato = 'mp3_44100_128'
$PausaEntrePeticiones = 0.5   # segundos, para no saturar la API

$RaizProyecto = Split-Path -Parent $PSScriptRoot
$Tablas  = Join-Path $RaizProyecto 'Assets\Localizacion\Tablas'
$Destino = Join-Path $RaizProyecto "Assets\misiones\Halloween\Story\sounds\$Idioma"

# ===================== Lectura de las tablas =====================

function Get-ClavesPorId {
    param([string]$Ruta)
    $mapa = @{}
    $id = $null
    foreach ($linea in Get-Content -LiteralPath $Ruta -Encoding UTF8) {
        if ($linea -match '- m_Id:\s*(\d+)')     { $id = $Matches[1] }
        elseif ($linea -match 'm_Key:\s*(.+?)\s*$' -and $id) { $mapa[$id] = $Matches[1] }
    }
    return $mapa
}

function Get-TextosPorId {
    param([string]$Ruta)
    $mapa = @{}
    $todo = Get-Content -LiteralPath $Ruta -Encoding UTF8 -Raw
    foreach ($bloque in ($todo -split '  - m_Id: ' | Select-Object -Skip 1)) {
        if ($bloque -notmatch '^(\d+)') { continue }
        $id = $Matches[1]
        if ($bloque -match '(?s)m_Localized:[ \t]*(.*?)(?=\r?\n[ \t]*m_Metadata:)') {
            # El YAML parte las líneas largas y las continúa indentadas.
            $mapa[$id] = ($Matches[1] -replace '\r?\n[ \t]+', ' ')
        }
    }
    return $mapa
}

function ConvertTo-TextoLimpio {
    param([string]$Crudo)
    if (-not $Crudo) { return '' }
    $t = $Crudo.Trim()

    if ($t -match '^"(.*)"$')      { $t = $Matches[1] }
    elseif ($t -match "^'(.*)'$")  { $t = $Matches[1] -replace "''", "'" }

    # Escapes del YAML
    $t = [regex]::Replace($t, '\\U([0-9A-Fa-f]{8})', { [char]::ConvertFromUtf32([Convert]::ToInt32($args[0].Groups[1].Value,16)) })
    $t = [regex]::Replace($t, '\\u([0-9A-Fa-f]{4})', { [char][Convert]::ToInt32($args[0].Groups[1].Value,16) })
    $t = [regex]::Replace($t, '\\x([0-9A-Fa-f]{2})', { [char][Convert]::ToInt32($args[0].Groups[1].Value,16) })
    $t = $t -replace '\\"', '"' -replace '\\r', '' -replace '\\n', "`n" -replace '\\\\', '\'

    $t = $t -replace '<[^>]*>', ''     # etiquetas de color
    $t = $t -replace '\{\d+\}', ''     # marcadores del typewriter

    $t = $t -replace '[ \t]+', ' ' -replace '[ \t]*\r?\n[ \t]*', "`n" -replace '\n{3,}', "`n`n"
    return $t.Trim()
}

# Parte el fragmento en intervenciones según las marcas [Niña] / [Bruja].
# Sin marcas, todo va con la voz de la niña (es quien narra).
function Split-PorHablante {
    param([string]$Texto)

    $trozos = [regex]::Split($Texto, '(?m)^\[([^\]]+)\]\s*$')
    $segmentos = @()

    if ($trozos.Count -eq 1) {
        if ($Texto.Trim()) { $segmentos += @{ Voz = $VozNina; Texto = $Texto.Trim(); Quien = 'Niña' } }
        return $segmentos
    }

    # Texto antes de la primera marca (narración)
    if ($trozos[0].Trim()) {
        $segmentos += @{ Voz = $VozNina; Texto = $trozos[0].Trim(); Quien = 'narración' }
    }

    for ($i = 1; $i -lt $trozos.Count; $i += 2) {
        $quien = $trozos[$i].Trim()
        $cuerpo = if ($i + 1 -lt $trozos.Count) { $trozos[$i + 1].Trim() } else { '' }
        if (-not $cuerpo) { continue }

        $voz = if ($quien -match '(?i)bruja|witch|sorci') { $VozBruja } else { $VozNina }
        $segmentos += @{ Voz = $voz; Texto = $cuerpo; Quien = $quien }
    }

    return $segmentos
}

# ===================== Llamada a la API =====================

function Invoke-TextoAVoz {
    param(
        [string]$Texto, [string]$VozId,
        [string]$Anterior, [string]$Siguiente
    )

    $cuerpo = @{
        text           = $Texto
        model_id       = $Modelo
        voice_settings = $Ajustes
    }
    # El contexto es lo que mantiene el acento y la entonación entre archivos.
    if ($Anterior)  { $cuerpo.previous_text = $Anterior }
    if ($Siguiente) { $cuerpo.next_text     = $Siguiente }

    $uri = "https://api.elevenlabs.io/v1/text-to-speech/$VozId?output_format=$Formato"

    $respuesta = Invoke-WebRequest -Uri $uri -Method Post `
        -Headers @{ 'xi-api-key' = $env:ELEVENLABS_API_KEY; 'Accept' = 'audio/mpeg' } `
        -ContentType 'application/json; charset=utf-8' `
        -Body ([Text.Encoding]::UTF8.GetBytes(($cuerpo | ConvertTo-Json -Depth 5)))

    return $respuesta.Content
}

# Une varios mp3. Los fotogramas mp3 son autónomos, así que basta concatenar los bytes
# quitando la cabecera ID3v2 de los que no son el primero.
function Join-Mp3 {
    param([byte[][]]$Partes)

    $salida = New-Object System.Collections.Generic.List[byte]
    $primero = $true

    foreach ($p in $Partes) {
        $inicio = 0
        if (-not $primero -and $p.Length -gt 10 -and
            $p[0] -eq 0x49 -and $p[1] -eq 0x44 -and $p[2] -eq 0x33) {
            # ID3v2: 10 bytes de cabecera + tamaño en 4 bytes sincsafe
            $tam = ($p[6] -shl 21) -bor ($p[7] -shl 14) -bor ($p[8] -shl 7) -bor $p[9]
            $inicio = 10 + $tam
        }
        for ($i = $inicio; $i -lt $p.Length; $i++) { $salida.Add($p[$i]) }
        $primero = $false
    }

    return $salida.ToArray()
}

# ===================== Programa =====================

if (-not $Simular -and -not $env:ELEVENLABS_API_KEY) {
    throw "Falta la clave. Ejecuta primero:  `$env:ELEVENLABS_API_KEY = 'tu-clave'"
}

$rutaShared = Join-Path $Tablas 'Story Shared Data.asset'
$rutaTabla  = Join-Path $Tablas "Story_$Idioma.asset"
foreach ($r in @($rutaShared, $rutaTabla)) {
    if (-not (Test-Path -LiteralPath $r)) { throw "No encuentro $r" }
}

$claves = Get-ClavesPorId -Ruta $rutaShared
$textos = Get-TextosPorId -Ruta $rutaTabla

# Fragmentos en orden, ya limpios
$fragmentos = @()
foreach ($id in $claves.Keys) {
    if (-not $textos.ContainsKey($id)) { continue }
    $limpio = ConvertTo-TextoLimpio -Crudo $textos[$id]
    if (-not $limpio) { continue }
    $fragmentos += [pscustomobject]@{ Clave = $claves[$id]; Texto = $limpio }
}
$fragmentos = $fragmentos | Sort-Object Clave

if ($Solo.Count -gt 0) {
    $fragmentos = $fragmentos | Where-Object { $Solo -contains $_.Clave }
    if (-not $fragmentos) { throw "Ninguna de las claves indicadas existe en Story_$Idioma" }
}

if (-not (Test-Path -LiteralPath $Destino)) {
    New-Item -ItemType Directory -Path $Destino -Force | Out-Null
}

Write-Host "Idioma: $Idioma | fragmentos: $($fragmentos.Count) | destino: $Destino`n"

$generados = 0; $saltados = 0; $caracteres = 0

for ($f = 0; $f -lt $fragmentos.Count; $f++) {
    $frag = $fragmentos[$f]
    $sufijo = $frag.Clave -replace '^story\.', ''
    $archivo = Join-Path $Destino "$Idioma-story-$sufijo.mp3"

    if ((Test-Path -LiteralPath $archivo) -and -not $Forzar) {
        Write-Host "  = $($frag.Clave): ya existe, se salta" -ForegroundColor DarkGray
        $saltados++
        continue
    }

    # @() fuerza array: con un solo segmento, PowerShell devolvería el hashtable suelto
    # y .Count contaría sus claves en vez de los segmentos.
    $segmentos = @(Split-PorHablante -Texto $frag.Texto)
    $resumen = ($segmentos | ForEach-Object { $_.Quien }) -join ' + '
    $nChars = ($segmentos | ForEach-Object { $_.Texto.Length } | Measure-Object -Sum).Sum
    $caracteres += $nChars

    Write-Host "  > $($frag.Clave): $($segmentos.Count) segmento(s) [$resumen], $nChars caracteres"

    if ($Simular) { $generados++; continue }

    $partes = @()
    for ($s = 0; $s -lt $segmentos.Count; $s++) {
        # Contexto: el segmento anterior y el siguiente dentro del mismo fragmento; en los
        # bordes, el fragmento contiguo. Así la voz no "reinicia" en cada archivo.
        $anterior = if ($s -gt 0) { $segmentos[$s-1].Texto }
                    elseif ($f -gt 0) { $fragmentos[$f-1].Texto } else { $null }
        $siguiente = if ($s -lt $segmentos.Count - 1) { $segmentos[$s+1].Texto }
                     elseif ($f -lt $fragmentos.Count - 1) { $fragmentos[$f+1].Texto } else { $null }

        $partes += ,(Invoke-TextoAVoz -Texto $segmentos[$s].Texto -VozId $segmentos[$s].Voz `
                                      -Anterior $anterior -Siguiente $siguiente)
        Start-Sleep -Seconds $PausaEntrePeticiones
    }

    [IO.File]::WriteAllBytes($archivo, (Join-Mp3 -Partes $partes))
    Write-Host "    guardado: $(Split-Path -Leaf $archivo)" -ForegroundColor Green
    $generados++
}

Write-Host "`nGenerados: $generados | saltados: $saltados | caracteres: $caracteres"
if ($Simular) { Write-Host "(simulación: no se llamó a la API)" -ForegroundColor Yellow }
