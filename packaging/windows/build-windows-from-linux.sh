#!/bin/bash
# Build un installeur Windows .exe depuis Linux
# Prérequis : sudo pacman -S nsis
set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"
PROJECT="${ROOT}/Pass-all/Pass-all.csproj"
PUBLISH_DIR="${ROOT}/build/publish-windows"
OUTPUT="${ROOT}/build/Pass-all-Setup-Windows-x64.exe"

# ── Vérification de NSIS ──────────────────────────────────────────────────────
if ! command -v makensis &>/dev/null; then
    echo "ERREUR : NSIS n'est pas installé."
    echo "  Installe-le avec : sudo pacman -S nsis"
    exit 1
fi

# ── 0. Synchroniser le logo en .ico ───────────────────────────────────────────
LOGO_SRC="${ROOT}/packaging/logo.png"
LOGO_ICO="${SCRIPT_DIR}/logo.ico"
if [ -f "${LOGO_SRC}" ] && command -v magick &>/dev/null; then
    magick "${LOGO_SRC}" -define icon:auto-resize=256,128,64,48,32,16 "${LOGO_ICO}"
    cp "${LOGO_ICO}" "${ROOT}/Pass-all/logo.ico"
fi

# ── 1. Publish pour Windows ───────────────────────────────────────────────────
echo "▶ Publication pour win-x64..."
dotnet publish "${PROJECT}" \
    -c Release \
    -r win-x64 \
    --self-contained true \
    -p:PublishSingleFile=false \
    -p:PublishReadyToRun=true \
    -o "${PUBLISH_DIR}"

# ── 2. Créer l'installeur avec NSIS ──────────────────────────────────────────
echo "▶ Compilation de l'installeur NSIS..."
makensis \
    -DAPP_VERSION="1.0.0" \
    "${SCRIPT_DIR}/Pass-all.nsi"

echo ""
echo "✓ Installeur Windows créé : ${OUTPUT}"
echo "  Taille : $(du -sh "${OUTPUT}" | cut -f1)"
