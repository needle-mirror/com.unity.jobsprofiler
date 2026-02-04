# Changelog

## [0.0.1-exp.8] - 2026-02-04

### Changed
* Split JobsProfilerColors for Burst compatibility
* Improve thread and group separator lines
* Use USS variables for theme colors
* Add top border to jobs info panel
* Add left padding and update theme colors in Stats
* Match foldout font size to main view
* Align single-thread group labels with bars

### Fixed
* Fix selection of small merged events
* Clear JobsInfo panel on data clear
* Correct preview lines in preview mode
* Fixed so compact mode shows arrows correctly
* Fix dependency lines in folded group mode
* Fix group separator positioning
* Fix timeline thread packing and memory leak

## [0.0.1-exp.7] - 2026-01-16

### Added
* Light theme support for Jobs Profiler
* Color-blind mode support using Unity's accessibility settings

### Changed
* Show link underline only on hover
* Removed vertical zoom experimental feature

### Fixed
* Sort order reset on hover in details view

## [0.0.1-exp.6] - 2026-01-13

### Added
* Toolbar search field for filtering events
* Settings menu (kebab menu) in toolbar
* Mini preview for folded thread groups
* Tree-view style indentation for thread labels under groups
* Dynamic visible depth - thread heights animate to match visible content depth

### Changed
* Use Foldout widget for thread and group labels
* Job group display names now use plural form ("Jobs", "Background Jobs")
* Standard background color for UI panels

### Fixed
* Grayscale filter for final merged bar
* Frame fading when switching between frames
* Filter out Main Thread root marker from display
* Dim filtered events instead of hiding them
* Thread fold state not restored correctly after unfolding a group

### Performance
* Optimize dependency lookup with hashmaps (O(n) to O(1) lookups)

## [0.0.1-exp.5] - 2025-11-03

Implemented initial version of thread and group folding.

### Fixed so it's eaiser to click on small events.

## [0.0.1-exp.4] - 2025-08-04

### Fixed so it's eaiser to click on small events.

## [0.0.1-exp.3] - 2025-07-30

* Fixed so event focus works on in-active frames.
* Updated Burst dependency to version 1.8.11
* Inspector UI
* Updated the `com.unity.burst` dependency to version `1.8.24`
* Updated the Burst dependency to version 1.8.25

## [0.0.1-exp.2] - 2025-07-29

### Fixes when pressing F and A for zooming the frame.

Inital release

## [0.0.1-exp.1] - 2025-07-23

### This is the first release of *Unity Package \<Jobs Profiler\>*.

Inital release
