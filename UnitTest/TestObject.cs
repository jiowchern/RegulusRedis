using System;

namespace RedisTest
{
    
    public class TestObject
    {
        public Guid Id { get; set; }

        public int Value { get; set; }

        public TestObject Field;

        public TestObject Child { get; set; }
    }
}