using System;
using Cysharp.Threading.Tasks;
using Prototype.Application;
using Prototype.Domain;

namespace Prototype.Infrastructure
{
    /// <summary>Infrastructure implementation of the NPC dialogue port.</summary>
    public sealed class DialogueService : IDialogueService
    {
        readonly DialogueState _state = new DialogueState();

        public UniTask<Result<DialogueFailure, DialogueDto>> Read()
            => Safe("dialogue.read", ToDto);

        public UniTask<Result<DialogueFailure, DialogueDto>> Open(NpcDefinition npc)
        {
            if (npc == null || npc.Lines == null || npc.Lines.Length == 0)
                return ResultFactory.UniTaskFailure<DialogueFailure, DialogueDto>(DialogueFailure.InvalidNpc());
            return Safe("dialogue.open", () =>
            {
                _state.ActiveNpc = npc;
                _state.LineIndex = 0;
                return ToDto();
            });
        }

        public UniTask<Result<DialogueFailure, DialogueDto>> AdvanceOrClose()
        {
            if (!_state.IsOpen)
                return ResultFactory.UniTaskFailure<DialogueFailure, DialogueDto>(DialogueFailure.NoActiveDialogue());
            return Safe("dialogue.advance", () =>
            {
                if (_state.LineIndex + 1 >= _state.ActiveNpc.Lines.Length)
                {
                    _state.ActiveNpc = null;
                    _state.LineIndex = 0;
                }
                else
                {
                    _state.LineIndex++;
                }
                return ToDto();
            });
        }

        public UniTask<Result<DialogueFailure, DialogueDto>> Close()
            => Safe("dialogue.close", () =>
            {
                _state.ActiveNpc = null;
                _state.LineIndex = 0;
                return ToDto();
            });

        UniTask<Result<DialogueFailure, DialogueDto>> Safe(string context, Func<DialogueDto> operation)
        {
            try
            {
                return UniTask.FromResult(ResultFactory.Success<DialogueFailure, DialogueDto>(operation()));
            }
            catch (Exception exception)
            {
                UnityEngine.Debug.LogException(exception);
                return UniTask.FromResult(ResultFactory.Failure<DialogueFailure, DialogueDto>(DialogueFailure.System(context)));
            }
        }

        DialogueDto ToDto()
        {
            var npc = _state.ActiveNpc;
            return new DialogueDto(_state.IsOpen, npc?.DisplayName ?? string.Empty,
                _state.CurrentLine, _state.IsOpen ? _state.LineIndex : 0,
                npc?.Lines?.Length ?? 0);
        }
    }
}
