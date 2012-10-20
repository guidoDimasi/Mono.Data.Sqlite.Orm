using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Transactions;

using Mono.Data.Sqlite.Orm.ComponentModel;
#if SILVERLIGHT 
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestFixtureAttribute = Microsoft.VisualStudio.TestTools.UnitTesting.TestClassAttribute;
using TestAttribute = Microsoft.VisualStudio.TestTools.UnitTesting.TestMethodAttribute;
#elif NETFX_CORE
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using TestFixtureAttribute = Microsoft.VisualStudio.TestPlatform.UnitTestFramework.TestClassAttribute;
using TestAttribute = Microsoft.VisualStudio.TestPlatform.UnitTestFramework.TestMethodAttribute;
#else
using NUnit.Framework;

#endif

#if WINDOWS_PHONE
using Community.CsharpSqlite.SQLiteClient;
#endif

namespace Mono.Data.Sqlite.Orm.Tests
{
    [TestFixture]
    public class InsertTest
    {
        public class TestObj
        {
            [AutoIncrement]
            [PrimaryKey]
            public int Id { get; set; }

            public String Text { get; set; }

            public override string ToString()
            {
                return string.Format("[TestObj: Id={0}, Text={1}]", Id, Text);
            }
        }

        public class TestObjPlain
        {
            public string Text { get; set; }
        }

        public class TestObj2
        {
            [PrimaryKey]
            public int Id { get; set; }

            public String Text { get; set; }

            public override string ToString()
            {
                return string.Format("[TestObj: Id={0}, Text={1}]", Id, Text);
            }
        }

        public class TestObjDateTime
        {
            [PrimaryKey]
            [AutoIncrement]
            public int Id { get; set; }

            public Guid Guid { get; set; }

            public DateTime TheDate { get; set; }

            public override string ToString()
            {
                return string.Format("[TestObj: Id={0}, TheDate={1}]", Id, TheDate);
            }
        }

        public class DefaultsObject
        {
            [PrimaryKey]
            [AutoIncrement]
            public int Id { get; set; }

            [Default("33")]
            public int Number { get; set; }

            [Default("'Default Text'")]
            public string Text { get; set; }
        }

        [Test]
        public void InsertUsingSystemTransactions()
        {
            var options = new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted };
#if NETFX_CORE
            var tempFileName = Windows.Storage.ApplicationData.Current.TemporaryFolder.Path + "\\TempDb" + DateTime.Now.Ticks + ".db";
#else
            var tempFileName = Path.GetTempFileName();

#endif
            SqliteSession.Trace = true;

            using (var db = new SqliteSession("Data Source=" + tempFileName + ";DefaultTimeout=100", false))
            {
                db.Connection.Open();
                db.CreateTable<TestObj>();
                db.Connection.Close();

                using (var trans = new TransactionScope(TransactionScopeOption.Required, options))
                {
                    db.Connection.Open();
                    db.Insert(new TestObj { Text = "My Text" });
                }

                Assert.AreEqual(0, db.Table<TestObj>().Count());

                db.Connection.Close();

                using (var trans = new TransactionScope(TransactionScopeOption.Required, options))
                {
                    db.Connection.Open();
                    db.Insert(new TestObj { Text = "My Text" });
                    trans.Complete();
                }

                Assert.AreEqual(1, db.Table<TestObj>().Count());
            }
        }

        [Test]
        public void InsertALotPlain()
        {
            var db = new OrmTestSession();

            SqliteSession.Trace = false;

            db.CreateTable<TestObjPlain>();

            const int n = 10000;
            IEnumerable<TestObjPlain> q = Enumerable.Range(1, n).Select(i => new TestObjPlain { Text = "I am" });
            TestObjPlain[] objs = q.ToArray();
            int numIn = db.InsertAll(objs);

            Assert.AreEqual(numIn, n, "Num inserted must = num objects");

            TestObjPlain[] inObjs = db.Table<TestObjPlain>().ToArray();

            foreach (TestObjPlain t in inObjs)
            {
                Assert.AreEqual("I am", t.Text);
            }

            int numCount = db.Table<TestObjPlain>().Count();

            Assert.AreEqual(numCount, n, "Num counted must = num objects");

            db.Close();
        }

        [Test]
        public void InsertALot()
        {
            var db = new OrmTestSession();

            SqliteSession.Trace = false;

            db.CreateTable<TestObj>();

            const int n = 10000;
            IEnumerable<TestObj> q = Enumerable.Range(1, n).Select(i => new TestObj { Text = "I am" });
            TestObj[] objs = q.ToArray();
            int numIn = db.InsertAll(objs);

            Assert.AreEqual(numIn, n, "Num inserted must = num objects");

            TestObj[] inObjs = db.Table<TestObj>().ToArray();

            for (int i = 0; i < inObjs.Length; i++)
            {
                Assert.AreEqual(i + 1, objs[i].Id);
                Assert.AreEqual(i + 1, inObjs[i].Id);
                Assert.AreEqual("I am", inObjs[i].Text);
            }

            int numCount = db.Table<TestObj>().Count();

            Assert.AreEqual(numCount, n, "Num counted must = num objects");

            db.Close();
        }

