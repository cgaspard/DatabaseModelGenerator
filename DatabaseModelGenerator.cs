using System;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Text;
using Microsoft.Data.SqlClient;

public class DatabaseModelGenerator
{
    private string _connectionString;
    private DbProviderFactory _dbProviderFactory;
    private string _schema;

    public DatabaseModelGenerator(string connectionString, DbProviderFactory dbProviderFactory, string schema)
    {
        _connectionString = connectionString;
        _dbProviderFactory = dbProviderFactory;
        _schema = schema;
    }

    public void GenerateModels(string outputDirectory)
    {
        if (!Directory.Exists(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }

        DataTable tableSchema = GetTableSchema();
        foreach (DataRow tableRow in tableSchema.Rows)
        {
            string tableName = tableRow["TABLE_NAME"].ToString();
            try {
                DataTable columnSchema = GetColumnSchema(tableName);

                string modelCode = GenerateModelCode(tableName, columnSchema);

                string modelFilePath = Path.Combine(outputDirectory, $"{tableName}.cs");
                File.WriteAllText(modelFilePath, modelCode);
            } catch (Exception ex) {
                Console.WriteLine($"Error getting schema for table {tableName}: {ex.Message}");
            }
        }
    }

    private DataTable GetTableSchema()
    {
        DataTable schema;

        using (var connection = _dbProviderFactory.CreateConnection())
        {
            connection.ConnectionString = _connectionString;
            connection.Open();

            // Get the current database name
            string currentDatabaseName = (connection as SqlConnection).Database;

            schema = connection.GetSchema("Tables");

            // Filter out tables from linked databases
            var rowsToRemove = new List<DataRow>();
            foreach (DataRow row in schema.Rows)
            {
                string tableCatalog = row["TABLE_CATALOG"].ToString();
                if (!tableCatalog.Equals(currentDatabaseName, StringComparison.OrdinalIgnoreCase))
                {
                    rowsToRemove.Add(row);
                }
            }

            // Remove the unwanted rows
            foreach (DataRow row in rowsToRemove)
            {
                schema.Rows.Remove(row);
            }

            schema.AcceptChanges();
        }

        return schema;
    }

    private DataTable GetColumnSchema(string tableName)
    {
        DataTable schema;

        using (var connection = _dbProviderFactory.CreateConnection())
        {
            connection.ConnectionString = _connectionString;
            connection.Open();

            using (var command = connection.CreateCommand())
            {
                command.CommandText = $"SELECT * FROM {_schema}.[{tableName}] WHERE 1 = 0";
                using (var reader = command.ExecuteReader(CommandBehavior.SchemaOnly))
                {
                    schema = reader.GetSchemaTable();
                }
            }
        }

        return schema;
    }

    public static string RemoveSpecialCharacters(string input)
{
    if (string.IsNullOrEmpty(input))
    {
        return input;
    }

    var stringBuilder = new StringBuilder();
    foreach (char c in input)
    {
        if (char.IsLetterOrDigit(c) || c == '_')
        {
            stringBuilder.Append(c);
        }
    }

    // If the first character is a digit, prepend an underscore
    if (char.IsDigit(stringBuilder[0]))
    {
        stringBuilder.Insert(0, '_');
    }

    return stringBuilder.ToString();
}


    private string GenerateModelCode(string tableName, DataTable columnSchema)
    {
        StringBuilder codeBuilder = new StringBuilder();

        codeBuilder.AppendLine("using System;");
        codeBuilder.AppendLine();
        codeBuilder.AppendLine($"public class {tableName}");
        codeBuilder.AppendLine("{");

        foreach (DataRow columnRow in columnSchema.Rows)
        {
            string columnName = columnRow["ColumnName"].ToString();
            Type columnType = (Type)columnRow["DataType"];
            bool allowNulls = (bool)columnRow["AllowDBNull"];

            if (allowNulls && columnType.IsValueType)
            {
                columnType = typeof(Nullable<>).MakeGenericType(columnType);
            }

            codeBuilder.AppendLine($"    public {GetClrTypeName(columnType.Name, allowNulls)} {RemoveSpecialCharacters(columnName.Replace(" ", "_"))} {{ get; set; }}");
        }

        codeBuilder.AppendLine("}");

        return codeBuilder.ToString();
    }

    private string GetClrTypeName(string dataType, bool isNullable)
    {
        string clrType = "object";

        switch (dataType.ToLower())
        {
            case "bigint":
                clrType = "long";
                break;
            case "binary":
            case "image":
            case "timestamp":
            case "varbinary":
                clrType = "byte[]";
                break;
            case "bit":
                clrType = "bool";
                break;
            case "char":
            case "nchar":
            case "nvarchar":
            case "varchar":
            case "text":
            case "nullable`1":
            case "string":
            case "ntext":
                clrType = "string";
                break;
            case "datetime":
            case "smalldatetime":
                clrType = "DateTime";
                break;
            case "decimal":
            case "money":
            case "numeric":
            case "smallmoney":
                clrType = "decimal";
                break;
            case "float":
                clrType = "double";
                break;
            case "int":
            case "int64":
                clrType = "int";
                break;
            case "real":
                clrType = "float";
                break;
            case "uniqueidentifier":
                clrType = "Guid";
                break;
            case "smallint":
                clrType = "short";
                break;
            case "tinyint":
                clrType = "byte";
                break;
            case "date":
                clrType = "DateTime";
                break;
            case "time":
                clrType = "TimeSpan";
                break;
            case "datetime2":
                clrType = "DateTime";
                break;
            case "datetimeoffset":
                clrType = "DateTimeOffset";
                break;
        }

        // Add '?' for nullable value types
        if (isNullable && clrType != "string" && clrType != "byte[]" && clrType != "object")
        {
            clrType += "?";
        }

        return clrType;
    }

}
