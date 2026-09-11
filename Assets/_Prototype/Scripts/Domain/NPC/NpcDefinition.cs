using Prototype.Domain;
using UnityEngine;

namespace Prototype.Domain
{
    /// <summary>
    /// Static NPC data for DEV-041. Plain C# so tests can assert dialogue/positions without a scene.
    /// These actors intentionally do not have schedule/quests/branching in the prototype slice.
    /// </summary>
    public sealed class NpcDefinition
    {
        public readonly string Id;
        public readonly string DisplayName;
        public readonly GridCoord Coord;
        public readonly string[] Lines;
        public readonly Color FallbackColor;

        public NpcDefinition(string id, string displayName, GridCoord coord, string[] lines, Color fallbackColor)
        {
            Id = id;
            DisplayName = displayName;
            Coord = coord;
            Lines = lines;
            FallbackColor = fallbackColor;
        }

        public Vector3 WorldPosition(GridMap grid) => grid.GridToWorld(Coord);
    }

    public static class NpcDefinitions
    {
        public const float InteractionRadiusTiles = 1.25f;

        public static readonly NpcDefinition Cora = new NpcDefinition(
            "cora",
            "Cora",
            new GridCoord(8, 17),
            new[]
            {
                "Chào buổi sáng! Ruộng nhỏ cũng thành chuyện lớn nếu ngày nào cũng chăm.",
                "Cây đã gieo thì nhớ tưới trước khi ngủ nhé. Đất khô là cây đứng yên đấy.",
                "Nếu hôm nay thu hoạch được, thử nghĩ xem mai sẽ trồng thêm ô nào.",
                "Tôi thích nhìn mảnh ruộng đổi màu sau mỗi lần cuốc — rất dễ biết mình đã làm được gì."
            },
            new Color(0.9f, 0.55f, 0.95f));

        public static readonly NpcDefinition Butch = new NpcDefinition(
            "butch",
            "Butch",
            new GridCoord(17, 12),
            new[]
            {
                "Thấy mấy cây phía kia không? Chặt vài cây là có thêm việc trong lúc chờ mùa vụ lớn.",
                "Rìu tốn sức hơn cuốc, nên đừng quên giữ stamina cho ruộng trước.",
                "Gốc cây sẽ mọc lại sau vài ngày. Tôi hay quay lại đúng lúc vụ củ cải tới kỳ.",
                "Gỗ bán được, nhưng đừng bỏ ruộng chỉ vì vài khúc củi nhanh tay."
            },
            new Color(0.75f, 0.36f, 0.18f));

        public static readonly NpcDefinition[] All = { Cora, Butch };
    }
}
