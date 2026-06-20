# Script to patch Unity WebGL build index.html
# Suppresses yellow warnings, keeps red errors, fixes brotli issues

$buildIndexPath = "Build\index.html"

if (-not (Test-Path $buildIndexPath)) {
    Write-Host "Build\index.html not found. Please build the project first." -ForegroundColor Red
    exit 1
}

Write-Host "Patching Build\index.html..." -ForegroundColor Cyan

$content = Get-Content $buildIndexPath -Raw

# If already patched, strip old patch first to re-apply fresh
if ($content -match "// PATCHED: Console override") {
    Write-Host "Already patched, re-applying fresh patch..." -ForegroundColor Yellow
    # Remove previous patch block: from <script> // PATCHED ... </script>
    $content = $content -replace '(?s)\s*<script>\s*// PATCHED: Console override.*?</script>\s*', "`n"
}

# Find the script tag that creates Unity instance and inject console override before it
$consoleOverride = @"
    <script>
        // PATCHED: Console override to hide warnings
        (function() {
            var originalWarn = console.warn;
            var originalError = console.error;
            
            // Known Unity WebGL noise to suppress
            var suppressPatterns = [
                'Content-Type" configured incorrectly',
                'should be "application/wasm"',
                'Startup time performance will suffer',
                'wasm streaming compile failed',
                'falling back to ArrayBuffer',
                'Unable to parse Build/',
                'Content-Encoding: br',
                'Brotli compression may not be supported',
            ];

            function isSuppressed(args) {
                var msg = Array.prototype.join.call(args, ' ');
                for (var i = 0; i < suppressPatterns.length; i++) {
                    if (msg.indexOf(suppressPatterns[i]) !== -1) return true;
                }
                return false;
            }

            console.warn = function() {
                if (!isSuppressed(arguments)) originalWarn.apply(console, arguments);
            };
            
            console.error = function() {
                if (!isSuppressed(arguments)) originalError.apply(console, arguments);
            };
            
            // Override Unity's showBanner to only show errors
            window.unityShowBanner = function(msg, type) {
                if (type === 'error') {
                    var warningBanner = document.querySelector("#unity-warning");
                    if (warningBanner) {
                        warningBanner.innerHTML = msg;
                        warningBanner.style.display = 'block';
                    }
                }
            };
        })();
    </script>
"@

# Insert before the first <script> tag
$content = $content -replace '(<script)', "$consoleOverride`n`$1"

# Patch inline unityShowBanner: skip showing anything for 'warning' type
$oldBanner = @'
      function unityShowBanner(msg, type) {
        function updateBannerVisibility() {
          warningBanner.style.display = warningBanner.children.length ? 'block' : 'none';
        }
        var div = document.createElement('div');
        div.innerHTML = msg;
        warningBanner.appendChild(div);
        if (type == 'error') div.style = 'background: red; padding: 10px;';
        else {
          if (type == 'warning') div.style = 'background: yellow; padding: 10px;';
          setTimeout(function() {
            warningBanner.removeChild(div);
            updateBannerVisibility();
          }, 5000);
        }
        updateBannerVisibility();
      }
'@

$newBanner = @'
      function unityShowBanner(msg, type) {
        if (type == 'error') {
          var div = document.createElement('div');
          div.innerHTML = msg;
          div.style = 'background: red; padding: 10px;';
          warningBanner.appendChild(div);
          warningBanner.style.display = 'block';
        }
        // warnings are suppressed intentionally
      }
'@

$content = $content.Replace($oldBanner, $newBanner)

# Save patched version
Set-Content -Path $buildIndexPath -Value $content -NoNewline

Write-Host "Successfully patched Build\index.html!" -ForegroundColor Green
Write-Host "- Yellow warnings will be hidden" -ForegroundColor Yellow
Write-Host "- Red errors will still be visible" -ForegroundColor Red
Write-Host "- Brotli errors will be suppressed" -ForegroundColor Yellow
