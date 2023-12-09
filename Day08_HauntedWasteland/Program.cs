using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using AdventOfCode;

var cookie = args.FirstOrDefault("");

var input = cookie switch
{
    "test1" => await Input.TestAsync("Test1.txt"),
    "test2" => await Input.TestAsync("Test2.txt"),
    "test3" => await Input.TestAsync("Test3.txt"),
    _ => await Input.RealAsync(8, cookie),
};

var (instructions, nodes) = input
    .Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
    .ToList() switch
    {
        [var first, .. var theRest] => (first, theRest),
        _ => throw new ApplicationException("Input should contain initial instruction line, followed by list of nodes`")
    };

var graph = nodes
    .Select(Node.Create)
    .ToList();
graph = graph
    .Select(node => node.Link(graph))
    .ToList();

var result1 = graph.Enumerate(instructions, startLabel: "AAA", endLabel: "ZZZ").Count() - 1;
Console.WriteLine($"Result 1: {result1}");

var result2 = await graph.CalculateStepsAsync(instructions, 
        startCondition: node => node is { LabelLastCharacter: 'A' },
        endCondition: current => current is { LabelLastCharacter: 'Z' });
Console.WriteLine($"Result 2: {result2}");

internal static class DomainExtensions
{
    private static IEnumerable<char> RepeatInstructions(this string instructions, Func<bool> whileFunc)
    {
        do
        {
            foreach (var instruction in instructions.Where(_ => whileFunc()))
                yield return instruction;
        }
        while (whileFunc());
    }

    public static IEnumerable<Node> Enumerate(this IEnumerable<Node> nodes, string instructions, string startLabel, string endLabel)
    {
        var current = nodes.Single(node => node.Label == startLabel);
        foreach (var instruction in instructions.RepeatInstructions(() => current.Label != endLabel))
        {
            yield return current;
            current = instruction switch
            {
                'L' when current is { Left: Node left } => left,
                'R' when current is  { Right: Node right } => right,
                _ => throw new ApplicationException($"Only L and R are valid instructions, current is {current}")
            };
        }
        yield return current;
    }

    public static async Task<long> CalculateStepsAsync(this IEnumerable<Node> nodes,
        string instructions, Predicate<Node> startCondition, Predicate<Node> endCondition)
    {
        var startNodes = nodes
            .Where(node => startCondition(node))
            .ToList();

        var visitorCount = startNodes.Count;

        var max = 0L;
        var semaphore = new SemaphoreSlim(0);
        var channel = Channel.CreateBounded<long>(capacity: visitorCount);

        var nodeVisitors = startNodes
            .Select(EnumerateInternalAsync)
            .ToList();

        var messages = new List<long>();
        await foreach (var message in channel.Reader.ReadAllAsync())
        {
            messages.Add(message);
            if (messages.Count == visitorCount)
            {
                if (messages.Distinct().ToArray() is [ long steps ])
                {
                    nodeVisitors.ForEach(v => v.Dispose());
                    return steps;
                }
                
                // keep the biggest, release the other workers to leap frog
                messages = [ messages.Max() ];
                semaphore.Release(visitorCount - 1);
            }
        }

        throw new UnreachableException();

        async Task EnumerateInternalAsync(Node startNode)
        {
            long steps = 0;
            var current = nodes.Single(node => node == startNode);
            foreach (var instruction in instructions.RepeatInstructions(() => true))
            {
                current = instruction switch
                {
                    'L' when current is { Left: Node left } => left,
                    'R' when current is  { Right: Node right } => right,
                    _ => throw new ApplicationException("Only L and R are valid instructions")
                };

                steps++;

                if (endCondition(current))
                {
                    await channel.Writer.WriteAsync(steps);
                    await semaphore.WaitAsync();

                    while (steps == max) // allow other workers to catch up
                    {
                        semaphore.Release();
                        await semaphore.WaitAsync();
                    }
                }
            }
        }
    }
}

internal record Node(string Label, string LeftLabel, string RightLabel)
{
    public static Node Create(string nodeLine)
    {
        var (label, connections) = nodeLine.SplitAndDeconstruct2('=', StringSplitOptions.TrimEntries);
        var (left, right) = connections.Replace("(", "").Replace(")", "").SplitAndDeconstruct2(',', StringSplitOptions.TrimEntries);
        return new(label, left, right);
    }

    public char LabelLastCharacter { get; } = Label.Last();

    public Node? Left { get; private set; }
    public Node? Right { get; private set; }

    public Node Link(IEnumerable<Node> nodes) // NB this must be by reference
    {
        Left = nodes.Single(node => node.Label == LeftLabel);
        Right = nodes.Single(node => node.Label == RightLabel);
        return this;
    }
}
