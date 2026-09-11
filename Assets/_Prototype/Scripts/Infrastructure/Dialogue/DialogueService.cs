using Cysharp.Threading.Tasks;
using Prototype.Domain;

namespace Prototype.Application
{
    /// <summary>Infrastructure state adapter for the dialogue interaction flow.</summary>
    public sealed class DialogueService : IDialogueService
    {
        readonly DialogueState _state = new DialogueState();

        public DialogueDto Read() => ToDto();

        public DialogueDto Open(NpcDefinition npc)
        {
            _state.Open(npc);
            return ToDto();
        }

        public DialogueDto AdvanceOrClose()
        {
            _state.AdvanceOrClose();
            return ToDto();
        }

        public DialogueDto Close()
        {
            _state.Close();
            return ToDto();
        }

        public UniTask<DialogueDto> OpenAsync(NpcDefinition npc)
            => UniTask.FromResult(Open(npc));

        public UniTask<DialogueDto> AdvanceOrCloseAsync()
            => UniTask.FromResult(AdvanceOrClose());

        DialogueDto ToDto()
        {
            var npc = _state.ActiveNpc;
            return new DialogueDto(_state.IsOpen, npc?.DisplayName ?? string.Empty,
                _state.CurrentLine, _state.IsOpen ? _state.LineIndex : 0,
                npc?.Lines?.Length ?? 0);
        }
    }
}
