# Jobs Profiler reference

Control how the CPU Usage Profiler displays jobs data with the New Timeline view.

The Jobs Profiler adds extra panels to the [CPU Usage module's](xref:um-profiler-cpu) Timeline view. These panels display information about the jobs in your application.

## Filter

Filter the Timeline view to display only jobs that you search for. When you search for a job, the Timeline view and jobs list only displays matching jobs which meet the search criteria.

## Display Settings

Customize how the Timeline displays data. Choose from the following options in the dropdown:

| Value                           | Description                    |
| :------------------------------ | :----------------------------- |
| **Zoom when changing event**    | Enable to zoom the timeline to make the selected job larger.|
| **Show Depends On**             | Displays lines in yellow between the selected job and any jobs that depend on it. |
| **Show Dependant On**           | Displays lines in yellow between the selected job and any jobs it depends on.|
| **Show Completed by (Wait)**    | Displays lines in red between the selected job and any jobs waiting to complete it. |
| **Show Completed by (No Wait)** | Displays lines in green between the selected job and any jobs that completed it.|
| **Show full dependency chain**  | Displays all job dependencies for the selected job. |

## Experimental Settings

Customize how to navigate the Timeline. Choose from the following options in the dropdown:

| Value                   | Description                    |
| :---------------------- | :----------------------------- |
| **Vertical Zoom**       | Enable this setting to zoom in and out of the Timeline view vertically. Press Alt + use the scroll wheel on your mouse to zoom in and out vertically. |
| **Zoom on event hover** | Enable this setting so that when you hover over an entry in the Job details panel, the Jobs Profiler zooms into and enlarges the entry in the Timeline view. If you disable this setting, when you hover over an entry in the Job details panel, the Jobs Profiler navigates to the entry in the Timeline view but doesn't zoom into it.|

## Job details panel

The job details panel to the right of the timeline view displays information about the selected job. Hover over any of the jobs to navigate to them in the timeline view. Click on any of the jobs to select it.

### Scheduled by

Displays what scheduled the job.

### Completed by

Displays what completed the job, if anything.

### Depends On

Displays a list of jobs which depend on the selected job, and how much time the jobs took during the selected frame.

### Dependant On

Displays a list of jobs the selected job depend on, and how much time the jobs took during the selected frame.

## Job list panel

The panel at the bottom of the window displays a list of all jobs in the selected frame.


| **Value**               | **Description**               |
| :---------------------- | :---------------------------- |
| **Job name**            | Displays the name of the job. |
| **Total count**         | The total number of times this job was called during the frame |
| **Minimum time**        | The minimum time in ms that the job took. Select the value to navigate to the job with the minimum time.|
| **Average time**        | The average time in ms that the job took.|
| **Maximum time**        | The maximum time in ms that the job took. Select the value to navigate to the job with the maximum time.|
| **Minimum start time**  | The minimum time in ms that the job took to start from the time it was requested. |
| **Average start time**  | The average time in ms that the job took to start from the time it was requested. |
| **Maximum start time**  | The maximum time in ms that the job took to start from the time it was requested. |
| **Minimum wait time**   | The minimum time in ms that the system waited for the job to start. |
| **Average wait time**   | The average time in ms that the system waited for the job to start. |
| **Maximum wait time**   | The maximum time in ms that the system waited for the job to start. |

## Additional resources

- [CPU Profiler reference](xref:um-profiler-cpu)
- [Using the Jobs Profiler](using-jobs-profiler.md)