using System;
using System.Threading.Tasks;
using CitizenFX.Core;
using static CitizenFX.Core.Native.API;

namespace DredgeClient.Effects
{

    public class EndgameSequence : BaseScript
    {
        private float _overlayAlpha;   // 0..255
        private string _overlayText = "";

        public EndgameSequence()
        {
            EventHandlers["dredge:runEndgame"] += new Action(() =>
            {
                if (!GameState.EndgameRunning) _ = Run();
            });
            Tick += DrawOverlay;
        }

        private async Task Run()
        {
            int ped = PlayerPedId();
            int boat = IsPedInAnyBoat(ped) ? GetVehiclePedIsIn(ped, false) : 0;
            if (boat == 0) return;

            GameState.EndgameRunning = true;

            // 1. Engine dies, then the rumble builds
            _ = WorldState.KillEngine(boat, 20000);
            await CameraFx.ShakeCamera("VIBRATE_SHAKE", 1.2f, 3000);

            // 2. Lights cut + world blackout
            _ = WorldState.KillLights(boat, 20000);
            WorldState.SetBlackout(true);
            await Delay(1500);

            // 3. Fade to black
            await Fade(0f, 255f, 1500);
            StopGameplayCamShaking(true);
            FreezeEntityPosition(boat, true);

            // 4. Horn in the dark 
            StartVehicleHorn(boat, 2500, (uint)GetHashKey("HELDDOWN"), false);
            await Delay(4000);

            // 5. Game over
            _overlayText = $"The sea took you.~n~Fish lost: {GameState.FishHeld}";
            TriggerEvent("dredge:gameOver");   // DreadMeter resets; //TO-DO reset finish mini game
            await Delay(4000);

            // 6. Clean up 
            WorldState.Reset();
            Spawner.ClearAll();

            ClearPedTasksImmediately(ped);       //player out before deleting 
            SetEntityAsMissionEntity(boat, true, true);
            DeleteVehicle(ref boat);

            TriggerEvent("dredge:spawnBoat");
            await Delay(1000);

            _overlayText = "";
            await Fade(255f, 0f, 1500);
            GameState.EndgameRunning = false;
        }

        private async Task Fade(float from, float to, int ms)
        {
            int start = GetGameTimer();
            while (true)
            {
                float t = Math.Min((GetGameTimer() - start) / (float)ms, 1f);
                _overlayAlpha = from + (to - from) * t;
                if (t >= 1f) break;
                await Delay(0);
            }
        }

        private Task DrawOverlay()
        {
            if (GameState.EndgameRunning) DisableAllControlActions(0);

            if (_overlayAlpha > 0f)
            {
                HideHudAndRadarThisFrame();
                Hud.Rect(0.5f, 0.5f, 1f, 1f, 0, 0, 0, (int)_overlayAlpha);
                if (_overlayText.Length > 0) Hud.Text(_overlayText, 0.5f, 0.45f, 0.7f);
            }
            return Task.FromResult(0);
        }
    }
}