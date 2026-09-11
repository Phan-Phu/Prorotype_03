namespace Prototype.Domain
{
    public enum ToolResultCode { Success, WrongTool, NoStamina, InvalidTile, NoSeed, NotChoppable, Blocked, InventoryFull }

    public enum FeedbackKind { Till, Plant, Water, Harvest, Chop, Miss }

    /// <summary>Domain result returned by a tool service; presentation maps it to visual feedback.</summary>
    public readonly struct ToolResult
    {
        public readonly ToolResultCode Code;
        public readonly FeedbackKind Feedback;
        public readonly int Amount;

        public ToolResult(ToolResultCode code, FeedbackKind feedback, int amount = 0)
        {
            Code = code;
            Feedback = feedback;
            Amount = amount;
        }

        public bool IsSuccess => Code == ToolResultCode.Success;
        public static ToolResult Success(FeedbackKind feedback, int amount = 0)
            => new ToolResult(ToolResultCode.Success, feedback, amount);
        public static ToolResult Fail(ToolResultCode code, FeedbackKind feedback)
            => new ToolResult(code, feedback, 0);
    }
}
