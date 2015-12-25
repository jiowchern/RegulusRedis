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
