using Prototype.Domain;
using Prototype.Application;
using UnityEngine;
using UnityEngine.UI;

namespace Prototype.Application
{
    /// <summary>
    /// Runtime-only view/controller for DEV-041 static NPCs. It owns only presentation and keyboard
    /// input; dialogue content/positions live in plain C# NpcDefinition/DialogueState for CLI tests.
    /// </summary>
    public class NpcDialogueController : PopupBase
    {
        public GameState State;
        public IDialogueService DialogueService;
        public PlayerController Player;

        [Header("DialoguePopup UI")]
        [SerializeField] RectTransform _dialoguePopup;
        [SerializeField] Image _dialoguePanel;
        [SerializeField] Image _portraitImage;
        [SerializeField] Text _speakerText;
        [SerializeField] Text _dialogueText;
        [SerializeField] Text _advanceText;

        private NpcDefinition _nearby;

        NpcDefinition[] Npcs => State?.MasterData?.Npcs ?? NpcDefinitions.All;

        public bool IsDialogueOpen => ReadDialogue()?.IsOpen == true;

        protected override void Awake()
        {
            base.Awake();
            if (_dialoguePopup == null || _dialoguePanel == null || _portraitImage == null ||
                _speakerText == null || _dialogueText == null || _advanceText == null)
                Debug.LogWarning("NpcDialogueController is missing serialized DialoguePopup references. Assign them in the scene.");

            SetDialogueVisible(false);
            SetDialogueTextColor(new Color32(42, 30, 20, 255));
            if (_portraitImage != null)
            {
                _portraitImage.preserveAspect = true;
                _portraitImage.color = Color.white;
            }
        }

        void Start()
        {
            if (State == null) return;
            var root = new GameObject("NPCs");
            foreach (var npc in Npcs)
                SpawnNpc(root.transform, npc);
        }

        void Update()
        {
            if (State == null || Player == null) return;

            _nearby = FindNearestNpcInRange();

            var dialogue = ReadDialogue();
            if (dialogue != null && dialogue.IsOpen)
            {
                Player.SetGameplayLocked(true);
                UpdateDialogueView();
                if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
                    DialogueService.AdvanceOrClose().GetAwaiter().GetResult();
                if (Input.GetKeyDown(KeyCode.Escape))
                    DialogueService.Close().GetAwaiter().GetResult();
                if (ReadDialogue()?.IsOpen != true)
                {
                    SetDialogueVisible(false);
                    Player.SetGameplayLocked(false);
                }
                return;
            }

            SetDialogueVisible(false);
            Player.SetGameplayLocked(false);
            if (_nearby != null && Input.GetKeyDown(KeyCode.E))
            {
                if (DialogueService != null)
                    DialogueService.Open(_nearby).GetAwaiter().GetResult();
                Player.SetGameplayLocked(true);
                UpdateDialogueView();
            }
        }

        void OnDisable()
        {
            if (Player != null) Player.SetGameplayLocked(false);
            SetDialogueVisible(false);
        }

        void SpawnNpc(Transform parent, NpcDefinition npc)
        {
            var go = new GameObject($"NPC_{npc.DisplayName}");
            go.transform.parent = parent;
            go.transform.position = npc.WorldPosition(State.Grid);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteFor(npc) ?? PlaceholderArt.WhiteSprite;
            sr.color = SpriteFor(npc) != null ? Color.white : npc.FallbackColor;
            sr.sortingOrder = 4;
            sr.transform.localScale = new Vector3(0.8f, 0.8f, 1f);
        }

        NpcDefinition FindNearestNpcInRange()
        {
            Vector3 playerPos = Player.transform.position;
            NpcDefinition best = null;
            float radius = State?.MasterData?.NpcInteractionRadiusTiles ?? NpcDefinitions.InteractionRadiusTiles;
            float bestSq = radius * radius;
            foreach (var npc in Npcs)
            {
                float sq = (npc.WorldPosition(State.Grid) - playerPos).sqrMagnitude;
                if (sq <= bestSq)
                {
                    bestSq = sq;
                    best = npc;
                }
            }
            return best;
        }

        void UpdateDialogueView()
        {
            var dialogue = ReadDialogue();
            if (_dialoguePanel == null || dialogue == null || !dialogue.IsOpen || _nearby == null) return;

            var npc = _nearby;
            SetDialogueVisible(true);
            if (_speakerText != null) _speakerText.text = dialogue.Speaker;
            if (_dialogueText != null) _dialogueText.text = dialogue.CurrentLine;
            if (_advanceText != null)
                _advanceText.text = $"E tiếp ({dialogue.LineIndex + 1}/{dialogue.LineCount}) | Esc đóng";

            if (_portraitImage != null)
            {
                _portraitImage.sprite = PortraitFor(npc);
                _portraitImage.preserveAspect = true;
                _portraitImage.color = Color.white;
            }
        }

        protected override RectTransform ResolvePopupTarget()
        {
            return _dialoguePopup;
        }

        void SetDialogueVisible(bool visible)
        {
            if (_dialoguePanel == null) return;
            if (visible) ShowPopup();
            else HidePopup();
        }

        void SetDialogueTextColor(Color color)
        {
            if (_speakerText != null) _speakerText.color = color;
            if (_dialogueText != null) _dialogueText.color = color;
            if (_advanceText != null) _advanceText.color = color;
        }

        DialogueDto ReadDialogue()
        {
            if (DialogueService == null) return null;
            var result = DialogueService.Read().GetAwaiter().GetResult();
            return result.IsSuccess ? result.Value : null;
        }

        static Sprite SpriteFor(NpcDefinition npc)
        {
            var art = PlaceholderArt.Art;
            if (art == null) return null;
            return npc.Id == "cora" ? art.CoraSprite : art.ButchSprite;
        }

        static Sprite PortraitFor(NpcDefinition npc)
        {
            var art = PlaceholderArt.Art;
            if (art == null) return null;
            return npc.Id == "cora" ? art.CoraPortrait : art.ButchPortrait;
        }
    }
}
