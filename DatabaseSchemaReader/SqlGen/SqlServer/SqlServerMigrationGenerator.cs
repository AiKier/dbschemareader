﻿using DatabaseSchemaReader.DataSchema;

namespace DatabaseSchemaReader.SqlGen.SqlServer
{
    class SqlServerMigrationGenerator : MigrationGenerator
    {
        public SqlServerMigrationGenerator() : base(SqlType.SqlServer)
        {
        }
        protected override string AlterColumnFormat
        {
            get { return "ALTER TABLE {0} ALTER COLUMN {1};"; }
        }
        public override string AddTrigger(DatabaseTable databaseTable, DatabaseTrigger trigger)
        {
            //sqlserver: 
            //CREATE TRIGGER (triggerName) 
            //ON (tableName) 
            //(FOR | AFTER | INSTEAD OF) ( [INSERT ] [ , ] [ UPDATE ] [ , ] [ DELETE ])
            //AS (sql_statement); GO 

            //nicely, SQLServer gives you the entire sql including create statement in TriggerBody
            if (string.IsNullOrEmpty(trigger.TriggerBody))
                return "-- add trigger " + trigger.Name;

            return trigger.TriggerBody + ";";
        }
    }
}
