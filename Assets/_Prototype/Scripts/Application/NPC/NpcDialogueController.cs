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
    public class NpcDialogueController : MonoBehaviour
    {
        public GameState State;
        public PlayerController Player;

        private readonly DialogueState _dialogue = new DialogueState();
        private NpcDefinition _nearby;
        private Image _dialoguePanel;
        private Image _portraitImage;
        private Text _speakerText;
        private Text _dialogueText;
        private Text _advanceText;

        public bool IsDialogueOpen => _dialogue.IsOpen;

        void Awake()
        {
            // DialoguePanel is authored in the scene. Runtime only binds content/state to it.
            _dialoguePanel = FindChild<Image>("DialoguePanel");
            _portraitImage = FindChild<Image>("DialoguePanel/Portrait");
            _speakerText = FindChild<Text>("DialoguePanel/SpeakerText");
            _dialogueText = FindChild<Text>("DialoguePanel/DialogueText");
            _advanceText = FindChild<Text>("DialoguePanel/AdvanceText");

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
            foreach (var npc in NpcDefinitions.All)
                SpawnNpc(root.transform, npc);
        }

        void Update()
        {
            if (State == null || Player == null) return;

            _nearby = FindNearestNpcInRange();

            if (_dialogue.IsOpen)
            {
                Player.SetGameplayLocked(true);
                UpdateDialogueView();
                if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
                    _dialogue.AdvanceOrClose();
                if (Input.GetKeyDown(KeyCode.Escape))
                    _dialogue.Close();
                if (!_dialogue.IsOpen)
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
                _dialogue.Open(_nearby);
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
            float bestSq = NpcDefinitions.InteractionRadiusTiles * NpcDefinitions.InteractionRadiusTiles;
            foreach (var npc in NpcDefinitions.All)
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

        void OnGUI()
        {
            // The authored Canvas is the source of truth. Keep IMGUI only as a safe fallback
            // for an older scene that has not yet been authored with DialoguePanel.
            if (_dialoguePanel != null) return;

            var oldContentColor = GUI.contentColor;
            GUI.contentColor = new Color32(42, 30, 20, 255);

            if (_dialogue.IsOpen)
            {
                DrawDialogueBox(_dialogue.ActiveNpc, _dialogue.CurrentLine, _dialogue.LineIndex + 1, _dialogue.ActiveNpc.Lines.Length);
                return;
            }

            if (_nearby != null)
            {
                GUI.Box(ResponsiveUILayout.NpcPromptRect(Screen.width, Screen.height), $"[E] Nói chuyện với {_nearby.DisplayName}");
            }
            GUI.contentColor = oldContentColor;
        }

        void UpdateDialogueView()
        {
            if (_dialoguePanel == null || _dialogue.ActiveNpc == null) return;

            var npc = _dialogue.ActiveNpc;
            SetDialogueVisible(true);
            if (_speakerText != null) _speakerText.text = npc.DisplayName;
            if (_dialogueText != null) _dialogueText.text = _dialogue.CurrentLine;
            if (_advanceText != null)
                _advanceText.text = $"E tiếp ({_dialogue.LineIndex + 1}/{npc.Lines.Length}) | Esc đóng";

            if (_portraitImage != null)
            {
                _portraitImage.sprite = PortraitFor(npc);
                _portraitImage.preserveAspect = true;
                _portraitImage.color = Color.white;
            }
        }

        void SetDialogueVisible(bool visible)
        {
            if (_dialoguePanel != null && _dialoguePanel.gameObject.activeSelf != visible)
                _dialoguePanel.gameObject.SetActive(visible);
        }

        void SetDialogueTextColor(Color color)
        {
            if (_speakerText != null) _speakerText.color = color;
            if (_dialogueText != null) _dialogueText.color = color;
            if (_advanceText != null) _advanceText.color = color;
        }

        T FindChild<T>(string path) where T : Component
        {
            var child = transform.Find(path);
            return child != null ? child.GetComponent<T>() : null;
        }

        void DrawDialogueBox(NpcDefinition npc, string line, int lineNumber, int lineCount)
        {
            Rect box = ResponsiveUILayout.DialogueBoxRect(Screen.width, Screen.height);

            var bg = PlaceholderArt.Art?.DialogueBox;
            if (bg != null) PlaceholderArt.DrawSprite(box, bg);
            else GUI.Box(box, string.Empty);

            Rect portrait = new Rect(box.x + 24f, box.y + 24f, 96f, 96f);
            var portraitSprite = PortraitFor(npc);
            if (portraitSprite != null) PlaceholderArt.DrawSprite(portrait, portraitSprite);
            else
            {
                GUI.color = npc.FallbackColor;
                GUI.Box(portrait, string.Empty);
                GUI.color = Color.white;
            }

            GUI.Label(new Rect(box.x + 136f, box.y + 20f, 220f, 28f), npc.DisplayName);
            GUI.Label(new Rect(box.x + 136f, box.y + 52f, box.width - 166f, 64f), line);
            GUI.Label(new Rect(box.x + box.width - 170f, box.y + box.height - 32f, 150f, 24f), $"E tiếp ({lineNumber}/{lineCount}) | Esc đóng");
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
