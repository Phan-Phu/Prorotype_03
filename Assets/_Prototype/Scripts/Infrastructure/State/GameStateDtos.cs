using Prototype.Domain;
using UnityEngine;

namespace Prototype.Application
{
    /// <summary>Application command for advancing simulation time.</summary>
    public readonly struct AdvanceTimeRequest
    {
        public readonly float DeltaSeconds;

        public AdvanceTimeRequest(float deltaSeconds)
        {
            DeltaSeconds = Mathf.Max(0f, deltaSeconds);
        }
    }

    /// <summary>Read-only transport model for the clock; views do not need the domain GameClock.</summary>
    public readonly struct GameClockDto
    {
        public readonly int Day;
        public readonly int RawHour;
        public readonly int DisplayHour;

        public GameClockDto(int day, int rawHour)
        {
            Day = day;
            RawHour = rawHour;
            DisplayHour = ClockService.WrapHour(rawHour);
        }
    }

    /// <summary>Read-only application snapshot used by HUD and future presentation components.</summary>
    public sealed class GameStateSnapshotDto
    {
        public readonly GameClockDto Clock;
        public readonly int Money;
        public readonly int Stamina;
        public readonly Vector2 PlayerPosition;
        public readonly InventorySnapshot Inventory;

        public GameStateSnapshotDto(GameClockDto clock, int money, int stamina,
            Vector2 playerPosition, InventorySnapshot inventory)
        {
            Clock = clock;
            Money = money;
            Stamina = stamina;
            PlayerPosition = playerPosition;
            Inventory = inventory;
        }
    }

    public interface IGameStateQuery
    {
        GameStateSnapshotDto Read();
    }

    public interface IGameTimeUseCase
    {
        GameStateSnapshotDto Advance(AdvanceTimeRequest request);
    }
}
