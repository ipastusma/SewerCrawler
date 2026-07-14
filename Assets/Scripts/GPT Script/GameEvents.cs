using System;

/// <summary>여러 시스템이 같은 게임 사건에 반응할 때 사용하는 최소 이벤트 허브입니다.</summary>
public static class GameEvents
{
    public static event Action<InventoryItem> ItemPicked;
    public static void PublishItemPicked(InventoryItem item) => ItemPicked?.Invoke(item);
}
