# Repository Guidelines

## Project Structure & Module Organization

This repository is for **Mania Survival**, a Unity 6 mobile-first top-down arcade survival game. The repo is currently minimal, so add Unity content using standard layout:

- `Assets/ManiaSurvival/Scenes/` for playable and test scenes.
- `Assets/ManiaSurvival/Scripts/` for C# gameplay scripts, grouped by feature such as `Core/`, `Player/`, `Monster/`, `Pickups/`, `Hazards/`, and `UI/`.
- `Assets/ManiaSurvival/Prefabs/` for reusable player, monster, pickup, trap, portal, and hazard objects.
- `Assets/ManiaSurvival/Art/`, `Assets/ManiaSurvival/Audio/`, and `Assets/ManiaSurvival/Materials/` for source assets.
- `Assets/ManiaSurvival/Tests/` for Unity Test Framework tests if added.

## Build, Test, and Development Commands

No command wrappers are committed yet. Use Unity 6 to open the repository root once project files are present. Useful future commands:

- `Unity -projectPath .` opens the project from the command line.
- `Unity -batchmode -projectPath . -runTests -testPlatform EditMode` runs edit mode tests.
- `Unity -batchmode -projectPath . -runTests -testPlatform PlayMode` runs play mode tests.

Commit wrapper scripts or CI workflows before treating them as required.

## Coding Style & Naming Conventions

Write small, beginner-friendly C# scripts with one clear responsibility. Prefer public Inspector fields grouped with `[Header("...")]` for tunable gameplay values. Avoid catch-all manager scripts and class renames unless requested.

Use PascalCase for classes, methods, properties, and Unity asset names. Use camelCase for private fields and locals. Keep MonoBehaviour filenames identical to class names, for example `PlayerDashAbility.cs`.

## Testing Guidelines

Use the Unity Test Framework when tests are added. Put edit mode tests in `Assets/ManiaSurvival/Tests/EditMode/` and runtime tests in `Assets/ManiaSurvival/Tests/PlayMode/`. Name test files after behavior, for example `PickupCollectionTests.cs`.

For gameplay changes, include manual Unity verification notes: where to attach scripts, Inspector fields to assign, and how to test in-scene.

## Commit & Pull Request Guidelines

Git history currently contains only `first commit`, so no strict convention exists yet. Use short imperative messages such as `Add player dash ability` or `Tune pickup spawn timing`.

Pull requests should include a summary, testing notes, and screenshots or clips for visible gameplay/UI changes. Link related issues when available.

## Agent-Specific Instructions

Keep the game identity focused on one Monster hunting Survivors for 3-5 minute rounds. Survivors win by staying alive until the timer ends; Monster wins by eliminating all Survivors. Avoid networking, monetization, gacha, base building, repair loops, task objectives, and copyrighted assets or names from inspirations.
