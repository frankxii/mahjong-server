namespace MahjongServer;

public class CardDeck
{
    private byte[] _cards;
    private int _head;
    private int _tail;

    public int RemainCard => _tail - _head;


    public CardDeck()
    {
        _cards = new byte[]
        {
            0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, // 万
            0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09,
            0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09,
            0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09,
            0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18, 0x19, // 筒
            0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18, 0x19,
            0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18, 0x19,
            0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18, 0x19,
            0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x27, 0x28, 0x29, // 条
            0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x27, 0x28, 0x29,
            0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x27, 0x28, 0x29,
            0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x27, 0x28, 0x29
        };
        _tail = _cards.Length - 1;
    }

    public void PrintAll()
    {
        foreach (byte card in _cards)
        {
            Console.Write($"{card:X2} ");
        }
    }

    public void Shuffle()
    {
        Random random = new();
        int newIndex;
        byte temp;
        for (int index = _cards.Length - 1; index > 0; index--)
        {
            newIndex = random.Next(0, index + 1);
            temp = _cards[newIndex];
            _cards[newIndex] = _cards[index];
            _cards[index] = temp;
        }
    }

    // 发牌
    public List<byte> Deal(int cardCount = 13)
    {
        List<byte> cards = _cards.Skip(_head).Take(cardCount).ToList();
        _head += cardCount;
        return cards;
    }

    // 摸牌
    public byte Draw()
    {
        byte card = _cards[_head];
        _head++;
        return card;
    }

    public byte DrawFromTail()
    {
        byte card = _cards[_tail];
        _tail--;
        return card;
    }

    /// <summary>
    /// 碰检测
    /// </summary>
    /// <param name="handCards">手牌</param>
    /// <param name="card">其他玩家打出的牌</param>
    /// <returns>是否可以碰</returns>
    public static bool CanPeng(List<byte> handCards, byte card)
    {
        return handCards.Count(item => item == card) == 2;
    }

    /// <summary>
    /// 杠检测
    /// </summary>
    /// <param name="handCards">手牌</param>
    /// <param name="card">摸到的牌或其他玩家打出的牌</param>
    /// <returns>是否可以杠</returns>
    public static bool CanGang(List<byte> handCards, byte card)
    {
        return handCards.Count(item => item == card) == 3;
    }

    /// <summary>
    /// 胡检测
    /// </summary>
    /// <param name="handCards">玩家手牌</param>
    /// <param name="card">摸到的牌或其他玩家打出的牌</param>
    /// <returns>是否可以胡</returns>
    public static bool CanHu(List<byte> handCards, byte card)
    {
        // 只有两张牌时，判断单调胡，两张牌是否一样
        if (handCards.Count == 1)
            return handCards[0] == card;

        // 合并手牌和加入判断的牌并排序
        List<byte> cards = new(handCards);
        cards.Add(card);
        cards.Sort();

        // 统计每张牌张数
        Dictionary<byte, int> count = new();
        foreach (byte number in cards)
        {
            if (count.ContainsKey(number))
                count[number]++;
            else
                count[number] = 1;
        }

        // 穷举所有可以做将牌的情况
        foreach (KeyValuePair<byte, int> pair in count)
        {
            if (pair.Value >= 2)
            {
                List<byte> cardsWithOutJiang = new(cards);
                cardsWithOutJiang.Remove(pair.Key);
                cardsWithOutJiang.Remove(pair.Key);
                if (CanDivideCards(cardsWithOutJiang))
                    return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 检测去掉一对将的其他牌是否可以分解配对
    /// </summary>
    /// <param name="cards">去掉将的牌组</param>
    /// <returns>是否可以胡</returns>
    private static bool CanDivideCards(List<byte> cards)
    {
        // 移除刻子
        int count = 0;
        byte card = 0;
        foreach (byte c in cards)
        {
            if (c == card)
                count++;
            else
            {
                count = 1;
                card = c;
            }

            if (count == 3)
            {
                // 重置计数器
                count = 0;
                List<byte> newCards = new(cards);
                newCards.Remove(card);
                newCards.Remove(card);
                newCards.Remove(card);
                // 移除刻子后继续递归
                if (CanDivideCards(newCards))
                    return true;
            }
        }

        // 取列表第一个作为标志，看是否存在大1点大2点的牌
        byte first = cards[0];
        byte second = (byte) (first + 1);
        byte third = (byte) (first + 2);
        if (cards.Contains(second) && cards.Contains(third))
        {
            // 所有卡牌都已配对，可以胡
            if (cards.Count == 3)
                return true;
            // 移除一搭顺子后继续递归，移除顺子不需要还原卡牌，所以在原列表上操作，不需要copy新列表
            cards.Remove(first);
            cards.Remove(second);
            cards.Remove(third);
            return CanDivideCards(cards);
        }

        return false;
    }
}