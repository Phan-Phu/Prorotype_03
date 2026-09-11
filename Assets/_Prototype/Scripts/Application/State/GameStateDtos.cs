using Prototype.Domain;
using UnityEngine;

namespace Prototype.Application
{
    public readonly struct AdvanceTimeRequest
    {
        public readonly float DeltaSeconds;

        public AdvanceTimeRequest(float deltaSeconds)
        {
            DeltaSeconds = Mathf.Max(0f, deltaSeconds);
        }
    }

    public readonly struct GameClockDto
    {
        public readonly int Day;
        public readonly int RawHour;
        public readonly int DisplayHour;

        public GameClockDto(int day, int rawHour)
        {
            Day = day;
            RawHour = rawHour;
            DisplayHour = ((rawHour % 24) + 24) % 24;
        }
    }

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
            Clock = clock; Money = money; Stamina = stamina;
            PlayerPosition = playerPosition; Inventory = inventory;
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
