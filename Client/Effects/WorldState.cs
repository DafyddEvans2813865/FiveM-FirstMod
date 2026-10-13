using System;
using System.Threading.Tasks;
using CitizenFX.Core;
using static CitizenFX.Core.Native.API;

namespace DredgeClient.Effects
{
    public static class WorldState
    {
        private static readonly Random Rng = new Random();

        // e.g. "FOGGY", "SMOG", "OVERCAST", "THUNDER"
        public static void SetFog(string weatherType)
        {
            ClearOverrideWeather();
            ClearWeatherTypePersist();
            SetWeatherTypeNowPersist(weatherType);
            SetOverrideWeather(weatherType); // stops game changing weather
        }

        public static void SetBlackout(bool enabled)
        {
            SetArtificialLightsState(enabled);
            SetArtificialLightsStateAffectsVehicles(false);
        }


        public static async Task FlickerLight(Vector3 position, int durationMs, int r = 255, int g = 220, int b = 160, float range = 15f, float intensity = 8f)
        {
            int endTime = GetGameTimer() + durationMs;
            bool on = true;
            int nextToggle = GetGameTimer();

            while (GetGameTimer() < endTime)
            {
                if (GetGameTimer() >= nextToggle)
                {
                    on = Rng.NextDouble() > 0.35; //35% chance to be off 
                    nextToggle = GetGameTimer() + Rng.Next(40, 400);
                }

                if (on)
                    DrawLightWithRange(position.X, position.Y, position.Z, r, g, b, range, intensity);

                await BaseScript.Delay(0); // light drawn every frame
            }
        }

        public static void Reset()
        {
            NetworkOverrideClockTime(12, 0, 0);
            ClearTimecycleModifier();
            SetArtificialLightsState(false);
            ClearOverrideWeather();
            ClearWeatherTypePersist();
            SetWeatherTypeNowPersist("CLEAR");
        }
        public static async Task KillEngine(int vehicle, int durationMs)
        {
            SetVehicleEngineOn(vehicle, false, true, true);
            await BaseScript.Delay(durationMs);
            SetVehicleEngineOn(vehicle, true, true, false);
        }

        public static async Task KillLights(int vehicle, int durationMs)
        {
            SetVehicleLights(vehicle, 1);
            await BaseScript.Delay(durationMs);
            SetVehicleLights(vehicle, 2);
        }
    }

}