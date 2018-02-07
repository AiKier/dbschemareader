﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using DatabaseSchemaReader.ProviderSchemaReaders.ConnectionContext;

namespace DatabaseSchemaReader.ProviderSchemaReaders.Databases
{

    abstract class SqlExecuter<T> : SqlExecuter where T : new()
    {

        protected List<T> Result { get; } = new List<T>();
    }

    /// <summary>
    /// Utility to execute ADO Sql
    /// </summary>
    public abstract class SqlExecuter
    {

        /// <summary>
        /// Gets or sets the SQL .
        /// </summary>
        public string Sql { get; set; }

        /// <summary>
        /// Gets or sets the schema owner.
        /// </summary>
        public string Owner { get; set; }

        /// <summary>
        /// Executes a database reader.
        /// </summary>
        /// <param name="connectionAdapter">The connection.</param>
        protected void ExecuteDbReader(IConnectionAdapter connectionAdapter)
        {
            Trace.WriteLine($"Sql: {Sql}");
            using (var cmd = BuildCommand(connectionAdapter))
            {
                cmd.CommandText = Sql;
                AddParameters(cmd);
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        Mapper(dr);
                    }
                }
            }
        }

        /// <summary>
        /// Builds the command.
        /// </summary>
        /// <param name="connectionAdapter">The connection adapter.</param>
        /// <returns></returns>
        protected DbCommand BuildCommand(IConnectionAdapter connectionAdapter)
        {
            var connection = connectionAdapter.DbConnection;
            if (connection.State == ConnectionState.Closed)
            {
                connection.Open();
            }
            var cmd = connection.CreateCommand();
            var transaction = connectionAdapter.DbTransaction;
            if (transaction != null)
            {
                cmd.Transaction = transaction;
            }
            return cmd;
        }

        /// <summary>
        /// Adds the database parameter (automatically handles null-> DbNull).
        /// </summary>
        /// <param name="command">The command.</param>
        /// <param name="parameterName">Name of the parameter.</param>
        /// <param name="value">The value.</param>
        /// <param name="dbType">Type of the database.</param>
        /// <returns></returns>
        protected static DbParameter AddDbParameter(DbCommand command, string parameterName, object value, DbType? dbType = null)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = parameterName;
            parameter.Value = value ?? DBNull.Value;
            if (dbType.HasValue) parameter.DbType = dbType.Value; //SqlServerCe needs this
            command.Parameters.Add(parameter);
            return parameter;
        }

        /// <summary>
        /// Add parameter(s).
        /// </summary>
        /// <param name="command">The command.</param>
        protected abstract void AddParameters(DbCommand command);

        /// <summary>
        /// Map the result ADO record to the result.
        /// </summary>
        /// <param name="record">The record.</param>
        protected abstract void Mapper(IDataRecord record);
    }
}
