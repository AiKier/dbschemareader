﻿using System;
using System.IO;
using System.Linq;
using DatabaseSchemaReader;
using DatabaseSchemaReader.CodeGen;
using DatabaseSchemaReader.DataSchema;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DatabaseSchemaReaderTest.Codegen
{


    /// <summary>
    ///Create a simple model and write it to filesystem
    ///</summary>
    [TestClass]
    public class CodeWriterTest
    {
        private static DatabaseReader GetNortwindReader()
        {
            const string providername = "System.Data.SqlClient";
            const string connectionString = @"Data Source=.\SQLEXPRESS;Integrated Security=true;Initial Catalog=Northwind";

            return new DatabaseReader(connectionString, providername);
        }
        
        [TestMethod]
        public void NorthwindTest()
        {
            var dbReader = GetNortwindReader();
            var schema = dbReader.ReadAll();

            var directory = new DirectoryInfo(Environment.CurrentDirectory);
            const string @namespace = "Northwind.Domain";

            var codeWriter = new CodeWriter();
            codeWriter.Execute(schema, directory, @namespace);

            var files = directory.GetFiles("*.cs");

            var category = files.First(f => f.Name == "Category.cs");
            var cs = File.ReadAllText(category.FullName);

            StringAssert.Contains(cs, "public virtual IList<Product> ProductCollection { get; private set; }", "Should contain the collection of products");

            /*
             * When generated, create a startup project-
             *  Reference NHibernate and Castle
             *  Add App.Config with NHibernate configuration
             *  Run the NH config in app startup - for test projects use something like this...
        private static ISession Initialize()
        {
            var configuration = new Configuration();
            configuration.Configure(); //configure from the app.config
            //reference one of your domain classes here
            configuration.AddAssembly(typeof(Category).Assembly);
            var sessionFactory = configuration.BuildSessionFactory();

            return sessionFactory.OpenSession();
        }
             * 
             */
        }


        /// <summary>
        ///A test for Execute
        ///</summary>
        [TestMethod]
        public void ExecuteTest()
        {
            var target = new CodeWriter();
            DatabaseSchema schema = PrepareModel();

            var directory = new DirectoryInfo(Environment.CurrentDirectory);
            const string @namespace = "MyTest";

            target.Execute(schema, directory, @namespace);

            var files = directory.GetFiles("*.cs");
            Assert.AreEqual(2, files.Length);

            var category = files.First(f => f.Name == "Category.cs");
            var cs = File.ReadAllText(category.FullName);

            StringAssert.Contains(cs, "public virtual IList<Product> ProductCollection { get; private set; }", "Should contain the collection of products");
        }

        private static DatabaseSchema PrepareModel()
        {
            var schema = new DatabaseSchema();
            var integer = new DataType { NetDataType = typeof(int).FullName};
            var @string = new DataType { NetDataType = typeof(string).FullName };

            var categories = new DatabaseTable { Name = "Categories" };
            var categoryId = new DatabaseColumn { Name = "CategoryId", DataType = integer };
            var name = new DatabaseColumn { Name = "CategoryName", DataType = @string };
            categories.Columns.Add(categoryId);
            categories.Columns.Add(name);
            schema.Tables.Add(categories);

            var products = new DatabaseTable { Name = "Products" };
            var productId = new DatabaseColumn { Name = "ProductId", DataType = integer };
            var productName = new DatabaseColumn { Name = "ProductName", DataType = @string };
            var productCategory = new DatabaseColumn { Name = "CategoryId", DataType = integer, ForeignKeyTableName = "Categories", IsForeignKey = true };
            products.Columns.Add(productId);
            products.Columns.Add(productName);
            products.Columns.Add(productCategory);
            schema.Tables.Add(products);

            DatabaseSchemaFixer.UpdateReferences(schema);

            return schema;
        }
    }
}
