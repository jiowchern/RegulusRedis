using System;

namespace RedisTest
{
    public struct TestObject1
    {
        public int Value;
    }
    public class TestObject
    {
        public Guid Id { get; set; }

        public int Value { get; set; }

        public TestObject Field;

        public TestObject Child { get; set; }

        public TestObject1 Field2;
    }
}