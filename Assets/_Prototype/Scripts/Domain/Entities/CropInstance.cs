namespace Prototype.Domain
{
    /// <summary>
    /// One planted crop. Plain C# so growth rules run in EditMode tests / headless sim.
    /// Design rule (AGENT_DESIGN §5.4): a crop only advances on days it was watered.
    /// </summary>
    public class CropInstance
    {
        public CropId Id;
        public int DaysGrown;
        public bool WateredToday;

        public CropInstance(CropId id) { Id = id; }

        public void AdvanceDay()
        {
            if (WateredToday) DaysGrown++;
            WateredToday = false;
        }

        public bool IsRipe => DaysGrown >= CropDefinition.GrowthDays(Id);
    }
}
