using UnityEngine;

/// <summary>GameState와 InventoryManager를 구독해 표시만 담당하는 프로토타입 UI입니다.</summary>
public sealed class InventoryUI : MonoBehaviour
{
    private Texture2D background;
    private void Awake()
    {
        background = new Texture2D(1, 1);
        background.SetPixel(0, 0, new Color(0, 0, 0, .85f));
        background.Apply();
    }
    private void OnDestroy() { if (background != null) Destroy(background); }
    private void OnGUI()
    {
        if (GameStateManager.Instance == null || !GameStateManager.Instance.IsState(GameState.Inventory)) return;
        const int width = 450, height = 300;
        int x = (Screen.width - width) / 2, y = (Screen.height - height) / 2;
        GUIStyle box = new GUIStyle(GUI.skin.box); box.normal.background = background;
        GUI.Box(new Rect(x, y, width, height), string.Empty, box);
        GUIStyle title = new GUIStyle { fontSize = 24, fontStyle = FontStyle.Bold, alignment = TextAnchor.UpperCenter };
        title.normal.textColor = Color.yellow;
        GUI.Label(new Rect(x, y + 25, width, 40), "INVENTORY (PROTOTYPE)", title);
        GUIStyle list = new GUIStyle { fontSize = 18, alignment = TextAnchor.UpperCenter }; list.normal.textColor = Color.white;
        var items = InventoryManager.Instance == null ? null : InventoryManager.Instance.Items;
        if (items == null || items.Count == 0) GUI.Label(new Rect(x, y + 120, width, 30), "(인벤토리가 비어 있습니다)", list);
        else for (int i = 0; i < items.Count; i++) GUI.Label(new Rect(x, y + 90 + 28 * i, width, 30), $"•  {items[i].displayName}", list);
        GUI.Label(new Rect(x, y + height - 35, width, 25), "[E] 또는 [ESC] 닫기", new GUIStyle(list) { fontSize = 14, alignment = TextAnchor.LowerCenter });
    }
}