        [Test]
        public void InsertDateTime()
        {
            var db = new OrmTestSession();
            db.CreateTable<TestObjDateTime>();

            DateTime date = DateTime.Now;
            var obj1 = new TestObjDateTime { TheDate = date };

            int numIn1 = db.Insert(obj1);
            Assert.AreEqual(1, numIn1);

            TestObjDateTime result = db.Table<TestObjDateTime>().First();
            Assert.AreEqual(date, result.TheDate);
            Assert.AreEqual(obj1.TheDate, result.TheDate);

            db.Close();
        }

        [Test]
        public void InsertGuid()
        {
            var db = new OrmTestSession();
            db.CreateTable<TestObjDateTime>();

            var obj2 = new TestObjDateTime { Guid = Guid.NewGuid() };

            int numIn1 = db.Insert(obj2);
            Assert.AreEqual(1, numIn1);

            TestObjDateTime result = db.Table<TestObjDateTime>().First(i => i.Id == obj2.Id);
            Assert.AreEqual(obj2.Guid, result.Guid);

            db.Close();
        }

        [Test]
        public void InsertIntoTwoTables()
        {
            var db = new OrmTestSession();
            db.CreateTable<TestObj>();
            db.CreateTable<TestObj2>();

            var obj1 = new TestObj { Text = "GLaDOS loves testing!" };
            var obj2 = new TestObj2 { Text = "Keep testing, just keep testing" };

            int numIn1 = db.Insert(obj1);
            Assert.AreEqual(1, numIn1);
            int numIn2 = db.Insert(obj2);
            Assert.AreEqual(1, numIn2);

            List<TestObj> result1 = db.Table<TestObj>().ToList();
            Assert.AreEqual(numIn1, result1.Count);
            Assert.AreEqual(obj1.Text, result1.First().Text);

            List<TestObj> result2 = db.Query<TestObj>("select * from TestObj2").ToList();
            Assert.AreEqual(numIn2, result2.Count);

            db.Close();
        }

        [Test]
        public void InsertTwoTimes()
        {
            var db = new OrmTestSession();
            db.CreateTable<TestObj>();

            var obj1 = new TestObj { Text = "GLaDOS loves testing!" };
            var obj2 = new TestObj { Text = "Keep testing, just keep testing" };

            int numIn1 = db.Insert(obj1);
            int numIn2 = db.Insert(obj2);
            Assert.AreEqual(1, numIn1);
            Assert.AreEqual(1, numIn2);

            List<TestObj> result = db.Query<TestObj>("select * from TestObj").ToList();
            Assert.AreEqual(2, result.Count);
            Assert.AreEqual(obj1.Text, result[0].Text);
            Assert.AreEqual(obj2.Text, result[1].Text);

            db.Close();
        }

        // BUG: Excetions ar silently swallowed in Silverlight
        //  - http://code.google.com/p/csharp-sqlite/issues/detail?id=130
        [Test]
        public void InsertWithExtra()
        {
            var db = new OrmTestSession();
            db.CreateTable<TestObj2>();

            var obj1 = new TestObj2 { Id = 1, Text = "GLaDOS loves testing!" };
            var obj2 = new TestObj2 { Id = 1, Text = "Keep testing, just keep testing" };
            var obj3 = new TestObj2 { Id = 1, Text = "Done testing" };

            db.Insert(obj1);

            try
            {
                db.Insert(obj2);
                Assert.Fail("Expected unique constraint violation");
            }
            catch (SqliteException)
            {
            }
            db.Insert(obj2, ConflictResolution.Replace);

            try
            {
                db.Insert(obj3);
                Assert.Fail("Expected unique constraint violation");
            }
            catch (SqliteException)
            {
            }
            db.Insert(obj3, ConflictResolution.Ignore);

            List<TestObj> result = db.Query<TestObj>("select * from TestObj2").ToList();
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(obj2.Text, result.First().Text);

            db.Close();
        }

        [Test]
        public void Unicode()
        {
            var insertItem = new TestObj { Text = "Sin�ad O'Connor" };

            var db = new OrmTestSession();
            db.CreateTable<TestObj>();

            int numIn = db.Insert(insertItem);

            Assert.AreEqual(numIn, 1, "Num inserted must = num objects");

            TestObj inObjs = db.Query<TestObj>("select * from TestObj").First();

            Assert.AreEqual(insertItem.Text, inObjs.Text);
        }

        [Test]
        public void InsertUsingSavePoints()
        {
            var obj = new TestObj { Text = "Matthew" };

            using (var db = new OrmTestSession())
            {
                db.CreateTable<TestObj>();

                using (var trans = db.BeginTransaction())
                {
                    db.Insert(obj);
                    Assert.AreEqual(db.Table<TestObj>().Count(), 1);

                    trans.CreateSavepoint("First");
                    db.Insert(obj);
                    Assert.AreEqual(db.Table<TestObj>().Count(), 2);

                    trans.RollbackSavepoint("First");

                    trans.Commit();
                }

                Assert.AreEqual(db.Table<TestObj>().Count(), 1);
            }
        }

