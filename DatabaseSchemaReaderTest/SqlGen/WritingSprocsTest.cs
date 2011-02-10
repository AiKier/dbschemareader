﻿using System;
using System.IO;
using System.Linq;
using DatabaseSchemaReader;
using DatabaseSchemaReader.DataSchema;
using DatabaseSchemaReaderTest.IntegrationTests;
#if !NUNIT
using Microsoft.VisualStudio.TestTools.UnitTesting;
#else
using NUnit.Framework;
using TestClass = NUnit.Framework.TestFixtureAttribute;
using TestMethod = NUnit.Framework.TestAttribute;
using TestInitialize = NUnit.Framework.SetUpAttribute;
using TestCleanup = NUnit.Framework.TearDownAttribute;
using TestContext = System.Object;
#endif

namespace DatabaseSchemaReaderTest.SqlGen
{
    /// <summary>
    /// Take a table and write CRUD sprocs
    /// </summary>
    [TestClass]
    public class WritingSprocsTest
    {

        private static DatabaseTable LoadCategoriesFromNorthwind()
        {
            const string providername = "System.Data.SqlClient";
            const string connectionString = @"Data Source=.\SQLEXPRESS;Integrated Security=true;Initial Catalog=Northwind";
            ProviderChecker.Check(providername, connectionString);

            var dbReader = new DatabaseReader(connectionString, providername);
            var schema = dbReader.ReadAll();
            return schema.FindTableByName("Categories");
        }
        private static DirectoryInfo CreateDirectory(string folder)
        {
            var directory = new DirectoryInfo(Environment.CurrentDirectory);
            if (directory.GetDirectories(folder).Any())
            {
                return directory.GetDirectories(folder).First();
            }
            return directory.CreateSubdirectory(folder);
        }

        [TestMethod]
        public void TestWritingOracleTables()
        {
            var table = LoadCategoriesFromNorthwind();

            //take a SQLServer table and create Oracle table DDL
            var gen = new DatabaseSchemaReader.SqlGen.Oracle.TableGenerator(table);

            var destination = CreateDirectory("sql").FullName;
            gen.WriteToFolder(destination);

            var path = Directory.GetFiles(destination, table.Name + "*").FirstOrDefault();
            Assert.IsNotNull(path);
            var txt = File.ReadAllText(path);
            Assert.IsFalse(string.IsNullOrEmpty(txt), "Should have written some text");

            //manually check the script is ok
        }

        [TestMethod]
        public void TestWritingCrudSprocs()
        {
            var table = LoadCategoriesFromNorthwind();

            //let's create the SQLServer crud procedures
            var gen = new DatabaseSchemaReader.SqlGen.SqlServer.ProcedureGenerator(table);
            gen.ManualPrefix = table.Name + "__";
            var destination = CreateDirectory("sql").FullName;
            var path = Path.Combine(destination, "sqlserver_sprocs.sql");
            gen.WriteToScript(path);

            var txt = File.ReadAllText(path);
            Assert.IsFalse(string.IsNullOrEmpty(txt), "Should have written some text");
            //manually check the script is ok
        }



        [TestMethod]
        public void TestWritingCrudSprocsWithOracleConversion()
        {
            var table = LoadCategoriesFromNorthwind();

            //let's pretend it's an oracle table and create an oracle package
            var oracleGen = new DatabaseSchemaReader.SqlGen.Oracle.ProcedureGenerator(table);
            oracleGen.ManualPrefix = table.Name + "__";
            //here i want all my parameters prefixed by a p
            oracleGen.FormatParameter = name => "p_" + name;
            //also define the cursor parameter
            oracleGen.CursorParameterName = "p_cursor";
            var destination = CreateDirectory("sql").FullName;
            var oraclePath = Path.Combine(destination, "oracle_sprocs.sql");
            oracleGen.WriteToScript(oraclePath);

            var txt = File.ReadAllText(oraclePath);
            Assert.IsFalse(string.IsNullOrEmpty(txt), "Should have written some text");
            //manually check the script is ok
        }
    }
}
