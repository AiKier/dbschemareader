﻿using System.Data.Common;
using System.Globalization;
using System.Linq;
using DatabaseSchemaReader.DataSchema;

namespace CopyToSQLite
{
    class SqlServerInserter : DatabaseInserter
    {
        private readonly DatabaseTable _table;
        private readonly bool _hasIdentity;

        public SqlServerInserter(DbConnection connection, string insertSql, DatabaseTable table)
            : base(connection, insertSql)
        {
            _table = table;
            if (_table.HasIdentityColumn)
            {
                _hasIdentity = true;
                SetIdentityInsertOn();
            }
            if (_table.ForeignKeys.Any(fk => fk.RefersToTable == _table.Name))
            {
                //a self referencing table (eg Employees in Northwind).
                //In full SQLServer we could turn off constraints (ALTER TABLE [Employees] NOCHECK CONSTRAINT ALL)
                //but we can't do that in CE.
                foreach (var foreignKey in _table.ForeignKeys)
                {
                    if(foreignKey.RefersToTable == _table.Name)
                        DropConstraint(foreignKey.Name);
                }
            }
        }

        protected override void CompleteTable()
        {
            if (_hasIdentity)
            {
                SetIdentityInsertOff();
            }
            //rewrite the self referencing foreign keys
            if (_table.ForeignKeys.Any(fk => fk.RefersToTable == _table.Name))
            {
                foreach (var foreignKey in _table.ForeignKeys)
                {
                    if (foreignKey.RefersToTable == _table.Name)
                        AddConstraint(foreignKey);
                }
            }
        }

        private void DropConstraint(string constraintName)
        {
            var command = Connection.CreateCommand();
            command.CommandText = string.Format(CultureInfo.InvariantCulture, 
                "ALTER TABLE [{0}] DROP CONSTRAINT [{1}]", _table.Name, constraintName);
            command.ExecuteNonQuery();
        }
        private void AddConstraint(DatabaseConstraint foreignKey)
        {
            var command = Connection.CreateCommand();
            var cols = string.Join(", ", foreignKey.Columns.ToArray());
            var refPrimaryKey = _table.PrimaryKey;
            var refcols = string.Join(", ", refPrimaryKey.Columns.ToArray());
            command.CommandText = string.Format(CultureInfo.InvariantCulture,
                "ALTER TABLE [{0}] ADD CONSTRAINT [{1}] FOREIGN KEY ({2}) REFERENCES [{0}]({3})", 
                _table.Name, 
                foreignKey.Name,
                cols,
                refcols);
            command.ExecuteNonQuery();
        }
        private void SetIdentityInsertOn()
        {
            var command = Connection.CreateCommand();
            command.CommandText = string.Format(CultureInfo.InvariantCulture, 
                "SET IDENTITY_INSERT [{0}] ON", _table.Name);
            command.ExecuteNonQuery();
        }
        private void SetIdentityInsertOff()
        {
            var command = Connection.CreateCommand();
            command.CommandText = string.Format(CultureInfo.InvariantCulture, 
                "SET IDENTITY_INSERT [{0}] OFF",
                _table.Name);
            command.ExecuteNonQuery();
        }
    }
}
