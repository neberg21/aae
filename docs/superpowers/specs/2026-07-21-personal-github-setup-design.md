# Design: Personal GitHub Setup Script

**Date:** 2026-07-21  
**Status:** Approved for implementation planning  
**Output:** `scripts/setup-personal-github.cmd`

## Goal

One-time Windows CMD wizard that wires this repo to the **personal** GitHub account via SSH host alias, so normal `git push` / `git pull` use personal credentials instead of the machine default (work) identity.

## Decisions

| Topic | Choice |
|-------|--------|
| Approach | Full personal setup (key + SSH config + remote + local author) |
| Script type | Windows `.cmd` (repo Windows scripting policy) |
| Auth model | Permanent SSH remote using `Host github.com-personal` |
| Scope | This repo’s remote + local git author; user-home SSH files |
| Personal author | `kenny` / `kenneth.newall@proton.me` |
| Default remote | `git@github.com-personal:neberg21/aae.git` |
| Push | Prompt after successful SSH test; do not push silently |

## Behavior

1. **SSH key** — If `%USERPROFILE%\.ssh\id_ed25519_personal` is missing, create it with `ssh-keygen -t ed25519 -C "kenneth.newall@proton.me" -f ...` (no passphrase prompt automation beyond what `ssh-keygen` provides; prefer empty passphrase only if user accepts the prompt).
2. **SSH config** — Ensure `%USERPROFILE%\.ssh\config` contains a `Host github.com-personal` block:
   - `HostName github.com`
   - `User git`
   - `IdentityFile` → personal key path
   - `IdentitiesOnly yes`
   - Idempotent: skip if the Host block already exists.
3. **Public key handoff** — Print `id_ed25519_personal.pub` and pause so the user can add it on personal GitHub → Settings → SSH and GPG keys.
4. **Repo git config (local only)**:
   - `user.name=kenny`
   - `user.email=kenneth.newall@proton.me`
5. **Remote** — `git remote set-url origin git@github.com-personal:neberg21/aae.git` (create `origin` if missing).
6. **Verify** — `ssh -T git@github.com-personal` (expect success / “Hi neberg21!” style message; treat auth success as non-fatal exit codes that still mean login worked).
7. **Optional push** — Ask Y/N; on Y run `git push -u origin HEAD` (or current branch).

## Content rules

- Do not change global `user.name` / `user.email` (work default stays).
- Do not overwrite an existing personal key file.
- Do not commit secrets; public key may be printed to console only.
- English messages in the script output.
- Tip at end: remove stale `git:https://github.com` Windows Credential Manager entries if HTTPS was used before.

## Verification

- After running the script once (with key added on GitHub):
  - `git remote -v` shows `git@github.com-personal:neberg21/aae.git`
  - `git config --local --get user.email` is `kenneth.newall@proton.me`
  - `ssh -T git@github.com-personal` authenticates as the personal account
  - Work repos / global git identity remain unchanged
  - Subsequent plain `git push` uses the personal account

## Non-goals

- Managing the work SSH key or `github.com-work` host (optional future)
- Installing GitHub CLI (`gh`)
- Changing commit history / rewriting existing commit authors
- PowerShell or bash scripts
