﻿using System;
using System.Data;
using System.Data.Common;
using System.Data.SqlTypes;
using System.Globalization;

namespace DatabaseSchemaReader
{
    /// <summary>
    /// Simple access to database schemas in ADO.Net 2.0. 
    /// </summary>
    /// <remarks>
    /// Works for SqlServer, Oracle, OLEDB, ODBC, MySql and SQLite. Does not work for System.Data.SqlServerCe.3.5 which throws NotSupportedException.
    /// </remarks>
    /// <example>
    /// Form Load:
    ///    DataTable dt = DbProviderFactories.GetFactoryClasses();
    ///    ProviderName.DataSource = dt;
    ///    ProviderName.DisplayMember = "InvariantName";
    ///
    /// After picked a provider from above list:
    ///    SchemaReader schema = new SchemaReader(ConnectionString.Text, "System.Data.OracleClient");
    ///    schema.ProviderName = ProviderName.Text;
    ///    dataGrid1.DataSource = schema.Tables(); //a list of all tables
    ///    dataGrid1.DataSource = schema.Columns("MYTABLENAME"); //a list of columns for a specific table
    ///</example>
    public class SchemaReader : IDisposable
    {
        //#region public static methods
        ///// <summary>
        ///// List of all the valid Providers. Use the ProviderInvariantName to fill ProviderName property
        ///// </summary>
        ///// <returns></returns>
        //public static DataTable Providers()
        //{
        //    return DbProviderFactories.GetFactoryClasses();
        //}
        //#endregion

        protected readonly string ConnectionString;
        protected readonly DbProviderFactory Factory;
        protected readonly string ProviderName;
        private DataTable _metadata;
        private SchemaRestrictions _restrictions;


        /// <summary>
        /// Constructor with connectionString and ProviderName
        /// </summary>
        /// <param name="connectionString">Eg "Data Source=localhost;Integrated Security=SSPI;Initial Catalog=Northwind;"</param>
        /// <param name="providerName">ProviderInvariantName for the provider (eg System.Data.SqlClient or System.Data.OracleClient)</param>
        public SchemaReader(string connectionString, string providerName)
        {
            if (String.IsNullOrEmpty(connectionString))
                throw new ArgumentException("connectionString must not be empty");

            if (String.IsNullOrEmpty(providerName))
                throw new ArgumentException("providerName must not be empty");

            ConnectionString = connectionString;
            ProviderName = providerName;
            Factory = DbProviderFactories.GetFactory(ProviderName);
        }

        /// <summary>
        /// There are a number of special-cases for Oracle, so we check the provider string
        /// </summary>
        internal bool IsOracle
        {
            get
            {
                //System.Data.OracleClient and Oracle.DataAccess.Client
                return (ProviderName.IndexOf("Oracle", StringComparison.OrdinalIgnoreCase) != -1);
                //System.Data.OleDb could be using Provider=msdaora (or Provider=OraOLEDB.Oracle) but the schema collections are more limited
            }
        }
        internal bool IsMySql
        {
            get
            {
                //MySql
                return (ProviderName.Equals("MySql.Data.MySqlClient", StringComparison.OrdinalIgnoreCase));
            }
        }

        /// <summary>
        /// Gets or sets the owner (for Oracle) /schema (for SqlServer) / database (MySql). Always set it with Oracle; if you use other than dbo in SqlServer you should also set it. 
        /// If it is null or empty, all owners are returned.
        /// </summary>
        public string Owner { get; set; }


        /// <summary>
        /// DataTable of all users
        /// </summary>
        /// <returns>Datatable with columns NAME, ID, CREATEDDATE</returns>
        public DataTable Users()
        {
            const string collection = "Users";
            using (DbConnection conn = Factory.CreateConnection())
            {
                conn.ConnectionString = ConnectionString;
                conn.Open();
                if (!SchemaCollectionExists(conn, collection))
                    return new DataTable(collection);
                return conn.GetSchema(collection);
            }
        }

        /// <summary>
        /// DataTable of all tables for a specific owner
        /// </summary>
        public DataTable Tables()
        {
            const string collectionName = "Tables";
            using (DbConnection conn = Factory.CreateConnection())
            {
                conn.ConnectionString = ConnectionString;
                conn.Open();
                string[] restrictions = SchemaRestrictions.ForOwner(conn, collectionName);
                return conn.GetSchema(collectionName, restrictions);
            }
        }

