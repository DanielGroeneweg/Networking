using System.Collections.Generic;
using System.Linq;
using UnityEditor;
public class HandEvaluator
{
    public class EvaluatedHand
    {
        public Combinations handRank;
        public List<int> values; // tie-breakers (high cards etc)

        public int CompareTo(EvaluatedHand other)
        {
            if (handRank != other.handRank)
                return handRank.CompareTo(other.handRank);

            for (int i = 0; i < values.Count; i++)
            {
                if (values[i] != other.values[i])
                    return values[i].CompareTo(other.values[i]);
            }

            return 0;
        }
    }
    public static int Compare(Player[] players, Card[] board)
    {
        var hand1 = Evaluate(GetAllCards(players[0], board));
        var hand2 = Evaluate(GetAllCards(players[1], board));

        int result = hand1.CompareTo(hand2);

        if (result > 0) return 1; // player 1 wins
        if (result < 0) return 2; // player 2 wins

        return -1; // tie
    }
    static int GetCardValue(Card card)
    {
        int value = (int)card.rank;

        // Ace should be high (14)
        if (value == 1)
            return 14;

        return value;
    }
    static List<Card> GetAllCards(Player player, Card[] board)
    {
        List<Card> cards = new List<Card>();
        cards.AddRange(player.cards);
        cards.AddRange(board);
        return cards;
    }
    static Dictionary<int, int> GetValueCounts(List<Card> cards)
    {
        Dictionary<int, int> counts = new Dictionary<int, int>();

        foreach (var card in cards)
        {
            int value = GetCardValue(card);

            if (!counts.ContainsKey(value))
                counts[value] = 0;

            counts[value]++;
        }

        return counts;
    }
    static int GetStraightHigh(List<int> values)
    {
        values = values.Distinct().OrderBy(v => v).ToList();

        // Handle Ace low (A=14 -> also 1)
        if (values.Contains(14))
            values.Insert(0, 1);

        int consecutive = 1;

        for (int i = 1; i < values.Count; i++)
        {
            if (values[i] == values[i - 1] + 1)
            {
                consecutive++;
                if (consecutive >= 5)
                    return values[i];
            }
            else
            {
                consecutive = 1;
            }
        }

        return -1;
    }
    static List<Card> GetFlush(List<Card> cards)
    {
        // fancy :-)
        return cards
            .GroupBy(c => c.suit)
            .Where(g => g.Count() >= 5)
            .SelectMany(g => g)
            .OrderByDescending(c => GetCardValue(c))
            .Take(5)
            .ToList();
    }
    static EvaluatedHand Evaluate(List<Card> cards)
    {
        var values = cards
            .Select(c => GetCardValue(c))
            .OrderByDescending(v => v)
            .ToList();

        var counts = GetValueCounts(cards);

        var groups = counts
            .OrderByDescending(kv => kv.Value)
            .ThenByDescending(kv => kv.Key)
            .ToList();

        var flushCards = GetFlush(cards);
        int straightHigh = GetStraightHigh(values);

        // Straight Flush
        if (flushCards != null && flushCards.Count >= 5)
        {
            int sfHigh = GetStraightHigh(
                flushCards.Select(c => GetCardValue(c)).ToList()
            );

            if (sfHigh != -1)
            {
                return new EvaluatedHand
                {
                    handRank = Combinations.StraightFlush,
                    values = new List<int> { sfHigh }
                };
            }
        }

        // Four of a Kind
        if (groups[0].Value == 4)
        {
            return new EvaluatedHand
            {
                handRank = Combinations.FourOfAKind,
                values = new List<int> { groups[0].Key, groups[1].Key }
            };
        }

        // Full House
        if (groups[0].Value == 3 && groups[1].Value >= 2)
        {
            return new EvaluatedHand
            {
                handRank = Combinations.FullHouse,
                values = new List<int> { groups[0].Key, groups[1].Key }
            };
        }

        // Flush
        if (flushCards != null && flushCards.Count >= 5)
        {
            return new EvaluatedHand
            {
                handRank = Combinations.Flush,
                values = flushCards
                    .Select(c => GetCardValue(c))
                    .ToList()
            };
        }

        // Straight
        if (straightHigh != -1)
        {
            return new EvaluatedHand
            {
                handRank = Combinations.Straight,
                values = new List<int> { straightHigh }
            };
        }

        // Three of a kind
        if (groups[0].Value == 3)
        {
            return new EvaluatedHand
            {
                handRank = Combinations.ThreeOfAKind,
                values = new List<int> {
                groups[0].Key,
                groups[1].Key,
                groups[2].Key
            }
            };
        }

        // Two pair
        if (groups[0].Value == 2 && groups[1].Value == 2)
        {
            return new EvaluatedHand
            {
                handRank = Combinations.TwoPair,
                values = new List<int> {
                groups[0].Key,
                groups[1].Key,
                groups[2].Key
            }
            };
        }

        // One pair
        if (groups[0].Value == 2)
        {
            return new EvaluatedHand
            {
                handRank = Combinations.Pair,
                values = new List<int> {
                groups[0].Key,
                groups[1].Key,
                groups[2].Key,
                groups[3].Key
            }
            };
        }

        // High card
        return new EvaluatedHand
        {
            handRank = Combinations.HighCard,
            values = values.Take(5).ToList()
        };
    }
}