﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
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

namespace DatabaseSchemaReaderTest.DataSchema
{
    /// <summary>
    /// Summary description for CanSerializeTest
    /// </summary>
    [TestClass]
    public class CanSerializeTest
    {
        private static DatabaseReader GetNortwindReader()
        {
            const string providername = "System.Data.SqlClient";
            const string connectionString = @"Data Source=.\SQLEXPRESS;Integrated Security=true;Initial Catalog=Northwind";
            ProviderChecker.Check(providername, connectionString);

            var databaseReader = new DatabaseReader(connectionString, providername);
            return databaseReader;
        }

        [TestMethod]
        public void BinarySerializeTest()
        {
            var dbReader = GetNortwindReader();
            var schema = dbReader.ReadAll();

            var f = new BinaryFormatter();

            using (var stm = new FileStream("schema.bin", FileMode.Create))
            {
                f.Serialize(stm, schema);
            }

            DatabaseSchema clone;
            using (var stm = new FileStream("schema.bin", FileMode.Open))
            {
                clone = (DatabaseSchema)f.Deserialize(stm);
            }

            Assert.AreEqual(schema.DataTypes.Count, clone.DataTypes.Count);
            Assert.AreEqual(schema.StoredProcedures.Count, clone.StoredProcedures.Count);
            Assert.AreEqual(schema.Tables.Count, clone.Tables.Count);
            Assert.AreEqual(schema.Tables[0].Columns.Count, clone.Tables[0].Columns.Count);
        }


        [TestMethod]
        public void DataContractSerializeTest()
        {
            var dbReader = GetNortwindReader();
            var schema = dbReader.ReadAll();

            //XmlSerializer won't work because there are circular dependencies
            //var f = new XmlSerializer(schema.GetType());

            var f = new DataContractSerializer(schema.GetType(), "DatabaseSchema", "SchemaReader", new List<Type>(), 32767, false, true, null);

            using (var stm = new FileStream("schema.xml", FileMode.Create))
            {
                f.WriteObject(stm, schema);
            }

            DatabaseSchema clone;
            using (var stm = new FileStream("schema.xml", FileMode.Open))
            {
                clone = (DatabaseSchema)f.ReadObject(stm);
            }

            Assert.AreEqual(schema.DataTypes.Count, clone.DataTypes.Count);
            Assert.AreEqual(schema.StoredProcedures.Count, clone.StoredProcedures.Count);
            Assert.AreEqual(schema.Tables.Count, clone.Tables.Count);
            Assert.AreEqual(schema.Tables[0].Columns.Count, clone.Tables[0].Columns.Count);
        }

    }
}
