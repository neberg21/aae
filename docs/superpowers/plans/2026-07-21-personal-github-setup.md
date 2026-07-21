# Personal GitHub Setup Script Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a one-time Windows CMD wizard that wires this repo to the personal GitHub account via SSH so normal `git push` uses personal credentials.

**Architecture:** A single repo-root-relative `.cmd` under `scripts/` orchestrates existing CLI tools (`ssh-keygen`, file append to `%USERPROFILE%\.ssh\config`, `git config`/`git remote`, `ssh -T`, optional `git push`). No new packages. Idempotent for key and Host block.

**Tech Stack:** Windows CMD, OpenSSH (`ssh-keygen`, `ssh`), Git

**Spec:** `docs/superpowers/specs/2026-07-21-personal-github-setup-design.md`

## Global Constraints

- Script must be Windows `.cmd` / `.bat` only (no PowerShell, no bash)
- Personal author (local only): `kenny` / `kenneth.newall@proton.me`
- Remote URL: `git@github.com-personal:neberg21/aae.git`
- SSH key path: `%USERPROFILE%\.ssh\id_ed25519_personal`
- Host alias: `github.com-personal` with `IdentitiesOnly yes`
- Do not change global `user.name` / `user.email`
- Do not overwrite an existing personal private key
- English console messages
- Push only after Y/N prompt
- Do not commit unless the user explicitly asks

## File Structure

| File | Responsibility |
|------|----------------|
| `scripts/setup-personal-github.cmd` | Full wizard: key, SSH config, pause for GitHub key add, local git author, remote URL, SSH test, optional push |
| `docs/superpowers/specs/2026-07-21-personal-github-setup-design.md` | Already written; no change required unless behavior drifts |

---

### Task 1: Create `scripts/setup-personal-github.cmd`

**Files:**
- Create: `scripts/setup-personal-github.cmd`
- Test: manual CMD verification commands below

**Interfaces:**
- Consumes: OpenSSH + Git on PATH; repo has (or will get) `origin`
- Produces: personal key (if missing), `Host github.com-personal` in user SSH config, local git author + SSH remote for this repo

- [ ] **Step 1: Create `scripts/` directory if missing**

Run from repo root:

```cmd
if not exist scripts mkdir scripts
```

- [ ] **Step 2: Write the wizard script**

Create `scripts/setup-personal-github.cmd` with exactly this content:

```bat
@echo off
setlocal EnableExtensions

rem Personal GitHub setup for this repo only.
rem Spec: docs/superpowers/specs/2026-07-21-personal-github-setup-design.md

set "REPO_ROOT=%~dp0.."
pushd "%REPO_ROOT%" >nul

set "SSH_DIR=%USERPROFILE%\.ssh"
set "KEY_PATH=%SSH_DIR%\id_ed25519_personal"
set "PUB_PATH=%KEY_PATH%.pub"
set "SSH_CONFIG=%SSH_DIR%\config"
set "HOST_ALIAS=github.com-personal"
set "REMOTE_URL=git@github.com-personal:neberg21/aae.git"
set "GIT_USER_NAME=kenny"
set "GIT_USER_EMAIL=kenneth.newall@proton.me"

echo.
echo === Personal GitHub setup (this repo) ===
echo.

rem --- Ensure .ssh directory ---
if not exist "%SSH_DIR%" (
  mkdir "%SSH_DIR%"
  if errorlevel 1 (
    echo ERROR: Could not create "%SSH_DIR%".
    popd >nul
    exit /b 1
  )
)

rem --- SSH key (do not overwrite) ---
if exist "%KEY_PATH%" (
  echo SSH key already exists: "%KEY_PATH%"
) else (
  echo Creating personal SSH key...
  ssh-keygen -t ed25519 -C "%GIT_USER_EMAIL%" -f "%KEY_PATH%" -N ""
  if errorlevel 1 (
    echo ERROR: ssh-keygen failed.
    popd >nul
    exit /b 1
  )
  echo Created "%KEY_PATH%"
)

if not exist "%PUB_PATH%" (
  echo ERROR: Public key not found at "%PUB_PATH%".
  popd >nul
  exit /b 1
)

rem --- SSH config Host block (idempotent) ---
findstr /C:"Host %HOST_ALIAS%" "%SSH_CONFIG%" >nul 2>&1
if %ERRORLEVEL%==0 (
  echo SSH config already has "Host %HOST_ALIAS%".
) else (
  echo Adding "Host %HOST_ALIAS%" to "%SSH_CONFIG%"...
  if not exist "%SSH_CONFIG%" (
    type nul > "%SSH_CONFIG%"
  ) else (
    echo.>> "%SSH_CONFIG%"
  )
  >>"%SSH_CONFIG%" echo Host %HOST_ALIAS%
  >>"%SSH_CONFIG%" echo   HostName github.com
  >>"%SSH_CONFIG%" echo   User git
  >>"%SSH_CONFIG%" echo   IdentityFile %KEY_PATH%
  >>"%SSH_CONFIG%" echo   IdentitiesOnly yes
  echo Host block added.
)

rem --- Public key handoff ---
echo.
echo Add this public key to your PERSONAL GitHub account:
echo   GitHub -^> Settings -^> SSH and GPG keys -^> New SSH key
echo.
type "%PUB_PATH%"
echo.
pause

rem --- Local git author (repo only) ---
git config --local user.name "%GIT_USER_NAME%"
if errorlevel 1 (
  echo ERROR: Could not set local user.name. Are you inside a git repo?
  popd >nul
  exit /b 1
)
git config --local user.email "%GIT_USER_EMAIL%"
if errorlevel 1 (
  echo ERROR: Could not set local user.email.
  popd >nul
  exit /b 1
)
echo Local author set to %GIT_USER_NAME% ^<%GIT_USER_EMAIL%^>

rem --- Remote origin ---
git remote get-url origin >nul 2>&1
if errorlevel 1 (
  git remote add origin "%REMOTE_URL%"
) else (
  git remote set-url origin "%REMOTE_URL%"
)
if errorlevel 1 (
  echo ERROR: Could not set origin remote.
  popd >nul
  exit /b 1
)
echo origin set to %REMOTE_URL%

rem --- Verify SSH auth ---
echo.
echo Testing SSH: ssh -T git@%HOST_ALIAS%
echo (GitHub often returns exit code 1 even when authentication succeeds.)
ssh -T git@%HOST_ALIAS%
echo.

rem --- Optional push ---
set "DO_PUSH="
set /p DO_PUSH=Push current branch to origin now? [Y/N]: 
if /I "%DO_PUSH%"=="Y" (
  git push -u origin HEAD
  if errorlevel 1 (
    echo ERROR: git push failed. Confirm the SSH key is added to the personal account.
    popd >nul
    exit /b 1
  )
) else (
  echo Skipping push. Use: git push
)

echo.
echo Tip: If old HTTPS logins interfere, remove git:https://github.com entries
echo from Windows Credential Manager -^> Windows Credentials.
echo.
echo Done. Later pushes in this repo use the personal account via SSH.
echo.

popd >nul
endlocal
exit /b 0
```

- [ ] **Step 3: Sanity-check the script file exists and is non-empty**

```cmd
dir scripts\setup-personal-github.cmd
```

Expected: one file listed with a non-zero size.

- [ ] **Step 4: Dry verification of embedded constants (no full run required yet)**

```cmd
findstr /C:"kenneth.newall@proton.me" /C:"kenny" /C:"github.com-personal" /C:"neberg21/aae.git" scripts\setup-personal-github.cmd
```

Expected: matches for email, name, host alias, and remote path.

- [ ] **Step 5: Manual end-to-end verification (user machine)**

Run from repo root:

```cmd
scripts\setup-personal-github.cmd
```

Then confirm:

```cmd
git remote -v
git config --local --get user.name
git config --local --get user.email
git config --global --get user.email
ssh -T git@github.com-personal
```

Expected:
- `origin` is `git@github.com-personal:neberg21/aae.git`
- local name `kenny`, local email `kenneth.newall@proton.me`
- global email still the work address (unchanged)
- SSH greets the personal GitHub user (e.g. `neberg21`)

- [ ] **Step 6: Commit only if the user explicitly asks**

Do not commit in this task unless requested. If requested later, stage:

- `scripts/setup-personal-github.cmd`
- `docs/superpowers/specs/2026-07-21-personal-github-setup-design.md`
- `docs/superpowers/plans/2026-07-21-personal-github-setup.md`

---

## Spec coverage (self-review)

| Spec requirement | Task |
|------------------|------|
| Create personal key if missing | Task 1 |
| Do not overwrite existing key | Task 1 |
| Idempotent `Host github.com-personal` | Task 1 |
| Print pubkey + pause | Task 1 |
| Local `kenny` / `kenneth.newall@proton.me` | Task 1 |
| Remote `git@github.com-personal:neberg21/aae.git` | Task 1 |
| `ssh -T` verify | Task 1 |
| Optional Y/N push | Task 1 |
| English messages + Credential Manager tip | Task 1 |
| No global author change / no `.ps1`/`.sh` | Task 1 constraints |

## Placeholder scan

No TBD/TODO placeholders. Full script body included.
