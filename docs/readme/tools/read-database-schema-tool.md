# ReadDatabaseSchemaTool

*Read database schema details from a SQL Server database named SHEFFIELD on localhost.*

## Usage

This tool is specifically designed to query schema information from a local SQL Server database instance, expecting a database named `SHEFFIELD`. It can retrieve information about tables or columns, optionally filtered by name.

**Parameters:**
-   `detailType` (string, required): Specifies the type of schema information to retrieve. Valid values are:
    -   `table`: Returns information about tables (schema, name, type).
    -   `column`: Returns information about columns (schema, table, column name, data type, max length, nullability, default value).
-   `filter` (string, optional): An optional filter string.
    -   If `detailType` is "table", this filters table names (e.g., "Customer" would find tables with "Customer" in their name).
    -   If `detailType` is "column", this filters by table name to get columns for specific tables (e.g., "Orders" would list columns for tables named like "Orders").

## Examples

To list all tables in the SHEFFIELD database:

```json
{
  "detailType": "table"
}
```

To list columns for tables whose names contain "Product":

```json
{
  "detailType": "column",
  "filter": "Product"
}
```

## Notes

-   **Hardcoded Connection:** This tool currently uses a hardcoded connection string: `Data Source=localhost;Initial Catalog=SHEFFIELD;Integrated Security=True;TrustServerCertificate=True`. It assumes Windows Authentication and a local SQL Server instance with a database named `SHEFFIELD`.
-   The output is formatted plain text, providing a summary of the requested schema details.
-   If the database, tables, or columns are not found based on the filter, it will indicate that in the output.