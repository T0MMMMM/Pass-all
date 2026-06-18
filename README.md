<div align="center">

# 🔐 Pass-all

**Gestionnaire de mots de passe de bureau au design glassmorphism**

Construit avec Avalonia UI & .NET 8 — multiplateforme (Windows & Linux)

</div>

---

## 📑 Sommaire

- [Présentation](#-présentation)
- [Technologies](#-technologies)
- [Fonctionnalités](#-fonctionnalités)
- [Architecture du projet](#-architecture-du-projet)
- [Installation (utilisateurs)](#-installation-utilisateurs)
  - [Windows (.exe)](#windows-exe)
  - [Linux (.AppImage)](#linux-appimage)
- [Développement (depuis les sources)](#-développement-depuis-les-sources)
- [Générer ses propres versions (release)](#-générer-ses-propres-versions-release)
  - [Construire l'AppImage Linux](#construire-lappimage-linux)
  - [Construire l'installeur Windows](#construire-linstalleur-windows)
- [Sécurité & stockage des données](#-sécurité--stockage-des-données)

---

## 🎯 Présentation

**Pass-all** est une application de bureau permettant de stocker et organiser
ses identifiants en local. Chaque utilisateur dispose de son propre coffre
protégé par un compte, et peut classer ses comptes par catégories colorées.
L'application vérifie aussi si vos mots de passe ont déjà fuité sur internet
et évalue leur robustesse en temps réel.

L'interface utilise un design **glassmorphism** (verre dépoli translucide) avec
une fenêtre entièrement personnalisée (barre de titre faite main) et un thème
clair/sombre.

---

## 🧰 Technologies

| Domaine | Technologie |
|---|---|
| **Framework UI** | [Avalonia UI](https://avaloniaui.net/) `11.3.12` (+ FluentTheme) |
| **Plateforme** | .NET 8 (`net8.0`) |
| **Langage** | C# |
| **Base de données** | SQLite via Entity Framework Core `8.0.13` (`Microsoft.EntityFrameworkCore.Sqlite`) |
| **Icônes** | [IconPacks.Avalonia.Lucide](https://github.com/MahApps/IconPacks.Avalonia) `1.3.1` |
| **Police** | Space Grotesk (embarquée dans `Assets/`) |
| **Chiffrement** | AES-256 + hachage SHA-256 (projet `DllPass-all`) |
| **Fuites de mots de passe** | API [Have I Been Pwned](https://haveibeenpwned.com/API/v3#PwnedPasswords) (modèle *k-anonymity*) |
| **Packaging Linux** | AppImage (`appimagetool`) |
| **Packaging Windows** | Installeur NSIS (`.exe`) — générable aussi depuis Linux |

> Les *bindings* compilés Avalonia sont activés par défaut
> (`AvaloniaUseCompiledBindingsByDefault=true`). En build Debug, appuyez sur
> **F12** pour ouvrir l'inspecteur Avalonia DevTools.

---

## ✨ Fonctionnalités

### Comptes & authentification
- **Inscription / connexion** par utilisateur, chaque coffre est isolé.
- **Mode super-administrateur** : un compte `superadmin` peut voir l'ensemble
  des profils, avec affichage du propriétaire de chaque entrée.
- Mots de passe de connexion stockés sous forme de **hash SHA-256**.

### Gestion des identifiants (profils)
- **CRUD complet** : ajout, édition *inline* (directement dans la carte) et
  suppression de comptes.
- Champs par compte : **nom**, **identifiant**, **email**, **URL**, **mot de passe**.
- **Recherche** instantanée et **tri** des comptes.
- **Catégories colorées** personnalisables (Personnel, Travail, etc.) avec
  palette de couleurs prédéfinie.
- **Copier le mot de passe** dans le presse-papier en un clic.
- **Afficher / masquer** le mot de passe.
- **Générateur de mots de passe** à partir d'un dictionnaire de mots
  (assemblage de mots + caractère spécial).

### Sécurité & vérification
- **Détection de failles via une API externe** : chaque mot de passe est
  vérifié en ligne contre la base de données de fuites
  [**Have I Been Pwned**](https://haveibeenpwned.com/API/v3#PwnedPasswords).
  L'application interroge l'**API publique Pwned Passwords** et **affiche le
  nombre de fois où le mot de passe est apparu dans des fuites** connues,
  indiquant ainsi son niveau de sécurité.
- La requête utilise le modèle **k-anonymity** : seuls les 5 premiers caractères
  du hash SHA-1 du mot de passe sont envoyés, le mot de passe complet (ni même
  son hash entier) ne quitte jamais la machine.
- Vérification activable / désactivable depuis les paramètres.

### Interface
- Design **glassmorphism** translucide, fenêtre sans chrome personnalisée
  (déplacement / minimiser / maximiser / fermer faits main).
- **Thème clair / sombre** commutable (`ThemeManager`).
- **Aide intégrée** (page HTML embarquée dans `Help/`).
- **Journalisation** des erreurs dans un fichier `passall.log` (`Logger`).

### 🆕 Fonctionnalité ajoutée — Indicateur de force du mot de passe

Une **jauge de robustesse en temps réel** s'affiche sous chaque champ de saisie
de mot de passe, à la fois dans le **formulaire d'ajout** et dans l'**édition
inline** d'une carte.

- Barre à **4 segments** colorés + libellé : **Faible** → **Moyen** → **Bon** → **Fort**.
- Calcul par **heuristique simple, sans dépendance externe** : combine la
  **longueur** du mot de passe et le nombre de **classes de caractères**
  présentes (minuscules, majuscules, chiffres, symboles).
- Couleurs : rouge `#E5534B`, orange `#E0A030`, bleu `#3DA5D9`, vert `#2FBF71`.
- Se met à jour automatiquement à la frappe, **à la génération** d'un mot de
  passe et au **chargement d'un compte** en édition.

Implémentation : logique pure dans `PasswordStrength.Evaluate()`
(`Pass-all/Utils/Utils.cs`), UI dans `MainWindow.axaml`, styles `strength-seg`
/ `strength-label` dans `Styles/styles.axaml`.

---

## 🏗 Architecture du projet

La solution `Pass-all.sln` contient **deux projets** :

```
Pass-all/                      # Application Avalonia (namespace Passall)
├── App.axaml(.cs)             # Point d'entrée Avalonia, chargement des styles globaux
├── Program.cs                 # main()
├── AuthWindow.axaml(.cs)      # Fenêtre d'inscription / connexion
├── MainWindow.axaml(.cs)      # Fenêtre principale (code-behind, pas de MVVM)
├── Controls/AppTitleBar.*     # Barre de titre personnalisée
├── Modeles/                   # Entités EF Core + DataContext (DbContext)
│   ├── DataContext.cs         # DbContext SQLite
│   ├── DBUser.cs / DBUserProfile.cs / DBProfileCategory.cs
│   ├── DBSettings.cs / DBDictionary.cs
├── Migrations/                # Migrations EF Core
├── Styles/                    # styles.axaml (glassmorphism) + thèmes clair/sombre
├── Utils/                     # Logger, ThemeManager, PasswordGenerator,
│                              #   PasswordStrength, DatabaseSeeder, Constantes
├── Assets/                    # Police Space Grotesk, fond, logo
└── Help/                      # Aide HTML embarquée

DllPass-all/                   # Bibliothèque de chiffrement (namespace DllPass_all)
└── Cryptage.cs                # AES-256 (DllEncrypt/DllDecrypt) + SHA-256 (DllHash)
```

### Design system glassmorphism
Tous les styles sont centralisés dans `Styles/styles.axaml`. Convention de
couleurs : hex **ARGB** avec préfixe alpha pour la transparence
(ex. `#44FFFFFF` = blanc à 27 %, `#88FFFFFF` = blanc à 53 %).

### Couche données
EF Core + SQLite. La base est créée dans le dossier de données local de
l'utilisateur :
- **Linux** : `~/.local/share/Pass-all/passall.db`
- **Windows** : `%LOCALAPPDATA%\Pass-all\passall.db`

---

## 📦 Installation (utilisateurs)

Les versions prêtes à l'emploi se trouvent dans la page **Releases** du dépôt
GitHub :

➡️ **https://github.com/T0MMMMM/Pass-all/releases**

Téléchargez le fichier correspondant à votre système dans la dernière release.

### Windows (.exe)

1. Téléchargez `Pass-all-Setup-Windows-x64.exe` depuis la page Releases.
2. Lancez l'installeur et suivez l'assistant. L'application s'installe par
   défaut dans `C:\Program Files\Pass-all` et crée un raccourci dans le menu
   Démarrer.
3. Lancez **Pass-all** depuis le menu Démarrer.

> L'application est *self-contained* : **aucune installation de .NET n'est
> requise**.

### Linux (.AppImage)

1. Téléchargez `Pass-all-linux-x86_64.AppImage` depuis la page Releases.
2. Rendez le fichier exécutable puis lancez-le :

```bash
chmod +x Pass-all-linux-x86_64.AppImage
./Pass-all-linux-x86_64.AppImage
```

> L'AppImage embarque l'intégralité du runtime .NET, **aucune dépendance** à
> installer. Compatible avec la plupart des distributions x86-64.

---

## 🛠 Développement (depuis les sources)

Prérequis : [SDK .NET 8](https://dotnet.microsoft.com/download/dotnet/8.0).

```bash
# Restaurer les dépendances
dotnet restore

# Compiler
dotnet build

# Lancer l'application
dotnet run --project Pass-all/Pass-all.csproj

# Compiler en Release
dotnet publish -c Release Pass-all/Pass-all.csproj
```

### Migrations EF Core

```bash
dotnet ef migrations add NomDeLaMigration --project Pass-all/Pass-all.csproj
dotnet ef database update --project Pass-all/Pass-all.csproj
```

---

## 🚀 Générer ses propres versions (release)

Les scripts de packaging se trouvent dans `packaging/` et produisent leurs
artefacts dans le dossier `build/`.

### Construire l'AppImage Linux

```bash
./packaging/linux/build-appimage.sh
```

Ce script :
1. publie l'app en `linux-x64` *self-contained* (`build/publish-linux/`) ;
2. assemble une `AppDir` (binaire, `.desktop`, icône) ;
3. télécharge `appimagetool` si absent ;
4. produit **`build/Pass-all-linux-x86_64.AppImage`**.

> Prérequis recommandé : `ImageMagick` (commande `magick`) pour générer l'icône
> à la bonne taille. `wget` est utilisé pour récupérer `appimagetool`.

### Construire l'installeur Windows

L'installeur Windows peut être généré **depuis Windows** ou **depuis Linux**.

**Depuis Linux** (avec NSIS) :

```bash
# Arch Linux : sudo pacman -S nsis
./packaging/windows/build-windows-from-linux.sh
```

Ce script :
1. synchronise le logo `.ico` ;
2. publie l'app en `win-x64` *self-contained* (`build/publish-windows/`) ;
3. compile le script `Pass-all.nsi` avec `makensis` ;
4. produit **`build/Pass-all-Setup-Windows-x64.exe`**.

**Depuis Windows** : utilisez `packaging/windows/build.bat` (NSIS) ou ouvrez
`packaging/windows/Pass-all.iss` avec Inno Setup selon votre outil.

> Pour publier une nouvelle version, générez les deux artefacts puis
> téléversez `Pass-all-Setup-Windows-x64.exe` et
> `Pass-all-linux-x86_64.AppImage` dans une nouvelle release GitHub
> (`gh release create vX.Y.Z build/Pass-all-Setup-Windows-x64.exe build/Pass-all-linux-x86_64.AppImage`).

---

## 🔒 Sécurité & stockage des données

- Les **mots de passe des comptes** stockés dans le coffre sont chiffrés en
  **AES-256** (`DllPass-all/Cryptage.cs`).
- Les **mots de passe de connexion** des utilisateurs sont **hachés en
  SHA-256**.
- La **vérification de fuites** utilise le modèle *k-anonymity* de Have I Been
  Pwned : seuls les 5 premiers caractères du hash SHA-1 sont envoyés, le mot de
  passe complet ne quitte jamais l'appareil.
- Toutes les données sont stockées **localement** dans une base SQLite (voir
  [Couche données](#couche-données)).

> ⚠️ **Note** : ce projet a une vocation pédagogique. La clé AES est actuellement
> codée en dur dans `Cryptage.cs` et l'IV est nul — à durcir (dérivation de clé
> par utilisateur, IV aléatoire) avant tout usage réellement sensible.

---

<div align="center">

Dépôt : **https://github.com/T0MMMMM/Pass-all**

</div>
