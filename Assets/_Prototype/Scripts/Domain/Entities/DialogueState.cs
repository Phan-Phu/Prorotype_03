namespace Prototype.Domain
{
    /// <summary>
    /// Raw dialogue session state. Transition rules are implemented by Infrastructure.DialogueService;
    /// this entity only stores data and exposes read-only projections.
    /// </summary>
    public sealed class DialogueState
    {
        // SET is reserved for Infrastructure state transitions or MasterData import.
        public NpcDefinition ActiveNpc { get; set; }
        // SET is reserved for Infrastructure state transitions or MasterData import.
        public int LineIndex { get; set; }
        public bool IsOpen => ActiveNpc != null;
        public string CurrentLine => IsOpen ? ActiveNpc.Lines[LineIndex] : string.Empty;
    }
}
