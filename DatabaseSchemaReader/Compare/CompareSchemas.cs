﻿using System;
using System.Text;
using DatabaseSchemaReader.DataSchema;
using DatabaseSchemaReader.Utilities;

namespace DatabaseSchemaReader.Compare
{
    /// <summary>
    /// Compares two schemas, and returns a migration script.
    /// </summary>
    /// <remarks>
    /// Take care with sorting. 
    /// Tables are sorted with those without foreign keys first.
    /// We don't know the dependency order for views and procedures, so a view that depends on another view may be written first and the script will fail.
    /// </remarks>
    public class CompareSchemas
    {
        private readonly ComparisonWriter _writer;
        private readonly DatabaseSchema _baseSchema;
        private readonly DatabaseSchema _compareSchema;

        /// <summary>
        /// Initializes a new instance of the <see cref="CompareSchemas"/> class.
        /// </summary>
        /// <param name="baseSchema">The base schema.</param>
        /// <param name="compareSchema">The compare schema.</param>
        public CompareSchemas(DatabaseSchema baseSchema, DatabaseSchema compareSchema)
        {
            //argument null
            if (baseSchema == null) baseSchema = new DatabaseSchema(null, null);
            if (compareSchema == null) compareSchema = new DatabaseSchema(null, null);

            _compareSchema = compareSchema;
            _baseSchema = baseSchema;

            SqlType sqlType = FindSqlType(compareSchema) ?? FindSqlType(baseSchema) ?? SqlType.SqlServer;

            _writer = new ComparisonWriter(sqlType);
        }


        private SqlType? FindSqlType(DatabaseSchema databaseSchema)
        {
            var providerName = databaseSchema.Provider;
            if (string.IsNullOrEmpty(providerName)) return null;
            if (providerName.IndexOf("Oracle", StringComparison.OrdinalIgnoreCase) != -1)
            {
                return SqlType.Oracle;
            }
            if (providerName.IndexOf("MySql", StringComparison.OrdinalIgnoreCase) != -1)
            {
                return SqlType.MySql;
            }
            if (providerName.IndexOf("Sqlite", StringComparison.OrdinalIgnoreCase) != -1)
            {
                return SqlType.SQLite;
            }
            if (providerName.IndexOf("SqlServerCe.4.0", StringComparison.OrdinalIgnoreCase) != -1)
            {
                return SqlType.SqlServerCe;
            }
            return SqlType.SqlServer;
        }
        /// <summary>
        /// Run the comparison.
        /// </summary>
        /// <returns></returns>
        public string Execute()
        {
            var sb = new StringBuilder();

            var compareTables = new CompareTables(sb, _writer);
            //make sure they are in topological order- if 2 tables are added, the first must not have a foreign key to the second...
            var comparedTables = SchemaTablesSorter.TopologicalSort(_compareSchema);
            compareTables.Execute(_baseSchema.Tables, comparedTables);

            //compare views
            var compareViews = new CompareViews(sb, _writer);
            compareViews.Execute(_baseSchema.Views, _compareSchema.Views);

            //compare stored procedures and functions
            var compareProcedures = new CompareProcedures(sb, _writer);
            compareProcedures.Execute(_baseSchema.StoredProcedures,_compareSchema.StoredProcedures);

            //compare packages

            //compare sequences

            return sb.ToString();
        }
    }
}
