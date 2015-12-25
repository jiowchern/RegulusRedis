using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;


using Microsoft.VisualStudio.TestTools.UnitTesting;


using RedisTest;


using Regulus.Database.Redis;


using StackExchange.Redis;

namespace UnitTest
{
    /// <summary>
    /// UnitTest2 的摘要描述
    /// </summary>
    [TestClass]
    public class UnitTestMapper
    {
        
        public UnitTestMapper()
        {
            var redis = ConnectionMultiplexer.Connect("localhost:6379");

            _Redis = new Regulus.Database.Redis.Client(redis.GetDatabase(), new JsonSeriallzer());
        }

        private TestContext testContextInstance;

        private Client _Redis;

        /// <summary>
        ///取得或設定提供目前測試回合
        ///相關資訊與功能的測試內容。
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        #region 其他測試屬性
        //
        // 您可以使用下列其他屬性撰寫測試: 
        //
        // 執行該類別中第一項測試前，使用 ClassInitialize 執行程式碼
        // [ClassInitialize()]
        // public static void MyClassInitialize(TestContext testContext) { }
        //
        // 在類別中的所有測試執行後，使用 ClassCleanup 執行程式碼
        // [ClassCleanup()]
        // public static void MyClassCleanup() { }
        //
        // 在執行每一項測試之前，先使用 TestInitialize 執行程式碼 
        // [TestInitialize()]
        // public void MyTestInitialize() { }
        //
        // 在執行每一項測試之後，使用 TestCleanup 執行程式碼
        // [TestCleanup()]
        // public void MyTestCleanup() { }
        //
        #endregion
        
        
        [TestMethod]
        public void TestMethod1()
        {            
            var testObject = new TestObject();
            var id = Guid.NewGuid();
            testObject.Id = id;
            testObject.Value = 1345;
            _Redis.Add(testObject);

            var mapper = new Regulus.Database.Redis.Mapper<TestObject>(_Redis , obj => obj.Id == id);            
            mapper.Update<int>(obj => obj.Value, 99);
            var result = mapper.Get<int>(obj => obj.Value);
            Assert.AreEqual(99 , result.First());
        }

        [TestMethod]
        public void TestMethod2()
        {
            var testObject = new TestObject();
            var id = Guid.NewGuid();
            testObject.Id = id;
            testObject.Value = 1345;
            _Redis.Add(testObject);

            var mapper = new Regulus.Database.Redis.Mapper<TestObject>(_Redis, obj => obj.Id == id);

            testObject.Value = 132245;
            mapper.Update(obj => obj, testObject);
            var result = mapper.Get(obj => obj.Value);
            Assert.AreEqual(132245, result.First());
        }
    }
}
