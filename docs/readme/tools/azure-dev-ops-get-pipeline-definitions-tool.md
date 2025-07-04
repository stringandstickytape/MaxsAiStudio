# AzureDevOpsGetPipelineDefinitionsTool

*Retrieves pipeline definitions and configurations from Azure DevOps, including build pipelines, release pipelines, and YAML pipelines.*

## Usage

This tool allows you to get a detailed overview of the pipeline configurations in your Azure DevOps project.

**Parameters:**
- `organization` (string, required): The name of your Azure DevOps organization.
- `project` (string, required): The name of the project within the organization.
- `pipeline_type` (string, optional, default: "all"): Type of pipeline definitions to retrieve. Can be "build", "release", "yaml", or "all".
- `definition_id` (integer, optional): A specific pipeline definition ID to retrieve.
- `name_filter` (string, optional): Filter definitions by name.
- `include_variables` (boolean, optional, default: false): Include pipeline variables.
- `include_triggers` (boolean, optional, default: false): Include trigger configurations.
- `include_tasks` (boolean, optional, default: false): Include detailed task configurations.
- `include_permissions` (boolean, optional, default: false): Include security and permission settings.
- `status_filter` (string, optional, default: "all"): Filter by pipeline status. Can be "enabled", "disabled", or "all".
- `top` (integer, optional, default: 100): The maximum number of definitions to return.
- `skip` (integer, optional, default: 0): The number of definitions to skip, for pagination.

## Examples

To get all pipeline definitions in the `MyProject` project:

```json
{
  "organization": "MyCompany",
  "project": "MyProject"
}
```

## Notes

- Requires a PAT with "Build (Read)" and "Release (Read)" permissions.
- The output is a formatted summary of the pipeline definitions.