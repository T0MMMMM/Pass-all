#!/bin/bash
set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"
PROJECT="${ROOT}/Pass-all/Pass-all.csproj"
PUBLISH_DIR="${ROOT}/build/publish-linux"
APPDIR="${ROOT}/build/Pass-all.AppDir"
OUTPUT="${ROOT}/build/Pass-all-linux-x86_64.AppImage"

# ── 1. Publish ────────────────────────────────────────────────────────────────
echo "▶ Publishing..."
dotnet publish "${PROJECT}" \
    -c Release \
    -r linux-x64 \
    --self-contained true \
    -p:PublishSingleFile=false \
    -p:PublishReadyToRun=true \
    -o "${PUBLISH_DIR}"

# ── 2. Build AppDir ───────────────────────────────────────────────────────────
echo "▶ Building AppDir..."
rm -rf "${APPDIR}"
mkdir -p "${APPDIR}/usr/lib/pass-all"
mkdir -p "${APPDIR}/usr/share/applications"
mkdir -p "${APPDIR}/usr/share/icons/hicolor/256x256/apps"

# App binaries
cp -r "${PUBLISH_DIR}/." "${APPDIR}/usr/lib/pass-all/"
chmod +x "${APPDIR}/usr/lib/pass-all/Pass-all"

# AppRun
cp "${SCRIPT_DIR}/AppRun" "${APPDIR}/AppRun"
chmod +x "${APPDIR}/AppRun"

# Desktop file
cp "${SCRIPT_DIR}/pass-all.desktop" "${APPDIR}/pass-all.desktop"
cp "${SCRIPT_DIR}/pass-all.desktop" "${APPDIR}/usr/share/applications/pass-all.desktop"

# Icon (PNG 256x256 requis) — synchronise depuis packaging/logo.png
ICON="${SCRIPT_DIR}/pass-all.png"
LOGO_SRC="${ROOT}/packaging/logo.png"
if [ -f "${LOGO_SRC}" ]; then
    if command -v magick &>/dev/null; then
        magick "${LOGO_SRC}" -resize 256x256 "${ICON}"
    else
        cp "${LOGO_SRC}" "${ICON}"
    fi
fi
if [ ! -f "${ICON}" ]; then
    echo "▶ Génération d'une icône temporaire (remplacez packaging/linux/pass-all.png par la vraie)"
    magick -size 256x256 xc:'#1e1b4b' \
        -fill '#6d28d9' \
        -draw "roundrectangle 0,0 256,256 48,48" \
        -fill '#1e1b4b' \
        -draw "roundrectangle 10,10 246,246 44,44" \
        -fill '#8b5cf6' \
        -draw "roundrectangle 18,18 238,238 40,40" \
        -fill white -font "DejaVu-Sans-Bold" -pointsize 72 \
        -gravity center -annotate 0 "PA" \
        "${ICON}" 2>/dev/null || {
            echo "  (magick non disponible, AppImage sans icône)"
            # Créer un PNG vide 1x1 comme fallback
            printf '\x89PNG\r\n\x1a\n\x00\x00\x00\rIHDR\x00\x00\x00\x01\x00\x00\x00\x01\x08\x02\x00\x00\x00\x90wS\xde\x00\x00\x00\x0cIDATx\x9cc\xf8\x0f\x00\x00\x01\x01\x00\x05\x18\xd8N\x00\x00\x00\x00IEND\xaeB`\x82' > "${ICON}"
        }
fi
cp "${ICON}" "${APPDIR}/pass-all.png"
cp "${ICON}" "${APPDIR}/usr/share/icons/hicolor/256x256/apps/pass-all.png"

# ── 3. appimagetool ──────────────────────────────────────────────────────────
TOOL="${ROOT}/build/appimagetool"
if [ ! -f "${TOOL}" ]; then
    echo "▶ Téléchargement de appimagetool..."
    mkdir -p "${ROOT}/build"
    wget -q --show-progress -O "${TOOL}" \
        "https://github.com/AppImage/appimagetool/releases/download/continuous/appimagetool-x86_64.AppImage"
    chmod +x "${TOOL}"
fi

# ── 4. Build AppImage ─────────────────────────────────────────────────────────
echo "▶ Création de l'AppImage..."
ARCH=x86_64 "${TOOL}" --appimage-extract-and-run "${APPDIR}" "${OUTPUT}"

echo ""
echo "✓ AppImage créé : ${OUTPUT}"
echo "  Taille : $(du -sh "${OUTPUT}" | cut -f1)"
