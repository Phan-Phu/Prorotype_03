using Prototype.Application;
using Prototype.Domain;
using Prototype.Infrastructure;
using UnityEngine;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Prototype.Application
{
    /// <summary>
    /// Runtime entry point. Bootstraps the whole prototype from GameState (plain C#) and wires the
    /// MonoBehaviour view layer. Uses [RuntimeInitializeOnLoadMethod] so it works whether or not the
    /// scene already contains the right GameObjects (CI build uses a freshly-create-empty scene).
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }
        public GameState State { get; private set; }
        public MasterDataSnapshot MasterData { get; private set; }
        public MasterDataAsset MasterDataAsset { get; private set; }

        private PlayerController _player;
        private DebugPanel _debug;
        private IGameTimeUseCase _timeUseCase;
        private IGameStateRepository _stateRepository;
        private IGameWorldService _worldService;
        // Keep Unity coupled to the DI abstraction. The concrete Microsoft provider stays
        // inside this composition root, which makes the rest of the game portable and avoids
        // relying on provider-specific APIs during domain/application execution.
        private IServiceProvider _services;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            // Prototype_Main can author the composition root in the scene. Do not create a
            // second runtime root when that scene already contains one.
            if (FindFirstObjectByType<GameManager>() != null) return;
            var go = new GameObject("GameManager");
            go.AddComponent<GameManager>();
        }

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Load the aggregate through the Domain repository port. Infrastructure owns the
            // storage adapter; GameState itself remains a Domain entity.
            MasterDataAsset = MasterDataImporter.LoadAsset();
            var masterDataResult = MasterDataImporter.Load().GetAwaiter().GetResult();
            if (!masterDataResult.IsSuccess)
            {
                Debug.LogError($"Could not import Master Data: {masterDataResult.Failure.Message} ({masterDataResult.Failure.Context})");
                return;
            }
            MasterData = masterDataResult.Value;
            _stateRepository = new InMemoryGameStateRepository();
            var load = _stateRepository.Load(20, 20, Prototype.Application.BootArgs.Seed, MasterData)
                .GetAwaiter().GetResult();
            if (!load.IsSuccess)
            {
                Debug.LogError($"Could not load game state: {load.Failure.Message}");
                return;
            }
            State = load.Value;
            State.Clock.Day = Mathf.Max(1, Prototype.Application.BootArgs.StartDay);
            State.Wallet.Money = Prototype.Application.BootArgs.StartMoney;

            ConfigureServices(_stateRepository);

            SetupCamera();
            SetupWorldView();
            SetupScenery();
            SetupPlayer();
            SetupHud();
            SetupToolbar();
            SetupInventoryScreen();
            SetupSeedShop();
            SetupNpcDialogue();
            SetupDebug();
        }

        void ConfigureServices(IGameStateRepository stateRepository)
        {
            var services = new ServiceCollection();
            services.AddSingleton(stateRepository);
            // Keep the application boundary explicit: raw inventory is projected by the
            // Infrastructure mapper when a view needs its destination type.
            var inventoryService = new InventoryService(State.InventorySystem);
            var clockService = new ClockService();
            var gameplayService = new GameplayService(inventoryService);
            var dialogueService = new DialogueService();
            var worldService = new GameWorldService();
            _worldService = worldService;
            var stateService = new GameStateApplicationService(State, inventoryService, clockService);
            services.AddSingleton<InventoryService>(inventoryService);
            services.AddSingleton<Prototype.Domain.IInventoryService>(inventoryService);
            services.AddSingleton<IClockService>(clockService);
            services.AddSingleton<IGameplayService>(gameplayService);
            services.AddSingleton<IDialogueService>(dialogueService);
            services.AddSingleton<IGameWorldService>(worldService);
            services.AddSingleton<IGameStateQuery>(stateService);
            services.AddSingleton<IGameTimeUseCase>(stateService);
            _services = services.BuildServiceProvider(new ServiceProviderOptions
            {
                ValidateScopes = true,
                ValidateOnBuild = true
            });
            _timeUseCase = _services.GetRequiredService<IGameTimeUseCase>();
        }

        void OnDestroy()
        {
            if (ReferenceEquals(Instance, this)) Instance = null;
            if (_services is IDisposable disposable) disposable.Dispose();
            _services = null;
        }

        void Update()
        {
            if (State == null) return;
            float dt = Time.deltaTime * (Prototype.Application.BootArgs.FastTime ? 10f : 1f);
            _timeUseCase?.Advance(new AdvanceTimeRequest(dt));
        }

        void SetupCamera()
        {
            // Don't rely on Camera.main alone: if the scene's camera isn't tagged "MainCamera"
            // (bug found 2026-08-15: Prototype_Main.unity had an untagged camera), Camera.main
            // returns null and this used to spawn a SECOND camera on top of the untagged one,
            // whose default Skybox clear flags painted over the whole game (blue screen, nothing
            // else visible). Find every camera in the scene, reuse the first one, and remove any
            // extras so there is always exactly one camera driving the view.
            var cams = FindObjectsByType<Camera>(FindObjectsSortMode.None);
            Camera cam;
            if (cams.Length == 0)
            {
                var go = new GameObject("Main Camera");
                cam = go.AddComponent<Camera>();
            }
            else
            {
                cam = cams[0];
                for (int i = 1; i < cams.Length; i++) Destroy(cams[i].gameObject);
            }
            cam.gameObject.tag = "MainCamera";
            cam.orthographic = true;
            // 0.65 (not 0.55) so the scenery ring just outside the grid (SetupScenery) is actually
            // in view, not just the play field.
            cam.orthographicSize = State.Grid.Height * 0.65f;
            cam.transform.position = new Vector3(0f, 0f, -10f);
            cam.backgroundColor = new Color(0.12f, 0.12f, 0.14f);
            cam.clearFlags = CameraClearFlags.SolidColor;
        }

        void SetupWorldView()
        {
            var wv = FindFirstObjectByType<WorldView>();
            if (wv == null)
            {
                var go = new GameObject("WorldView");
                wv = go.AddComponent<WorldView>();
            }
            wv.State = State;
        }

        /// <summary>
        /// Static, non-interactive scenery (trees/rocks/grass/farmhouse) placed in a ring just outside
        /// the playable grid so the world doesn't feel empty beyond the player (real art only — no
        /// placeholder-square fallback here, since a ring of grey squares wouldn't read as scenery).
        /// Positions are deterministic from GameState.Rng (seeded by BootArgs.Seed), so the same seed
        /// always produces the same layout.
        /// </summary>
        void SetupScenery()
        {
            var art = PlaceholderArt.Art;
            if (art == null) return; // catalog not baked yet

            var root = new GameObject("Scenery");
            float halfW = State.Grid.Width * 0.5f;
            float halfH = State.Grid.Height * 0.5f;

            if (art.Farmhouse != null)
                SpawnDecoration(root.transform, art.Farmhouse, new Vector3(-halfW * 0.4f, halfH + 1.8f, 0f), 0.4f);

            const int count = 16;
            for (int i = 0; i < count; i++)
            {
                float angle = i / (float)count * Mathf.PI * 2f;
                var pos = new Vector3(Mathf.Cos(angle) * (halfW + 2f), Mathf.Sin(angle) * (halfH + 2f), 0f);

                Sprite sprite; float scale;
                switch (State.Rng.Next(3))
                {
                    case 0: sprite = art.Tree;      scale = 1.1f; break;
                    case 1: sprite = art.GrassTuft;  scale = 0.6f; break;
                    default: sprite = art.Stone;     scale = 0.5f; break;
                }
                if (sprite != null) SpawnDecoration(root.transform, sprite, pos, scale);
            }
        }

        void SpawnDecoration(Transform parent, Sprite sprite, Vector3 pos, float scale)
        {
            var go = new GameObject(sprite.name);
            go.transform.parent = parent;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.transform.position = pos;
            sr.transform.localScale = new Vector3(scale, scale, 1f);
        }

        void SetupPlayer()
        {
            _player = FindFirstObjectByType<PlayerController>();
            if (_player == null)
            {
                var go = new GameObject("Player");
                _player = go.AddComponent<PlayerController>();
            }
            _player.State = State;
            _player.GameplayService = _services.GetRequiredService<IGameplayService>();
            _player.WorldService = _worldService;
        }

        void SetupHud()
        {
            var hud = FindFirstObjectByType<HUD>();
            if (hud == null)
            {
                var go = new GameObject("HUD");
                hud = go.AddComponent<HUD>();
            }
            hud.StateQuery = _services.GetRequiredService<IGameStateQuery>();
            hud.Player = _player;
        }

        void SetupToolbar()
        {
            // NavigationBar is scene-authored; only bind gameplay state to its existing component.
            var bar = FindFirstObjectByType<ToolbarCanvasUI>();
            if (bar != null)
            {
                bar.Player = _player;
                bar.InventoryService = _services.GetRequiredService<InventoryService>();
            }
        }

        void SetupInventoryScreen()
        {
            var inv = FindFirstObjectByType<InventoryScreenUI>();
            if (inv == null)
            {
                var go = new GameObject("InventoryScreenUI");
                inv = go.AddComponent<InventoryScreenUI>();
            }
            inv.State = State;
            inv.InventoryService = _services.GetRequiredService<Prototype.Domain.IInventoryService>();
            inv.InventoryReadService = _services.GetRequiredService<InventoryService>();
            inv.Player = _player;
        }

        void SetupSeedShop()
        {
            var sign = GameObject.Find("SeedShopSign");
            if (sign == null) sign = new GameObject("SeedShopSign");
            var sr = sign.GetComponent<SpriteRenderer>() ?? sign.AddComponent<SpriteRenderer>();
            var turnipData = State.MasterData.GetCrop(Prototype.Domain.CropId.Turnip);
            string turnipSeed = turnipData != null
                ? turnipData.SeedItemId
                : Prototype.Domain.CropDefinition.SeedItemId(Prototype.Domain.CropId.Turnip);
            sr.sprite = PlaceholderArt.ItemIcon(turnipSeed) ?? PlaceholderArt.WhiteSprite;
            sr.color = PlaceholderArt.ItemIcon(turnipSeed) != null
                ? PlaceholderArt.ItemTint(turnipSeed)
                : new Color(0.95f, 0.8f, 0.2f);
            var shopCoord = _worldService.SeedShopCoord(State).GetAwaiter().GetResult();
            if (!shopCoord.IsSuccess)
            {
                Debug.LogError($"Could not resolve seed shop position: {shopCoord.Failure.Message}");
                return;
            }
            sr.transform.position = State.Grid.GridToWorld(shopCoord.Value);
            sr.transform.localScale = new Vector3(0.8f, 0.8f, 1f);
            sr.sortingOrder = 4;

            var shop = FindFirstObjectByType<SeedShopUI>();
            if (shop == null)
            {
                var label = new GameObject("SeedShopUI");
                shop = label.AddComponent<SeedShopUI>();
            }
            shop.State = State;
            shop.MasterDataAsset = MasterDataAsset;
            shop.GameplayService = _services.GetRequiredService<IGameplayService>();
            shop.InventoryService = _services.GetRequiredService<Prototype.Domain.IInventoryService>();
            shop.Player = _player;
        }

        void SetupNpcDialogue()
        {
            var npc = FindFirstObjectByType<NpcDialogueController>();
            if (npc == null)
            {
                var go = new GameObject("NpcDialogueController");
                npc = go.AddComponent<NpcDialogueController>();
            }
            npc.State = State;
            npc.DialogueService = _services.GetRequiredService<IDialogueService>();
            npc.Player = _player;
        }

        void SetupDebug()
        {
            _debug = FindFirstObjectByType<DebugPanel>();
            if (_debug == null)
            {
                var go = new GameObject("DebugPanel");
                _debug = go.AddComponent<DebugPanel>();
            }
            _debug.State = State;
            _debug.ClockService = _services.GetRequiredService<IClockService>();
            _debug.InventoryService = _services.GetRequiredService<Prototype.Domain.IInventoryService>();
            _debug.GameplayService = _services.GetRequiredService<IGameplayService>();
            _debug.WorldService = _worldService;
            _debug.Player = _player;
        }

        /// <summary>Flash a feedback square over a tile (AGENT_DESIGN §5.4 Feel Matrix). Pure visual cue.</summary>
        public static void SpawnFeedback(GridCoord coord, Prototype.Domain.FeedbackKind kind)
        {
            if (Instance?.State == null) return;
            var pos = Instance.State.Grid.GridToWorld(coord);
            var go = new GameObject("Feedback");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = PlaceholderArt.WhiteSprite;
            sr.color = PlaceholderArt.FeedbackColor(kind);
            sr.transform.position = pos;
            sr.transform.localScale = new Vector3(1f, 1f, 1f);
            sr.sortingOrder = 10;
            Destroy(go, 0.4f);
        }
    }
}
