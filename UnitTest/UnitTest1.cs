using System;
using System.Linq;


using Microsoft.VisualStudio.TestTools.UnitTesting;


using Regulus.Database;


using StackExchange.Redis;

namespace RedisTest
{
    [TestClass]
    public class UnitTestClient
    {
        private Regulus.Database.Redis.Client _Redis;

        public UnitTestClient()
        {
            var redis = ConnectionMultiplexer.Connect("localhost:6379");

            _Redis = new Regulus.Database.Redis.Client(redis.GetDatabase(), new JsonSeriallzer());
        }
        [TestMethod]
        public void TestMethod0()
        {
            var testObject = new TestObject();
            var id = Guid.NewGuid();
            testObject.Id = id;
            testObject.Value = 1345;
            _Redis.Add(testObject);

            var results = _Redis.Find<TestObject>(test_object => test_object.Id == id);
            Assert.AreEqual(1345, results.First().Value);
        }

        [TestMethod]
        public void TestMethod1()
        {            
            var testObject = new TestObject();
            var id = Guid.NewGuid();
            testObject.Id = id;
            testObject.Value = 1345;
            _Redis.Add(testObject);

            var results = _Redis.Find<TestObject>(test_object => test_object.Id == id );
            var result = results.First().Value;
            Assert.AreEqual(1345 , result);
        }

        [TestMethod]
        public void TestMethod2()
        {
            
            var id = Guid.NewGuid();
            var testObject = new TestObject();            
            testObject.Id = id;
            testObject.Value = 1345;
            _Redis.Add(testObject);

            testObject.Value = 13451;
            _Redis.Update(test_object => test_object.Id == id, testObject);

            var results = _Redis.Find<TestObject>(test_object => test_object.Id == id);
            Assert.AreEqual(13451, results.First().Value);
        }

        [TestMethod]
        public void TestMethod3()
        {
            
            var id = Guid.NewGuid();
            var testObject = new TestObject();
            testObject.Id = id;
            testObject.Value = 1345;
            _Redis.Add(testObject);

            testObject.Value = 13451;
            _Redis.UpdateField<TestObject , int>(test_object => test_object.Id == id, test_object => test_object.Value , 9999);

            var results = _Redis.Find<TestObject>(test_object => test_object.Id == id);
            Assert.AreEqual(9999, results.First().Value);
        }

        [TestMethod]
        public void TestMethod5()
        {
            var results = _Redis.Find<TestObject>(test_object => true).ToArray();
            
        }

        [TestMethod]
        public void TestMethod6()
        {
            bool allPass = true;
            var results = _Redis.Find<TestObject>(test_object => allPass).ToArray();

        }

        [TestMethod]
        public void TestMethod7()
        {
            var testObject1 = new TestObject();
            
            testObject1.Value = 13451;

            var id = Guid.NewGuid();
            var testObject = new TestObject();
            testObject.Id = id;
            testObject.Value = 1345;
            testObject.Child = testObject1;

            _Redis.Add(testObject);

            testObject.Value = 13451;
            _Redis.UpdateField<TestObject, TestObject>(test_object => test_object.Id == id, test_object => test_object.Child, null );

            var results = _Redis.GetField<TestObject , TestObject>(test_object => test_object.Id == id , o => o.Child );
            Assert.AreEqual(null, results.First());
        }

        [TestMethod]
        public void TestMethod8()
        {
            var testObject1 = new TestObject();

            testObject1.Value = 13451;

            var testObject2 = new TestObject();

            testObject2.Value = 13454651;

            var id = Guid.NewGuid();
            var testObject = new TestObject();
            testObject.Id = id;
            testObject.Value = 1345;
            testObject.Child = testObject1;
            testObject.Field = testObject2;

            _Redis.Add(testObject);

            var results = _Redis.GetField<TestObject, TestObject>(test_object => test_object.Id == id, o => o.Field);
            Assert.AreEqual(13454651, results.First().Value );
        }

        [TestMethod]
        public void TestMethod9()
        {
            var testObject1 = new TestObject();

            testObject1.Value = 13451;

            var testObject2 = new TestObject1();

            testObject2.Value = 13454651;

            var id = Guid.NewGuid();
            var testObject = new TestObject();
            testObject.Id = id;
            testObject.Value = 1345;
            testObject.Child = testObject1;
            testObject.Field2 = testObject2;

            _Redis.Add(testObject);

            var results = _Redis.GetField<TestObject, TestObject1>(test_object => test_object.Id == id, o => o.Field2);
            Assert.AreEqual(13454651, results.First().Value);
        }


        [TestMethod]
        public void TestMethod10()
        {
            var testObject1 = new TestObject();

            var id = Guid.NewGuid();
            testObject1.Id = id;
            testObject1.Value = 13451;
            testObject1.ByteArray = new byte[] {1,2,3,4,5,6,7,8,9,0};



            _Redis.Add(testObject1);

            var results = _Redis.Find<TestObject>((t) => t.Id == id);
            var test = results.First();
            Assert.AreEqual(1, test.ByteArray[0]);
            Assert.AreEqual(2, test.ByteArray[1]);
            Assert.AreEqual(3, test.ByteArray[2]);
            Assert.AreEqual(4, test.ByteArray[3]);
        }

        [TestMethod]
        public void TestMethod11()
        {
            var testObject1 = new TestObject();
            var id = Guid.NewGuid();
            testObject1.Id = id;
            testObject1.Value = 13451;
            testObject1.ByteArray = new byte[0] ;



            _Redis.Add(testObject1);

            var results = _Redis.Find<TestObject>((t) => t.Id == id);
            var test = results.First();
            Assert.AreEqual(0, test.ByteArray.Length);
            
        }

        [TestMethod]
        public void TestMethod12()
        {
            var testObject1 = new TestObject();
            var id = Guid.NewGuid();
            testObject1.Id = id;
            testObject1.Value = 13451;
            testObject1.ByteArray = null;



            _Redis.Add(testObject1);

            var results = _Redis.Find<TestObject>((t) => t.Id == id);
            var test = results.First();
            Assert.AreEqual(null, test.ByteArray );

        }

        [TestMethod]
        public void TestMethod999()
        {            
            var id = Guid.NewGuid();
            var testObject = new TestObject();
            testObject.Id = id;
            testObject.Value = 1345;
            _Redis.Add(testObject);

            var results = _Redis.Delete<TestObject>(t => true);
            Assert.AreNotEqual(0 , results);
        }
    }
}
