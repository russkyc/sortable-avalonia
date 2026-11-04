# Changelog

All notable changes to **Sortable.Avalonia** are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [2.4.1] - 2026-11-04

### Fixed

- Inconsistent touch drag behavior. Only the pointer that starts a drag owns it, so additional fingers can no longer reset or end an active drag, and release/capture-lost events from other pointers are ignored.
- Ancestor gesture recognizers (for example a page back-swipe) could steal pointer capture mid-drag. Gesture recognition is now suppressed for the duration of the drag.
- Controls that are both `Sortable` and `Droppable` subscribed to pointer events twice, causing every pointer event to be processed twice. Subscriptions are now idempotent.
- Detaching the drag surface mid-drag left the drag stuck in a pressed state. Detaching now ends the drag cleanly.
- `ScrollViewer` is now treated as an interactive origin, preventing accidental item drags from starting on scrollable content.

## [2.4.0] - 2026-10-01

### Added

- `LastPreviewedIndex` on `SortableReleaseEventArgs` for tracking the last previewed drop index when an item is released.

### Fixed

- `WrapPanel` drop index calculation.
- Disposed objects were retained in memory; drag tracking now uses weak references ([#25](https://github.com/russkyc/sortable-avalonia/issues/25)).

## [2.3.1] - 2026-07-21

### Fixed

- Inconsistent droppable target behavior.
- Dragged items freezing when hovered over droppable targets.

## [2.3.0] - 2026-07-21

### Added

- **AutoScroll support**: parent and ancestor `ScrollViewer` viewports scroll automatically when dragging near the edges. Configurable via `AutoScroll`, `AutoScrollThreshold`, and `AutoScrollSpeed`.
- **ListBox compatibility**: native `ListBox` and other `SelectingItemsControl` derivatives support reordering with selection persistence.
- **AOT compatibility**: the package is marked `IsAotCompatible`, and hot drag paths are allocation-free.

### Fixed

- Touch devices prematurely releasing the dragged item.

## [2.2.0] - 2026-04-12

### Changed

- Migrated to Avalonia 12.
- Package now targets .NET 8.0 and .NET 10.0.
- Removed the FluentAvalonia dependency from the demo project.

## [2.0.1] - 2026-03-17

### Fixed

- Inconsistent outside-droppable-bounds detection.

## [2.0.0] - 2026-03-16

### Added

- `ReleaseCommand` and `SortableReleaseEventArgs` for handling items released outside any valid drop target.

### Changed

- **Breaking:** `AnimationDuration` now uses `TimeSpan` instead of `int` milliseconds. Update XAML and code to use TimeSpan format (for example, `0:0:0.500`).

[2.4.1]: https://github.com/russkyc/sortable-avalonia/compare/v2.4.0...v2.4.1
[2.4.0]: https://github.com/russkyc/sortable-avalonia/compare/v2.3.1...v2.4.0
[2.3.1]: https://github.com/russkyc/sortable-avalonia/compare/v2.3.0...v2.3.1
[2.3.0]: https://github.com/russkyc/sortable-avalonia/compare/v2.2.0...v2.3.0
[2.2.0]: https://github.com/russkyc/sortable-avalonia/compare/v2.0.1...v2.2.0
[2.0.1]: https://github.com/russkyc/sortable-avalonia/compare/v2.0.0...v2.0.1
[2.0.0]: https://github.com/russkyc/sortable-avalonia/releases/tag/v2.0.0