        [Test]
        public void InsertUsingMultipleSavePoints()
        {
            var obj = new TestObj { Text = "Matthew" };

            using (var db = new OrmTestSession())
            {
                db.CreateTable<TestObj>();

                using (var trans = db.BeginTransaction())
                {
                    db.Insert(obj);
                    Assert.AreEqual(db.Table<TestObj>().Count(), 1);

                    trans.CreateSavepoint("First");
                    db.Insert(obj);
                    Assert.AreEqual(db.Table<TestObj>().Count(), 2);

                    trans.CreateSavepoint("Second");
                    db.Insert(obj);
                    Assert.AreEqual(db.Table<TestObj>().Count(), 3);

                    trans.RollbackSavepoint("Second");

                    trans.Commit();
                }

                Assert.AreEqual(db.Table<TestObj>().Count(), 2);
            }
        }

        [Test]
        public void InsertUsingInterweavingSavePoints()
        {
            var obj = new TestObj { Text = "Matthew" };

            using (var db = new OrmTestSession())
            {
                db.CreateTable<TestObj>();

                using (var trans = db.BeginTransaction())
                {
                    db.Insert(obj);
                    Assert.AreEqual(db.Table<TestObj>().Count(), 1);

                    trans.CreateSavepoint("First");
                    db.Insert(obj);
                    Assert.AreEqual(db.Table<TestObj>().Count(), 2);

                    trans.CreateSavepoint("Second");
                    db.Insert(obj);
                    Assert.AreEqual(db.Table<TestObj>().Count(), 3);

                    trans.RollbackSavepoint("First");

                    trans.Commit();
                }

                Assert.AreEqual(db.Table<TestObj>().Count(), 1);
            }
        }

        [Test]
        public void InsertUsingSavePointsOnACommittedTransaction()
        {
            var obj = new TestObj { Text = "Matthew" };

            using (var db = new OrmTestSession())
            {
                db.CreateTable<TestObj>();

                var trans = db.BeginTransaction();
                trans.CreateSavepoint("First");
                trans.Commit();

#if SILVERLIGHT
                ExceptionAssert.Throws<SqliteException>(() => trans.RollbackSavepoint("First"));
#elif NETFX_CORE
                Assert.ThrowsException<SqliteException>(() => trans.RollbackSavepoint("First"));
#else
                Assert.Catch<SqliteException>(() => trans.RollbackSavepoint("First"));
#endif
            }
        }

        [Test]
        public void DefaultsTest()
        {
            var db = new OrmTestSession();
            db.CreateTable<DefaultsObject>();

            int numIn = db.InsertDefaults<DefaultsObject>();

            Assert.AreEqual(numIn, 1, "Num inserted must = 1");

            var inObjs = db.Get<DefaultsObject>(1);

            Assert.AreEqual("Default Text", inObjs.Text);
            Assert.AreEqual(33, inObjs.Number);
        }

        [Test]
        [NUnit.Framework.Ignore] // TODO
        public void NestedTransactionsTest()
        {
            int oneTrans;
            int nestedTrans;

            // one transaction
            using (var db = new OrmTestSession())
            {
                SqliteSession.Trace = false;
                db.CreateTable<TestObj>();

                var start = Environment.TickCount;

                const int n = 500 * 500;
                IEnumerable<TestObj> q = Enumerable.Range(1, n).Select(x => new TestObj { Text = "I am" });
                TestObj[] objs = q.ToArray();
                int numIn = db.InsertAll(objs);

                oneTrans = Environment.TickCount - start;
            }

            // nested transactions
            using (var db = new OrmTestSession())
            {
                SqliteSession.Trace = false;
                db.CreateTable<TestObj>();

                var start = Environment.TickCount;

                using (var trans = db.BeginTransaction())
                {
                    for (var i = 0; i < 500; ++i)
                    {
                        const int n = 500;
                        IEnumerable<TestObj> q = Enumerable.Range(1, n).Select(x => new TestObj { Text = "I am" });
                        TestObj[] objs = q.ToArray();
                        int numIn = db.InsertAll(objs);
                    }

                    trans.Commit();
                }

                nestedTrans = Environment.TickCount - start;
            }

            System.Diagnostics.Debug.WriteLine("Single tranasction: " + oneTrans);
            System.Diagnostics.Debug.WriteLine("Nested tranasction: " + nestedTrans);
            System.Diagnostics.Debug.WriteLine("Difference: " + Math.Abs(nestedTrans - oneTrans));

            // this is a really dodgy test to use the execution time...
            Assert.IsTrue(Math.Abs(oneTrans - nestedTrans) <= 100);
        }
    }
}