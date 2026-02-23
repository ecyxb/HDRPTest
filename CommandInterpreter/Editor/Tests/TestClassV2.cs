using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace EventFramework.Test
{
    public class TestClassV2
    {
        public int PublicField = 10;
        public int PublicProperty { get; set; } = 20;

        private int privateField = 42;
        private string PrivateProperty { get; set; } = "secret";
        protected int protectedField = 100;

        public NestedClass Nested;

        private string PrivateMethod()
        {
            return "private result";
        }

        public class NestedClass
        {
            public int Value { get; set; }
        }
        public static string TestGenericMethod<T>(T input)
        {
            return input.GetType().Name;
        }

        public static string TestGenericMethod2<T1, T2>(T1 input1, T2 input2)
        {
            return $"{input1.GetType().Name}, {input2.GetType().Name}";
        }

        public string InstanceGenericMethod<T>(T input)
        {
            return input.GetType().Name;
        }
    }
}