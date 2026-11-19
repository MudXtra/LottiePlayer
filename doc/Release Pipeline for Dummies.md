# LottiePlayer Release Pipeline for Dummies

`.github/workflows/Release_LottiePlayer.yml` (**Release LottiePlayer**) releases `Blazor.Lottie.Player` when the version changes. There is nothing to start by hand.

## How to release

1. Open a PR to `main` that changes `<Version>` in `src/Blazor.Lottie.Player/Blazor.Lottie.Player.csproj` (stable `X.Y.Z` only).
2. Merge it. The workflow runs on the push to `main`.

Ordinary merges that do not change the version find everything already released and stop after the **plan** job.

## What it does

1. **plan** reads `<Version>` at the pushed commit and checks: is it on NuGet, is there a published GitHub Release `vX.Y.Z`. If both exist, it stops. It fails if the version is lower than the highest published version and not already on NuGet.
2. **build** builds net8.0/net9.0/net10.0/net11.0, runs `tests/Blazor.Lottie.Player.UnitTests`, and packs.
3. **nuget** publishes the `.nupkg` and `.snupkg` with `--skip-duplicate` (only if NuGet does not have the version yet).
4. **github-release** creates a published release `vX.Y.Z` tagged at the exact commit, with generated notes and the packages attached. A leftover draft for the same tag is reused. It never moves an existing tag that points to a different commit.

Each stage runs only if still needed, so re-running after a partial failure finishes only the missing parts.

## Re-running

**Actions → Release LottiePlayer → Run workflow** on `main`. If everything is already released it is a no-op.

## One-time setup (owner)

Do (1) before the first release: GitHub auto-creates the `nuget-release` environment on first run without a branch rule.

1. GitHub → Settings → Environments → `nuget-release` (create it) → Deployment branches: **Selected branches** → add `main` only. No required reviewers, no wait timer.
2. nuget.org → account → **Trusted Publishing** → add a policy: Repository Owner `MudXtra`, Repository `LottiePlayer`, Workflow File `Release_LottiePlayer.yml`, Environment `nuget-release`. The policy owner must be the account that owns `Blazor.Lottie.Player`.
3. Add the repository secret `NUGET_USERNAME` = that nuget.org profile name. The workflow fails on MudXtra/LottiePlayer if it is missing, and skips NuGet with a warning on forks.

No `NUGET_KEY` is used. The policy binds to the workflow file name and the `nuget-release` environment, so keep the file name, keep the nuget job's `environment: nuget-release`, and keep the push inside `Release_LottiePlayer.yml`.
