using System;
using System.Security.Cryptography;
using AtraBase.Toolkit.StringHandler;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

namespace Benchmarking;

[MemoryDiagnoser]
public class SplitTest
{
    private static readonly string text = System.IO.File.ReadAllText(@"C:\Users\night\source\repos\Benchmarking\TestingStuff\text.txt");

    [Benchmark]
    public void SpanSplit()
    {
        for (int i = 0; i < 1000; i++)
        {
            foreach (var line in text.SpanSplit())
            {
                int.TryParse(line, out var result);
            }
        }
    }

    [Benchmark]
    public void Split()
    {
        for (int i = 0; i < 1000; i++)
        {
            foreach (var line in text.Split())
            {
                int.TryParse(line, out var result);
            }
        }
    }
}

public class Program
{
    public static void Main(string[] args)
    {
        var summary = BenchmarkRunner.Run(typeof(Program).Assembly);
    }
}
