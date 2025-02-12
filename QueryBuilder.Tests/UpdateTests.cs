﻿using System;
using System.Collections.Generic;
using SqlKata.Compilers;
using SqlKata.Tests.Infrastructure;
using Xunit;

namespace SqlKata.Tests
{
    public class UpdateTests : TestSupport
    {
        [Fact]
        public void UpdateObject()
        {
            Query query = new Query("Table").AsUpdate(new
            {
                Name = "The User",
                Age = new DateTime(2018, 1, 1),
            });

            IReadOnlyDictionary<string, string> c = Compile(query);

            Assert.Equal("UPDATE [Table] SET [Name] = 'The User', [Age] = '2018-01-01'", c[EngineCodes.SqlServer]);


            Assert.Equal("UPDATE \"TABLE\" SET \"NAME\" = 'The User', \"AGE\" = '2018-01-01'", c[EngineCodes.Firebird]);
        }

        [Fact]
        public void UpdateWithNullValues()
        {
            Query query = new Query("Books").Where("Id", 1).AsUpdate(
                new[] { "Author", "Date", "Version" },
                new object[] { "Author 1", null, null }
            );

            IReadOnlyDictionary<string, string> c = Compile(query);

            Assert.Equal("UPDATE [Books] SET [Author] = 'Author 1', [Date] = NULL, [Version] = NULL WHERE [Id] = 1",
                c[EngineCodes.SqlServer]);


            Assert.Equal(
                "UPDATE \"BOOKS\" SET \"AUTHOR\" = 'Author 1', \"DATE\" = NULL, \"VERSION\" = NULL WHERE \"ID\" = 1",
                c[EngineCodes.Firebird]);
        }

        [Fact]
        public void UpdateWithEmptyString()
        {
            Query query = new Query("Books").Where("Id", 1).AsUpdate(
                new[] { "Author", "Description" },
                new object[] { "Author 1", "" }
            );

            IReadOnlyDictionary<string, string> c = Compile(query);

            Assert.Equal("UPDATE [Books] SET [Author] = 'Author 1', [Description] = '' WHERE [Id] = 1", c[EngineCodes.SqlServer]);


            Assert.Equal("UPDATE \"BOOKS\" SET \"AUTHOR\" = 'Author 1', \"DESCRIPTION\" = '' WHERE \"ID\" = 1", c[EngineCodes.Firebird]);
        }

        [Fact]
        public void UpdateWithCte()
        {
            string now = DateTime.UtcNow.ToString("yyyy-MM-dd");

            Query query = new Query("Books")
                .With("OldBooks", q => q.From("Books").Where("Date", "<", now))
                .Where("Price", ">", 100)
                .AsUpdate(new Dictionary<string, object>
                {
                    {"Price", "150"}
                });

            IReadOnlyDictionary<string, string> c = Compile(query);

            Assert.Equal(
                $"WITH [OldBooks] AS (SELECT * FROM [Books] WHERE [Date] < '{now}')\nUPDATE [Books] SET [Price] = '150' WHERE [Price] > 100",
                c[EngineCodes.SqlServer]);
        }


        private class Book
        {
            public Book(string name, string author, decimal price = 1.0m, string color = null)
            {
                this.Name = name ?? throw new ArgumentNullException("name must be provided");
                this.BookPrice = price;
                this.color = color;
                this.BookAuthor = author;
            }

            public string Name { get; set; }
            [Column("Author")]
            public string BookAuthor { get; set; }
            [Column("Price")]
            public decimal BookPrice { get; set; }
            [Ignore]
            public string color { get; set; }
        }

        [Fact]
        public void UpdateWithIgnoreAndColumnProperties()
        {
            Book book = new Book(name: $"SqlKataBook", author: "Kata", color: $"red", price: 100m);
            Query query = new Query("Book").AsUpdate(book);

            IReadOnlyDictionary<string, string> c = Compile(query);

            Assert.Equal(
                "UPDATE [Book] SET [Name] = 'SqlKataBook', [Author] = 'Kata', [Price] = 100",
                c[EngineCodes.SqlServer]);


            Assert.Equal(
                "UPDATE \"BOOK\" SET \"NAME\" = 'SqlKataBook', \"AUTHOR\" = 'Kata', \"PRICE\" = 100",
                c[EngineCodes.Firebird]);
        }



        private class OrderProductComposite
        {
            public OrderProductComposite(string orderid, string productid, int quantity)
            {
                OrderId = orderid;
                ProductId = productid;
                Quantity = quantity;
                Foo = "baz";
            }

            [Key("OrdId")]
            public string OrderId { get; set; }

            [Key]
            public string ProductId { get; set; }

            public int Quantity { get; set; }

            [Column("Faa")]
            public string Foo { get; set; }
        }

        [Fact]
        public void UpdateWithKeyAttribute()
        {
            OrderProductComposite order = new OrderProductComposite("ORD01", "PROD02", 20);

            Query query = new Query("OrderProductComposite").AsUpdate(order);

            IReadOnlyDictionary<string, string> c = Compile(query);


            Assert.Equal(
                "UPDATE [OrderProductComposite] SET [OrdId] = 'ORD01', [ProductId] = 'PROD02', [Quantity] = 20, [Faa] = 'baz' WHERE [OrdId] = 'ORD01' AND [ProductId] = 'PROD02'",
                c[EngineCodes.SqlServer]);

            Assert.Equal(
              "UPDATE \"ORDERPRODUCTCOMPOSITE\" SET \"ORDID\" = 'ORD01', \"PRODUCTID\" = 'PROD02', \"QUANTITY\" = 20, \"FAA\" = 'baz' WHERE \"ORDID\" = 'ORD01' AND \"PRODUCTID\" = 'PROD02'",
              c[EngineCodes.Firebird]);
        }


        [Fact]
        public void UpdateFromRaw()
        {
            Query query = new Query().FromRaw("Table.With.Dots").AsUpdate(new
            {
                Name = "The User",
            });

            IReadOnlyDictionary<string, string> c = Compile(query);

            Assert.Equal(
                "UPDATE Table.With.Dots SET [Name] = 'The User'",
                c[EngineCodes.SqlServer]
            );
        }


        [Fact]
        public void UpdateFromQueryShouldFail()
        {
            Query query = new Query().From(new Query("InnerTable")).AsUpdate(new
            {
                Name = "The User",
            });

            Assert.Throws<InvalidOperationException>(() =>
            {
                Compile(query);
            });
        }

    }
}