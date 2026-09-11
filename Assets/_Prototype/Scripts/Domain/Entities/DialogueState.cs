namespace Prototype.Domain
{
    /// <summary>
    /// Plain dialogue state machine: opening always starts at line 0, Interact advances, Esc closes.
    /// Runtime view/input lives in NpcDialogueController; tests exercise this directly in EditMode.
    /// </summary>
    public sealed class DialogueState
    {
        public NpcDefinition ActiveNpc { get; private set; }
        public int LineIndex { get; private set; }
        public bool IsOpen => ActiveNpc != null;
        public string CurrentLine => IsOpen ? ActiveNpc.Lines[LineIndex] : string.Empty;

        public void Open(NpcDefinition npc)
        {
            ActiveNpc = npc;
            LineIndex = 0;
        }

        /// <summary>Advances one line; closes when called on the final line.</summary>
        public void AdvanceOrClose()
        {
            if (!IsOpen) return;
            if (LineIndex + 1 >= ActiveNpc.Lines.Length) Close();
            else LineIndex++;
        }

        public void Close()
        {
            ActiveNpc = null;
            LineIndex = 0;
        }
    }
}
