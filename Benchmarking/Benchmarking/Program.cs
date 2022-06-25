using System;
using System.Security.Cryptography;
using AtraBase.Toolkit.StringHandler;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

namespace Benchmarking;

public class Program
{
    public static void Main(string[] args)
    {
        var summary = BenchmarkRunner.Run(typeof(Program).Assembly);
    }
}
