﻿using DatabaseSchemaReader.DataSchema;

namespace DatabaseSchemaReader.SqlGen
{
    /// <summary>
    /// Performs simple database schema migrations
    /// </summary>
    public interface IMigrationGenerator
    {
        /// <summary>
        /// Adds the table. If any primary key, unqiue or check constraints are attached, they are written too (don't write them individually). Foreign keys must be added separately (use <see cref="AddConstraint"/>)
        /// </summary>
        /// <param name="databaseTable">The database table.</param>
        /// <returns></returns>
        string AddTable(DatabaseTable databaseTable);
        /// <summary>
        /// Adds the column.
        /// </summary>
        /// <param name="databaseTable">The database table.</param>
        /// <param name="databaseColumn">The database column.</param>
        /// <returns></returns>
        string AddColumn(DatabaseTable databaseTable, DatabaseColumn databaseColumn);
        /// <summary>
        /// Alters the column.
        /// </summary>
        /// <param name="databaseTable">The database table.</param>
        /// <param name="databaseColumn">The database column.</param>
        /// <param name="originalColumn">The original column.</param>
        /// <returns></returns>
        string AlterColumn(DatabaseTable databaseTable, DatabaseColumn databaseColumn, DatabaseColumn originalColumn);
        /// <summary>
        /// Drops the column.
        /// </summary>
        /// <param name="databaseTable">The database table.</param>
        /// <param name="databaseColumn">The database column.</param>
        /// <returns></returns>
        string DropColumn(DatabaseTable databaseTable, DatabaseColumn databaseColumn);

        /// <summary>
        /// Drops the table.
        /// </summary>
        /// <param name="databaseTable">The database table.</param>
        /// <returns></returns>
        string DropTable(DatabaseTable databaseTable);

        /// <summary>
        /// Adds the constraint.
        /// </summary>
        /// <param name="databaseTable">The database table.</param>
        /// <param name="constraint">The constraint.</param>
        /// <returns></returns>
        string AddConstraint(DatabaseTable databaseTable, DatabaseConstraint constraint);

        /// <summary>
        /// Drops the constraint.
        /// </summary>
        /// <param name="databaseTable">The database table.</param>
        /// <param name="constraint">The constraint.</param>
        /// <returns></returns>
        string DropConstraint(DatabaseTable databaseTable, DatabaseConstraint constraint);

        /// <summary>
        /// Adds the view.
        /// </summary>
        /// <param name="view">The view.</param>
        /// <returns></returns>
        string AddView(DatabaseView view);

        /// <summary>
        /// Drops the view.
        /// </summary>
        /// <param name="view">The view.</param>
        /// <returns></returns>
        string DropView(DatabaseView view);

        /// <summary>
        /// Adds the procedure.
        /// </summary>
        /// <param name="procedure">The procedure.</param>
        /// <returns></returns>
        string AddProcedure(DatabaseStoredProcedure procedure);

        /// <summary>
        /// Drops the procedure.
        /// </summary>
        /// <param name="procedure">The procedure.</param>
        /// <returns></returns>
        string DropProcedure(DatabaseStoredProcedure procedure);

        /// <summary>
        /// Adds the index.
        /// </summary>
        /// <param name="databaseTable">The database table.</param>
        /// <param name="index">The index.</param>
        /// <returns></returns>
        string AddIndex(DatabaseTable databaseTable, DatabaseIndex index);

        /// <summary>
        /// Drops the index.
        /// </summary>
        /// <param name="databaseTable">The database table.</param>
        /// <param name="index">The index.</param>
        /// <returns></returns>
        string DropIndex(DatabaseTable databaseTable, DatabaseIndex index);

        /// <summary>
        /// Adds the trigger.
        /// </summary>
        /// <param name="databaseTable">The database table.</param>
        /// <param name="trigger">The trigger.</param>
        /// <returns></returns>
        string AddTrigger(DatabaseTable databaseTable, DatabaseTrigger trigger);

        /// <summary>
        /// Drops the trigger.
        /// </summary>
        /// <param name="trigger">The trigger.</param>
        /// <returns></returns>
        string DropTrigger(DatabaseTrigger trigger);

        /// <summary>
        /// Runs a batch of statements. May be needed before a script block.
        /// </summary>
        string RunStatements();
    }
}