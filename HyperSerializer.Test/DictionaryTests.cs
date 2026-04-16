using System;
using System.Collections.Generic;
using NUnit.Framework;
using Hyper;

namespace Hyper.Test;

[TestFixture]
public class DictionaryTests
{
    [Test]
    public void Test_IntStringDictionary_Serialization()
    {
        var original = new TestClassWithDictionary
        {
            Id = 42,
            Data = new Dictionary<int, int>
            {
                { 1, 100 },
                { 2, 200 },
                { 3, 300 }
            }
        };

        var serialized = HyperSerializer<TestClassWithDictionary>.Serialize(original);
        var deserialized = HyperSerializer<TestClassWithDictionary>.Deserialize(serialized);

        Assert.AreEqual(original.Id, deserialized.Id);
        Assert.AreEqual(original.Data.Count, deserialized.Data.Count);
        foreach (var kvp in original.Data)
        {
            Assert.IsTrue(deserialized.Data.ContainsKey(kvp.Key));
            Assert.AreEqual(kvp.Value, deserialized.Data[kvp.Key]);
        }
    }

    [Test]
    public void Test_DictionaryWithNullValue()
    {
        var original = new TestClassWithDictionary
        {
            Id = 42,
            Data = null
        };

        var serialized = HyperSerializer<TestClassWithDictionary>.Serialize(original);
        var deserialized = HyperSerializer<TestClassWithDictionary>.Deserialize(serialized);

        Assert.AreEqual(original.Id, deserialized.Id);
        Assert.IsNull(deserialized.Data);
    }

    [Test]
    public void Test_EmptyDictionary()
    {
        var original = new TestClassWithDictionary
        {
            Id = 42,
            Data = new Dictionary<int, int>()
        };

        var serialized = HyperSerializer<TestClassWithDictionary>.Serialize(original);
        var deserialized = HyperSerializer<TestClassWithDictionary>.Deserialize(serialized);

        Assert.AreEqual(original.Id, deserialized.Id);
        Assert.AreEqual(0, deserialized.Data.Count);
    }

    [Test]
    public void Test_LargeDictionary()
    {
        var original = new TestClassWithDictionary
        {
            Id = 42,
            Data = new Dictionary<int, int>()
        };

        // Add 1000 entries
        for (int i = 0; i < 1000; i++)
        {
            original.Data[i] = i * 2;
        }

        var serialized = HyperSerializer<TestClassWithDictionary>.Serialize(original);
        var deserialized = HyperSerializer<TestClassWithDictionary>.Deserialize(serialized);

        Assert.AreEqual(original.Id, deserialized.Id);
        Assert.AreEqual(1000, deserialized.Data.Count);
        for (int i = 0; i < 1000; i++)
        {
            Assert.AreEqual(i * 2, deserialized.Data[i]);
        }
    }

    [Test]
    public void Test_DictionaryAsync()
    {
        var original = new TestClassWithDictionary
        {
            Id = 42,
            Data = new Dictionary<int, int>
            {
                { 1, 100 },
                { 2, 200 }
            }
        };

        var task = HyperSerializer<TestClassWithDictionary>.SerializeAsync(original);
        var serialized = task.Result;

        var deserializeTask = HyperSerializer<TestClassWithDictionary>.DeserializeAsync(serialized);
        var deserialized = deserializeTask.Result;

        Assert.AreEqual(original.Id, deserialized.Id);
        Assert.AreEqual(original.Data.Count, deserialized.Data.Count);
    }
}

public class TestClassWithDictionary
{
    public int Id { get; set; }
    public Dictionary<int, int> Data { get; set; }
}
