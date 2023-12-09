using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using AdventOfCode;

var cookie = args.FirstOrDefault("");

var input = cookie switch
{
    "test" => await Input.TestAsync(),
    "reddit" => await Input.TestAsync("Reddit.txt"),
    _ => await Input.RealAsync(7, cookie),
};

var hands = input
    .Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
    .Select(Hand.Create)
    .ToList();

// foreach (var hand in hands)
//     Console.WriteLine(hand);

var result1 = hands
    .Order()
    .Select((hand, index) => hand.Bid * (index + 1))
    .Sum();
Console.WriteLine($"Result 1: {result1}");

var result2 = hands
    .Select(Hand.ApplyJokers)
    .Order()
    .Select((hand, index) => hand.Bid * (index + 1))
    .Sum();
Console.WriteLine($"Result 2: {result2}");

internal abstract record Hand(IEnumerable<Card> Cards, int Bid, int TypeValue) : IComparable<Hand>
{
    public static Hand Create(string handLine)
    {
        var (labels, bid) = handLine.Split(' ', StringSplitOptions.TrimEntries) switch
        {
            [var first, var second] when int.TryParse(second, out var parsed) => (first, parsed),
            _ => throw new ApplicationException("Hand line should consist of two parts, space separated"),
        };

        var cards = labels.ToCharArray().Select(Card.Create).ToPrintableCollection();

        if (cards is not { Count: 5 }) throw new ApplicationException("Hand should contain exactly 5 cards");

        var sets = cards
            .GroupBy(card => card.Label, resultSelector: (_, group) => group.Count())
            .OrderDescending()
            .ToList();

        return sets switch 
        {
            [ 5 ] => new FiveOfAKind(cards, bid),
            [ 4, 1 ] => new FourOfAKind(cards, bid),
            [ 3, 2 ] => new FullHouse(cards, bid),
            [ 3, 1, 1 ] => new ThreeOfAKind(cards, bid),
            [ 2, 2, 1 ] => new TwoPair(cards, bid),
            [ 2, 1, 1, 1 ] => new OnePair(cards, bid),
            [ 1, 1, 1, 1, 1 ] => new HighCard(cards, bid),
            _ => throw new UnreachableException()
        };
    }

    public static Hand ApplyJokers(Hand hand)
    {
        var jokerCount = hand.Cards.Count(card => card is { Label: 'J' });

        return hand switch
        {
            FourOfAKind when jokerCount is 1 or 4 => new FiveOfAKind(ApplyJokersToCards(), hand.Bid),
            FullHouse when jokerCount is 2 or 3 => new FiveOfAKind(ApplyJokersToCards(), hand.Bid),
            ThreeOfAKind when jokerCount is 1 or 3 => new FourOfAKind(ApplyJokersToCards(), hand.Bid),
            TwoPair when jokerCount is 2 => new FourOfAKind(ApplyJokersToCards(), hand.Bid),
            TwoPair when jokerCount is 1 => new FullHouse(ApplyJokersToCards(), hand.Bid),
            OnePair when jokerCount is 1 or 2 => new ThreeOfAKind(ApplyJokersToCards(), hand.Bid),
            HighCard when jokerCount is 1 => new OnePair(ApplyJokersToCards(), hand.Bid),
            _ when jokerCount is not 0 => hand with { Cards = ApplyJokersToCards() },
            _ => hand
        };

        IEnumerable<Card> ApplyJokersToCards() => hand.Cards.Select(Card.ApplyJoker).ToPrintableCollection();
    }

    public int CompareTo(Hand? other)
    {
        return other switch 
        {
            null => 1,
            _ when TypeValue > other.TypeValue => 1,
            _ when TypeValue < other.TypeValue => -1,
            _ when CompareCardsIncrementally(other) is not 0 and var notZero => notZero,
            _ when Bid > other.Bid => 1,
            _ when Bid < other.Bid => -1,
            _ => 0
        };

        int CompareCardsIncrementally(Hand other) => 
            Cards.Zip(other.Cards, (first, second) => first.CompareTo(second))
                .FirstOrDefault(i => i is not 0, defaultValue: 0);
    }
}

internal record FiveOfAKind(IEnumerable<Card> Cards, int Bid) : Hand(Cards, Bid, 6);
internal record FourOfAKind(IEnumerable<Card> Cards, int Bid) : Hand(Cards, Bid, 5);
internal record FullHouse(IEnumerable<Card> Cards, int Bid) : Hand(Cards, Bid, 4);
internal record ThreeOfAKind(IEnumerable<Card> Cards, int Bid) : Hand(Cards, Bid, 3);
internal record TwoPair(IEnumerable<Card> Cards, int Bid) : Hand(Cards, Bid, 2);
internal record OnePair(IEnumerable<Card> Cards, int Bid) : Hand(Cards, Bid, 1);
internal record HighCard(IEnumerable<Card> Cards, int Bid) : Hand(Cards, Bid, 0);

internal record Card(char Label) : IComparable<Card>
{
    public static Card Create(char label) => new(label);

    public static Card ApplyJoker(Card card) => card switch
    {
        { Label: 'J' } => card with { Value = 1 },
        _ => card   
    };

    public int Value { get; private set; } = Label switch
    {
        'A' => 14,
        'K' => 13,
        'Q' => 12,
        'J' => 11,
        'T' => 10,
        char digit when char.IsDigit(digit) => int.Parse(digit.ToString()), 
        _ => throw new ApplicationException($"Card label {Label} is invalid")
    };

    public int CompareTo(Card? other)
    {
        return other switch
        {
            null => 1,
            _ when Value > other.Value => 1,
            _ when Value < other.Value => -1,
            _ => 0
        };
    }
}
