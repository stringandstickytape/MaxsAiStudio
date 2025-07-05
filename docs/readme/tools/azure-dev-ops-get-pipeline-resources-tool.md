# AzureDevOpsGetPipelineResourcesTool

*Retrieves pipeline-related resources including variable groups, service connections, agent pools, environments, and deployment groups.*

## Usage

This tool allows you to inspect the resources that your pipelines use.

**Parameters:**
- `organization` (string, required): The name of your Azure DevOps organization.
- `project` (string, required): The name of the project within the organization.
- `resource_type` (string, optional, default: "all"): Type of resources to retrieve. Can be "variableGroups", "serviceConnections", "agentPools", "environments", "deploymentGroups", or "all".
- `resource_id` (integer, optional): A specific resource ID to retrieve.
- `name_filter` (string, optional): Filter resources by name.
- `include_permissions` (boolean, optional, default: false): Include security and permission settings.
- `include_usage` (boolean, optional, default: false): Include which pipelines use these resources.
- `include_values` (boolean, optional, default: false): Include variable values (non-secret only) and connection details.
- `top` (integer, optional, default: 100): The maximum number of resources to return.
- `skip` (integer, optional, default: 0): The number of resources to skip, for pagination.

## Examples

To get all variable groups in the `MyProject` project:

```json
{
  "organization": "MyCompany",
  "project": "MyProject",
  "resource_type": "variableGroups"
}
```

## Notes

- Requires a PAT with appropriate permissions for the resources being requested (e.g., "Variable Groups (Read)", "Service Connections (Read)").
- The output is a formatted summary of the requested resources.