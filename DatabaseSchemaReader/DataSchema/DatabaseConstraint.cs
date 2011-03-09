﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace DatabaseSchemaReader.DataSchema
{
    /// <summary>
    /// Represents a constraint (of <see cref="ConstraintType"/> such as primary key, foreign key...) that is attached to <see cref="Columns"/> of a table with name <see cref="TableName"/>
    /// </summary>
    [Serializable]
    public partial class DatabaseConstraint
    {
        #region Fields
        //backing fields
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly List<string> _columns;
        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="DatabaseConstraint"/> class.
        /// </summary>
        public DatabaseConstraint()
        {
            _columns = new List<string>();
        }

        public string Name { get; set; }

        public string TableName { get; set; }

        public string RefersToConstraint { get; set; }

        public string RefersToTable { get; set; }

        public string DeleteRule { get; set; }

        public ConstraintType ConstraintType { get; set; }

        /// <summary>
        /// Gets the columns. A check constraint has no columns.
        /// </summary>
        public List<string> Columns { get { return _columns; } }

        /// <summary>
        /// Gets or sets the expression (check constraints only).
        /// </summary>
        public string Expression { get; set; }

        public DatabaseTable ReferencedTable(DatabaseSchema schema)
        {
            if(schema == null) return null;

            return schema.Tables.FirstOrDefault(table =>
                //the string RefersToTable is the same
                table.Name.Equals(RefersToTable, StringComparison.OrdinalIgnoreCase) ||
                //or the RefersToConstraint is it's primary key
                (table.PrimaryKey != null && 
                table.PrimaryKey.Name.Equals(RefersToConstraint, StringComparison.OrdinalIgnoreCase)));
        }


        public override string ToString()
        {
            return Name + " on " + TableName;
        }

    }
}
