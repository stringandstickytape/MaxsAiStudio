




using Microsoft.Data.SqlClient;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using System.ComponentModel;





namespace AiStudio4.Core.Tools
{
    /// <summary>
    /// Implementation of the ReadSchemaDetails tool
    /// </summary>
    [McpServerToolType]
    public class ReadDatabaseSchemaTool : BaseToolImplementation
    {
        public ReadDatabaseSchemaTool(ILogger<ReadDatabaseSchemaTool> logger, IGeneralSettingsService generalSettingsService, IStatusMessageService statusMessageService) : base(logger, generalSettingsService, statusMessageService)
        {
        }

        /// <summary>
        /// Gets the ReadSchemaDetails tool definition
        /// </summary>
        public override Tool GetToolDefinition()
        {
            return new Tool
            {
                Guid = ToolGuids.READ_DATABASE_SCHEMA_TOOL_GUID,
                Name = "ReadDatabaseSchema",
                Description = "Read database schema details from SQL Server.",
                Schema = """
{
  "name": "ReadDatabaseSchema",
  "description": "Read database schema details from SQL Server. Can query table or column information from the SHEFFIELD database.",
  "input_schema": {
    "properties": {
      "detailType": { "type": "string", "description": "Type of schema details to retrieve: 'table' for table information or 'column' for column information" },
      "filter": { "type": "string", "description": "Optional filter. For tables: filter by table name. For columns: filter by table name to get columns for a specific table." }
    },
    "required": ["detailType"],
    "type": "object"
  }
}
""",
                Categories = new List<string> { "Development" },
                OutputFileType = "txt",
                Filetype = string.Empty,
                LastModified = DateTime.UtcNow,
                ExtraProperties = new Dictionary<string, string> {
                    { "DatabaseSchema", "SHEFFIELD" }
                }
            };
        }

