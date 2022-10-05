namespace MahjongServer;

public class CardDeck
{
    private byte[] _cards;
    private int _head;
    private int _tail;

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
}