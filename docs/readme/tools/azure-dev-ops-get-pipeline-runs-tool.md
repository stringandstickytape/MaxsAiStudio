# AzureDevOpsGetPipelineRunsTool

*Retrieves pipeline execution data with granular analysis capabilities including detailed timeline data, task-level performance metrics, log analysis with error/warning detection, and execution bottleneck identification.*

## Usage

This tool allows you to analyze the execution history of your pipelines.

**Parameters:**
- `organization` (string, required): The name of your Azure DevOps organization.
- `project` (string, required): The name of the project within the organization.
- `run_type` (string, optional, default: "all"): Type of pipeline runs to retrieve. Can be "build", "release", "yaml", or "all".
- `definition_id` (integer, optional): Filter by a specific pipeline definition ID.
- `run_id` (integer, optional): A specific run ID to retrieve.
- `status_filter` (array of strings, optional, default: ["all"]): Filter by run status. Can be ["inProgress", "completed", "cancelling", "postponed", "notStarted", "all"].
- `result_filter` (array of strings, optional, default: ["all"]): Filter by run result. Can be ["succeeded", "partiallySucceeded", "failed", "canceled", "all"].
- `branch_name` (string, optional): Filter by source branch.
- `from_date` (string, optional): Filter runs from this date (ISO 8601 format).
- `to_date` (string, optional): Filter runs until this date (ISO 8601 format).
- `include_logs` (boolean, optional, default: false): Include detailed log analysis.
- `include_artifacts` (boolean, optional, default: false): Include artifact information.
- `include_tests` (boolean, optional, default: false): Include test results and coverage data.
- `include_timeline` (boolean, optional, default: false): Include detailed task-level timeline.
- `top` (integer, optional, default: 100): The maximum number of runs to return.
- `skip` (integer, optional, default: 0): The number of runs to skip, for pagination.

## Examples

To get the last 5 failed builds for a specific definition:

```json
{
  "organization": "MyCompany",
  "project": "MyProject",
  "run_type": "build",
  "definition_id": 123,
  "result_filter": ["failed"],
  "top": 5
}
```

## Notes

- Requires a PAT with "Build (Read)" and "Release (Read)" permissions.
- The output is a formatted summary of the pipeline runs, including detailed timeline and log analysis if requested.