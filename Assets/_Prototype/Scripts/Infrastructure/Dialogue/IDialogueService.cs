using Cysharp.Threading.Tasks;
using Prototype.Domain;

namespace Prototype.Application
{
    public sealed class DialogueDto
    {
        public readonly bool IsOpen;
        public readonly string Speaker;
        public readonly string CurrentLine;
        public readonly int LineIndex;
        public readonly int LineCount;

        public DialogueDto(bool isOpen, string speaker, string currentLine, int lineIndex, int lineCount)
        {
            IsOpen = isOpen; Speaker = speaker; CurrentLine = currentLine;
            LineIndex = lineIndex; LineCount = lineCount;
        }
    }

    public interface IDialogueService
    {
        DialogueDto Read();
        DialogueDto Open(NpcDefinition npc);
        DialogueDto AdvanceOrClose();
        DialogueDto Close();
        UniTask<DialogueDto> OpenAsync(NpcDefinition npc);
        UniTask<DialogueDto> AdvanceOrCloseAsync();
    }
}
