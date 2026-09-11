namespace Prototype.Domain
{
    /// <summary>
    /// Single source of truth for all prototype balance numbers.
    /// Tagged [DESIGN-OWNED] = do NOT edit without a ticket from the Design Agent.
    /// Source: AGENT_DESIGN.md mục 5.3 (Bảng số liệu khởi điểm).
    /// </summary>
    public static class BalanceConfig
    {
        // === PLAYER ===
        public const float MoveSpeed    = 4.0f;   // tiles/sec              [DESIGN-OWNED]
        public const int   MaxStamina   = 100;    //                        [DESIGN-OWNED]
        public const int   StartMoney   = 500;    //                        [DESIGN-OWNED]

        // === TIME ===
        public const float SecondsPerInGameHour = 40f; // ~13 min real/day [DESIGN-OWNED]
        public const int   DayStartHour = 6;     // 06:00                  [DESIGN-OWNED]
        public const int   DayEndHour   = 26;    // 02:00 next day         [DESIGN-OWNED]

        // === TOOL COST (stamina) ===
        public const int TillStaminaCost     = 2;   // [DESIGN-OWNED]
        public const int WaterStaminaCost    = 1;   // [DESIGN-OWNED]
        public const int HarvestStaminaCost  = 0;   // [DESIGN-OWNED]

        // === CROP: TURNIP ===
        public const int TurnipSeedPrice   = 20;  // [DESIGN-OWNED]
        public const int TurnipSellPrice   = 60;  // [DESIGN-OWNED]
        public const int TurnipGrowthDays  = 4;   // [DESIGN-OWNED]

        // === CROP: POTATO ===
        public const int PotatoSeedPrice   = 50;  // [DESIGN-OWNED]
        public const int PotatoSellPrice   = 160; // [DESIGN-OWNED]
        public const int PotatoGrowthDays  = 6;   // [DESIGN-OWNED]

        // === TREE / WOOD ===
        // Source: DESIGN_BRIEFS.md [DSN-030] (S2-DES-01/02). Chặt gỗ là hoạt động PHỤ —
        // mọi số dưới đây bị ràng buộc bởi luật bất biến: WoodProfitPerDay phải <= 70% của
        // PotatoProfitPerDay, để Potato luôn giữ vai trò nguồn thu chính (xem rule + phép tính
        // đầy đủ trong DESIGN_BRIEFS.md). Đổi bất kỳ số nào ở đây phải tính lại rule đó trước.
        public const int TreeMaxHP          = 3; // số nhát rìu để hạ 1 cây          [DESIGN-OWNED]
        public const int ChopStaminaCost    = 4; // stamina/nhát, cố tình nặng hơn Till/Water để
                                                  // chặt gỗ không âm thầm là lựa chọn stamina-hiệu-quả nhất [DESIGN-OWNED]
        public const int WoodPerTree        = 5; // gỗ nhận được khi hạ xong 1 cây    [DESIGN-OWNED]
        public const int WoodSellPrice      = 8; // giá bán mỗi đơn vị gỗ            [DESIGN-OWNED]
        public const int TreeRespawnDays    = 4; // gốc cây mọc lại sau N ngày, cố tình bằng
                                                  // TurnipGrowthDays để lồng vào nhịp trồng trọt sẵn có [DESIGN-OWNED]
        public const int InitialTreeCount   = 8; // số cây có sẵn trên map lúc bắt đầu, hữu hạn
                                                  // có chủ đích — không phải vòi nước bất tận  [DESIGN-OWNED]
    }
}
