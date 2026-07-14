using UnityEngine;

/// <summary>씬마다 수동으로 매니저를 배치하지 않아도 핵심 서비스를 한 번 생성합니다.</summary>
public static class GameBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void CreateServices()
    {
        GameObject root = new GameObject("Game Services");
        Object.DontDestroyOnLoad(root);
        root.AddComponent<GameStateManager>();
        root.AddComponent<InteractionManager>();
        root.AddComponent<InventoryManager>();
        root.AddComponent<InventoryUI>();
        root.AddComponent<InputManager>();
        root.AddComponent<PlayerInteractor>();
    }
}
