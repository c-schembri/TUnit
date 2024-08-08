﻿namespace TUnit.TestProject.OneTimeSetUpWithBaseTests;

public class NonBase : Base1
{
    [Before(Class)]
    public static Task NonBaseOneTimeSetup()
    {
        return Task.CompletedTask;
    }
    
    [Before(EachTest)]
    public Task NonBaseSetUp()
    {
        return Task.CompletedTask;
    }

    [Test]
    public void Test()
    {
        // Dummy method
    }
}