        /// <summary>
        /// Processes a ReadSchemaDetails tool call
        /// </summary>
        public override async Task<BuiltinToolResult> ProcessAsync(string toolParameters, Dictionary<string, string> extraProperties)
        {
            _logger.LogInformation("ReadSchemaDetails tool called");
            SendStatusUpdate("Starting ReadDatabaseSchema tool execution...");
            var resultBuilder = new StringBuilder();

            try
            {
                var parameters = JsonConvert.DeserializeObject<Dictionary<string, string>>(toolParameters);
                string detailType = parameters["detailType"].ToLower();
                string filter = parameters.ContainsKey("filter") ? parameters["filter"] : null;
                
                SendStatusUpdate($"Reading database schema for type: {detailType}{(filter != null ? $", filter: {filter}" : "")}");

                // Get database schema from extraProperties or default to SHEFFIELD
                string databaseSchema = extraProperties != null && extraProperties.TryGetValue("DatabaseSchema", out var schemaName) && !string.IsNullOrWhiteSpace(schemaName) ? schemaName : "SHEFFIELD";

                // Connection string for SQL Server using Windows Authentication
                string connectionString = $@"Data Source=localhost;Initial Catalog={databaseSchema};Integrated Security=True;TrustServerCertificate=True";

                using (var connection = new SqlConnection(connectionString))
                {
                    SendStatusUpdate("Connecting to database...");
                    await connection.OpenAsync();

                    if (detailType == "table")
                    {
                        SendStatusUpdate("Retrieving table details...");
                        await GetTableDetailsAsync(connection, filter, resultBuilder);
                    }
                    else if (detailType == "column")
                    {
                        SendStatusUpdate("Retrieving column details...");
                        await GetColumnDetailsAsync(connection, filter, resultBuilder);
                    }
                    else
                    {
                        SendStatusUpdate($"Error: Invalid detailType '{detailType}'.");
                        resultBuilder.AppendLine($"Error: Invalid detailType '{detailType}'. Use 'table' or 'column'.");
                    }
                }

                SendStatusUpdate("Database schema retrieved successfully.");
                return CreateResult(true, true, resultBuilder.ToString());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing ReadSchemaDetails tool");
                SendStatusUpdate($"Error processing ReadDatabaseSchema tool: {ex.Message}");
                return CreateResult(true, true, $"Error processing ReadSchemaDetails tool: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets table details from INFORMATION_SCHEMA.TABLES
        /// </summary>
        private async Task GetTableDetailsAsync(SqlConnection connection, string tableNameFilter, StringBuilder resultBuilder)
        {
            string sql = @"
               SELECT 
                   TABLE_SCHEMA, 
                   TABLE_NAME, 
                   TABLE_TYPE
               FROM 
                   INFORMATION_SCHEMA.TABLES
               WHERE 
                   TABLE_TYPE = 'BASE TABLE'";

            if (!string.IsNullOrEmpty(tableNameFilter))
            {
                sql += " AND TABLE_NAME LIKE @TableName";
            }

            sql += " ORDER BY TABLE_SCHEMA, TABLE_NAME";

            using (var command = new SqlCommand(sql, connection))
            {
                if (!string.IsNullOrEmpty(tableNameFilter))
                {
                    command.Parameters.AddWithValue("@TableName", $"%{tableNameFilter}%");
                }

                using (var reader = await command.ExecuteReaderAsync())
                {
                    resultBuilder.AppendLine("--- Table Details ---");

                    if (!reader.HasRows)
                    {
                        resultBuilder.AppendLine("No tables found matching the criteria.");
                        return;
                    }

                    while (await reader.ReadAsync())
                    {
                        string schema = reader["TABLE_SCHEMA"].ToString();
                        string tableName = reader["TABLE_NAME"].ToString();
                        string tableType = reader["TABLE_TYPE"].ToString();

                        resultBuilder.AppendLine($"{tableName}");
                    }
                }
            }
        }

        /// <summary>
        /// Gets column details from INFORMATION_SCHEMA.COLUMNS
        /// </summary>
        private async Task GetColumnDetailsAsync(SqlConnection connection, string tableNameFilter, StringBuilder resultBuilder)
        {
            string sql = @"
                SELECT 
                    TABLE_SCHEMA,
                    TABLE_NAME, 
                    COLUMN_NAME, 
                    DATA_TYPE, 
                    CHARACTER_MAXIMUM_LENGTH,
                    IS_NULLABLE, 
                    COLUMN_DEFAULT
                FROM 
                    INFORMATION_SCHEMA.COLUMNS
                WHERE 
                    1=1";

            if (!string.IsNullOrEmpty(tableNameFilter))
            {
                sql += " AND TABLE_NAME LIKE @TableName";
            }

            sql += " ORDER BY TABLE_SCHEMA, TABLE_NAME, ORDINAL_POSITION";

            using (var command = new SqlCommand(sql, connection))
            {
                if (!string.IsNullOrEmpty(tableNameFilter))
                {
                    command.Parameters.AddWithValue("@TableName", $"%{tableNameFilter}%");
                }

                using (var reader = await command.ExecuteReaderAsync())
                {
                    resultBuilder.AppendLine("--- Column Details ---");

                    if (!reader.HasRows)
                    {
                        resultBuilder.AppendLine("No columns found matching the criteria.");
                        return;
                    }

                    resultBuilder.AppendLine("Schema | Table | Column | Data Type | Length | Nullable | Default");
                    resultBuilder.AppendLine("-------|-------|--------|-----------|--------|----------|--------");

                    while (await reader.ReadAsync())
                    {
                        string schema = reader["TABLE_SCHEMA"].ToString();
                        string tableName = reader["TABLE_NAME"].ToString();
                        string columnName = reader["COLUMN_NAME"].ToString();
                        string dataType = reader["DATA_TYPE"].ToString();

                        string length = reader["CHARACTER_MAXIMUM_LENGTH"] != DBNull.Value
                            ? reader["CHARACTER_MAXIMUM_LENGTH"].ToString()
                            : "N/A";

                        string isNullable = reader["IS_NULLABLE"].ToString();
                        string defaultValue = reader["COLUMN_DEFAULT"] != DBNull.Value
                            ? reader["COLUMN_DEFAULT"].ToString()
                            : "NULL";

                        resultBuilder.AppendLine($"{schema} | {tableName} | {columnName} | {dataType} | {length} | {isNullable} | {defaultValue}");
                    }
                }
            }
        }

        [McpServerTool, Description("Read database schema details from SQL Server.")]
        public async Task<string> ReadDatabaseSchema([Description("JSON parameters for ReadDatabaseSchema")] string parameters = "{}")
        {
            try
            {
                var result = await ProcessAsync(parameters, new Dictionary<string, string>());
                
                if (!result.WasProcessed)
                {
                    return $"Tool was not processed successfully.";
                }
                
                return result.ResultMessage ?? "Tool executed successfully with no output.";
            }
            catch (Exception ex)
            {
                return $"Error executing tool: {ex.Message}";
            }
        }
    }
}
