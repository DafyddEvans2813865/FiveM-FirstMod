using System;
using System.Threading.Tasks;
using CitizenFX.Core;
using static CitizenFX.Core.Native.API;

namespace DredgeClient.Effects
{
    // Physically pushes the boat around.     
    public static class BoatFx
    {
        private static readonly Random Rng = new Random();


        public static Task Jolt(int boat, float strength)
        {
            if (!DoesEntityExist(boat)) return Task.FromResult(0);

            float side = Rng.Next(2) == 0 ? -1.5f : 1.5f;
            ApplyForceToEntity(boat, 1,
                0f, 0f, strength,      // force (up, relative to boat)
                side, 1f, 0f,          // applied off-centre so it rolls
                0, true, true, true, false, true);
            return Task.FromResult(0);
        }

        // Rocks side to side for the duration
        public static async Task Rock(int boat, int durationMs, float strength)
        {
            int end = GetGameTimer() + durationMs;
            float side = 1.5f;
            while (GetGameTimer() < end && DoesEntityExist(boat))
            {
                ApplyForceToEntity(boat, 1,
                    0f, 0f, strength,
                    side, 0f, 0f,
                    0, true, true, true, false, true);
                side = -side;
                await BaseScript.Delay(180);
            }
        }
    }
}