        /// <summary>
        /// Get all data for a specified table name.
        /// </summary>
        /// <param name="tableName">Name of the table. Oracle names can be case sensitive.</param>
        /// <returns>A dataset containing the tables: Columns, Indexes, IndexColumns</returns>
        public virtual DataSet Table(string tableName)
        {
            var ds = new DataSet();
            using (DbConnection conn = Factory.CreateConnection())
            {
                conn.ConnectionString = ConnectionString;
                conn.Open();

                LoadTable(tableName, ds, conn);
            }
            return ds;
        }

        /// <summary>
        /// Loads the table COLUMNS, INDEXES and INDEXCOLUMNS tables into a dataset.
        /// </summary>
        /// <param name="tableName">Name of the table.</param>
        /// <param name="ds">The dataset.</param>
        /// <param name="connection">The connection.</param>
        protected void LoadTable(string tableName, DataSet ds, DbConnection connection)
        {
            DataTable cols = Columns(tableName, connection);
            if (cols.Rows.Count == 0) return; //no columns found
            ds.Tables.Add(cols);

            string[] indexRestrictions = SchemaRestrictions.ForTable(connection, "Indexes", tableName);
            ds.Tables.Add(connection.GetSchema("Indexes", indexRestrictions));

            const string collectionName = "IndexColumns";
            if (SchemaCollectionExists(connection, collectionName))
            {
                string[] indexColRestrictions = SchemaRestrictions.ForTable(connection, collectionName, tableName);
                ds.Tables.Add(connection.GetSchema(collectionName, indexColRestrictions));
            }
        }

        /// <summary>
        /// DataTable of all tables for a specific owner
        /// </summary>
        /// <returns>Datatable with columns OWNER, TABLE_NAME, TYPE</returns>
        public DataTable Views()
        {
            const string collectionName = "Views";
            using (DbConnection conn = Factory.CreateConnection())
            {
                conn.ConnectionString = ConnectionString;
                conn.Open();
                if (!SchemaCollectionExists(conn, collectionName))
                    return new DataTable(collectionName); //doesn't exist in SqlServerCe
                string[] restrictions = SchemaRestrictions.ForOwner(conn, collectionName);
                return conn.GetSchema(collectionName, restrictions);
            }
        }

        /// <summary>
        /// All the columns for a specific table
        /// </summary>
        /// <param name="tableName">Name of the table. Oracle names can be case sensitive.</param>
        /// <returns>DataTable columns incl. COLUMN_NAME, DATATYPE, LENGTH, PRECISION, SCALE, NULLABLE</returns>
        public virtual DataTable Columns(string tableName)
        {
            using (DbConnection conn = Factory.CreateConnection())
            {
                conn.ConnectionString = ConnectionString;
                conn.Open();

                return Columns(tableName, conn);
            }
        }

        /// <summary>
        /// Get the columns using GetSchema. Override to get additional stuff from Oracle.
        /// </summary>
        protected virtual DataTable Columns(string tableName, DbConnection connection)
        {
            string[] restrictions = SchemaRestrictions.ForTable(connection, "Columns", tableName);
            return connection.GetSchema("Columns", restrictions);
        }

        /// <summary>
        /// Gets the indexes. 
        /// </summary>
        /// <param name="tableName">Name of the table (or null for all tables).</param>
        /// <returns></returns>
        public DataTable Indexes(string tableName)
        {
            using (DbConnection conn = Factory.CreateConnection())
            {
                conn.ConnectionString = ConnectionString;
                conn.Open();
                const string collectionName = "Indexes";
                if (!SchemaCollectionExists(conn, collectionName))
                {
                        return new DataTable(collectionName);
                }

                return RunGetSchema(conn, collectionName, tableName);
            }
        }

        private DataTable RunGetSchema(DbConnection conn, string collectionName, string tableName)
        {
            string[] restrictions = SchemaRestrictions.ForTable(conn, collectionName, tableName);
            try
            {
                return conn.GetSchema(collectionName, restrictions);
            }
            catch (DbException exception)
            {
                //Postgresql throws this nasty error with a restriction. We'll carry on.
                Console.WriteLine("Provider returned error for " + collectionName + ": " + exception.Message);
                return new DataTable(collectionName);
            }
            catch (SqlNullValueException exception)
            {
                //MySQL can't run this without a table (it does a SHOW INDEX FROM table so you get the above error)
                Console.WriteLine("Provider returned error for " + collectionName + ": " + exception.Message);
                return new DataTable(collectionName);
            }
        }


