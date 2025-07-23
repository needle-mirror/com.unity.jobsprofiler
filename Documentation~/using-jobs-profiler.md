# Using the Jobs Profiler

To use the Jobs Profiler, you must enable it in the Profiler window.

## Prerequisites

* Install the Jobs Profiler [through the Package Manager window](xref:um-upm-ui-install).

## Enabling the Jobs Profiler

To enable the Jobs Profiler:

1. Open the Profiler window (menu: **Window &gt; Analysis &gt; Profiler**).
1. Select the **CPU Usage** Profiler module.
1. Select the **Timeline** dropdown at the top of the details pane, and then select **New Timeline (Experimental)**.

The details pane at the bottom of the Profiler window displays the Jobs Profiler timeline view.

## Capturing data

The workflow to capture data with the Jobs Profiler is the same as the CPU Profiler module:

1. Open the Profiler window (menu: **Window &gt; Analysis &gt; Profiler**).
1. Select the target player from the dropdown (Play mode, Edit mode, or a running player).
1. If the window doesn't automatically collect data, select the Record button.

For detailed information on how to capture data, refer to [Profiling your application](xref:um-profiler-profiling-applications).

> [!TIP]
> To navigate the Timeline view, rotate the wheel on your mouse to zoom in and out, or press and hold Alt + right-click to zoom in and out. Press and hold Alt + left-click to pan the view.

## Viewing data

When you select a job, the Jobs Profiler adds lines to the timeline view which displays the relationships between jobs as follows:

* Purple lines: When and where a job has been scheduled
* Yellow lines: Job dependencies
* Red lines: When a job is waiting and hasn't been completed
* Green lines: When a job is waiting and has been completed

## Additional resources

- [CPU Profiler reference](xref:um-profiler-cpu)
- [Profiling your application](xref:um-profiler-profiling-applications)
- [Jobs Profiler reference](ui-reference.md)