        /// <summary>
        /// Gets the indexed columns.
        /// </summary>
        /// <param name="tableName">Name of the table.</param>
        /// <returns></returns>
        public DataTable IndexColumns(string tableName)
        {
            using (DbConnection conn = Factory.CreateConnection())
            {
                conn.ConnectionString = ConnectionString;
                conn.Open();
                string collectionName = "IndexColumns";
                if (!SchemaCollectionExists(conn, collectionName))
                {
                    collectionName = "Indexes";
                    if (!SchemaCollectionExists(conn, collectionName))
                        return new DataTable(collectionName);
                }

                return RunGetSchema(conn, collectionName, tableName);
            }
        }

        /// <summary>
        /// Gets the primary keys
        /// </summary>
        /// <param name="tableName">Name of the table.</param>
        /// <returns></returns>
        public virtual DataTable PrimaryKeys(string tableName)
        {
            using (DbConnection conn = Factory.CreateConnection())
            {
                conn.ConnectionString = ConnectionString;
                conn.Open();
                const string collectionName = "PrimaryKeys";
                if (!SchemaCollectionExists(conn, collectionName))
                    return new DataTable(collectionName);

                string[] restrictions = SchemaRestrictions.ForTable(conn, collectionName, tableName);
                return conn.GetSchema(collectionName, restrictions);
            }
        }

        /// <summary>
        /// Finds the foreign keys. Usually just fk name and the table, not the columns (Oracle has ForeignKeyColumns). SqlServer doesn't even have the referenced table/ unique constraint. 
        /// </summary>
        public virtual DataTable ForeignKeys(string tableName)
        {
            using (DbConnection conn = Factory.CreateConnection())
            {
                conn.ConnectionString = ConnectionString;
                conn.Open();
                string collectionName = "Foreign Keys";
                if (!SchemaCollectionExists(conn, collectionName))
                {
                    collectionName = "ForeignKeys";
                    if (!SchemaCollectionExists(conn, collectionName))
                        return new DataTable(collectionName);
                }

                string[] restrictions = SchemaRestrictions.ForTable(conn, collectionName, tableName);
                return conn.GetSchema(collectionName, restrictions);
            }
        }

        /// <summary>
        /// Finds the foreign key columns. SqlServer doesn't have this collection.
        /// </summary>
        /// <param name="tableName">Name of the table.</param>
        public virtual DataTable ForeignKeyColumns(string tableName)
        {
            using (DbConnection conn = Factory.CreateConnection())
            {
                conn.ConnectionString = ConnectionString;
                conn.Open();
                const string collectionName = "ForeignKeyColumns";
                if (!SchemaCollectionExists(conn, collectionName))
                    return new DataTable(collectionName);

                string[] restrictions = SchemaRestrictions.ForTable(conn, collectionName, tableName);
                return conn.GetSchema(collectionName, restrictions);
            }
        }

        /// <summary>
        /// Gets the sequences (if supported, eg Oracle)
        /// </summary>
        /// <returns></returns>
        public DataTable Sequences()
        {
            const string name = "Sequences";
            if (!IsOracle) return new DataTable(name);

            using (DbConnection conn = Factory.CreateConnection())
            {
                conn.ConnectionString = ConnectionString;
                conn.Open();
                string[] restrictions = SchemaRestrictions.ForOwner(conn, name);
                return conn.GetSchema(name, restrictions);
            }
        }

        protected DataTable GenericCollection(string collectionName, DbConnection connection, string tableName)
        {
            if (SchemaCollectionExists(connection, collectionName))
                return connection.GetSchema(collectionName, SchemaRestrictions.ForTable(connection, collectionName, tableName));
            DataTable dt = new DataTable(collectionName);
            dt.Locale = CultureInfo.InvariantCulture;
            return dt;
        }

        #region Sprocs
        /// <summary>
        /// Get all the functions (always empty except for Oracle, as the others mix stored procedures and functions).
        /// </summary>
        /// <returns></returns>
        public virtual DataTable Functions()
        {
            const string collectionName = "Functions";
            //if (!IsOracle) return new DataTable(collectionName); //in sql server, functions are in the sprocs collection.

            using (DbConnection conn = Factory.CreateConnection())
            {
                conn.ConnectionString = ConnectionString;
                conn.Open();
                if (!SchemaCollectionExists(conn, collectionName))
                    return new DataTable(collectionName);
                string[] restrictions = SchemaRestrictions.ForOwner(conn, collectionName);
                return conn.GetSchema(collectionName, restrictions);
            }
        }
        /// <summary>
        /// Get all the stored procedures (owner required for Oracle- otherwise null).
        /// NB: in oracle does not get stored procedures in packages
        /// </summary>
        public DataTable StoredProcedures()
        {
            using (DbConnection conn = Factory.CreateConnection())
            {
                conn.ConnectionString = ConnectionString;
                conn.Open();
                const string collectionName = "Procedures";
                if (!SchemaCollectionExists(conn, collectionName)) return new DataTable(collectionName);
                string[] restrictions = SchemaRestrictions.ForOwner(conn, collectionName);
                return conn.GetSchema(collectionName, restrictions);
            }
        }
        /// <summary>
        /// Get all the arguments for a stored procedures (or all sprocs)
        /// NB: in oracle we get arguments for sprocs in packages. This is slow.
        /// </summary>
        public DataTable StoredProcedureArguments(string storedProcedureName)
        {
            using (DbConnection conn = Factory.CreateConnection())
            {
                conn.ConnectionString = ConnectionString;
                conn.Open();
                //different collections here- we could just if(IsOracle)
                string collectionName = "ProcedureParameters";
                if (IsMySql) collectionName = "Procedure Parameters";
                else if (IsOracle) collectionName = "Arguments"; //Oracle, assume packages
                if (!SchemaCollectionExists(conn, collectionName)) return new DataTable(collectionName);

                string[] restrictions = SchemaRestrictions.ForRoutine(conn, collectionName, storedProcedureName);
                return conn.GetSchema(collectionName, restrictions);
            }
        }
        /// <summary>
        /// Get all the arguments for a package (or all packs)
        /// Package is only for Oracle - for SqlServer it's all sprocs
        /// </summary>
        public DataTable PackageStoredProcedureArguments(string packageName)
        {
            using (DbConnection conn = Factory.CreateConnection())
            {
                conn.ConnectionString = ConnectionString;
                conn.Open();
                //for SqlServer the restriction doesn't apply
                string collectionName = "ProcedureParameters";
                if (IsOracle)
                    collectionName = "Arguments"; //Oracle, we assume you mean packages
                if (!SchemaCollectionExists(conn, collectionName)) return new DataTable();

                string[] restrictions = SchemaRestrictions.ForSpecific(conn, collectionName, packageName, "PACKAGENAME");
                return conn.GetSchema(collectionName, restrictions);
            }
        }
        /// <summary>
        /// Get all the packages (Oracle only concept- returns empty DataTable for others)
        /// </summary>
        public DataTable Packages()
        {
            using (DbConnection conn = Factory.CreateConnection())
            {
                conn.ConnectionString = ConnectionString;
                conn.Open();
                const string collectionName = "Packages";
                if (!SchemaCollectionExists(conn, collectionName)) return new DataTable();
                string[] restrictions = SchemaRestrictions.ForOwner(conn, collectionName);
                return conn.GetSchema(collectionName, restrictions);
            }
        }

        #endregion

        private SchemaRestrictions SchemaRestrictions
        {
            get
            {
                if (_restrictions == null)
                    _restrictions = new SchemaRestrictions(Owner);
                return _restrictions;
            }
        }

        #region MetadataCollections

        /// <summary>
        /// check is a schema collection exists.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="name">The name.</param>
        /// <returns></returns>
        protected bool SchemaCollectionExists(DbConnection connection, string name)
        {
            if (_metadata == null)
                _metadata = MetadataCollections(connection);
            return (_metadata.Select(string.Format(CultureInfo.InvariantCulture, "[CollectionName] = '{0}'", name)).Length != 0);
        }

        /// <summary>
        /// All the collections that are available via GetSchema
        /// </summary>
        public DataTable MetadataCollections()
        {
            using (DbConnection conn = Factory.CreateConnection())
            {
                conn.ConnectionString = ConnectionString;
                conn.Open();
                _metadata = MetadataCollections(conn);
                return _metadata;
            }
        }

        private static DataTable MetadataCollections(DbConnection connection)
        {
            return connection.GetSchema(DbMetaDataCollectionNames.MetaDataCollections);
        }

        /// <summary>
        /// All the Datatypes in the database and the mappings to .Net types
        /// </summary>
        /// <returns>DataTable with columns incl. TYPENAME, DataType (.net)</returns>
        public DataTable DataTypes()
        {
            using (DbConnection conn = Factory.CreateConnection())
            {
                conn.ConnectionString = ConnectionString;
                conn.Open();
                try
                {
                    return conn.GetSchema(DbMetaDataCollectionNames.DataTypes);
                }
                catch (NotSupportedException)
                {
                    //Npgsql doesn't have the collection and throws this exception
                    return new DataTable("DataTypes");
                }
            }
        }
        #endregion

        #region Implementation of IDisposable

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        /// <summary>
        /// Releases unmanaged and - optionally - managed resources
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // free managed resources
                if (_restrictions != null)
                {
                    _restrictions.Dispose();
                    _restrictions = null;
                }
            }
        }

        #endregion
    }
}