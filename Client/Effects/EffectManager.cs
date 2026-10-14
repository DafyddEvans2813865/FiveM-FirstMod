using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CitizenFX.Core;
using static CitizenFX.Core.Native.API;

namespace DredgeClient.Effects
{
    public class EffectsManager : BaseScript
    {
        private class ScareEvent
        {
            public string Name;
            public int Weight;           // higher -> more often
            public int dredge; //level of dread to trigger this event (0 = any, 100 = only at max dread)
            public Func<int, Task> Run;
        }

        private readonly Random _rng = new Random();
        private readonly List<ScareEvent> _events;
        private bool _enabled = true;
        private bool _running;
        private string _lastEvent;

        // Gap between scares
        private const int MinGap = 20000;
        private const int MaxGap = 60000;

        public EffectsManager()
        {
            _events = BuildEvents();
            Tick += OnTick;
            RegisterDebugCommands();
        }

        private List<ScareEvent> BuildEvents()
        {
            return new List<ScareEvent>
            {
                new ScareEvent { Name = "ghostboat", Weight = 3, Run = boat => Spawner.SpawnAndMove("tug", Offset(boat, -150, 150), Offset(boat, 150, 150), 4f) },
                new ScareEvent { Name = "debris", Weight = 5, Run = boat => Spawner.SpawnAndMove("prop_barrel_02a", Offset(boat, 15, 40), Offset(boat, 15, -40), 1.5f) },

                new ScareEvent { Name = "shorelight", Weight = 4, Run = boat => WorldState.FlickerLight(Offset(boat, 0, 120, 5), 10000, range: 40f, intensity: 15f) },

                new ScareEvent { Name = "blackout", Weight = 2, Run = async boat =>
                {
                    WorldState.SetBlackout(true);
                    await Delay(8000);
                    WorldState.SetBlackout(false);
                }},

                new ScareEvent { Name = "enginecut", Weight = 3, Run = boat => WorldState.KillEngine(boat, 6000) },
                new ScareEvent { Name = "lightscut", Weight = 3, Run = boat => WorldState.KillLights(boat, 4000) },
 
                // blackout + something out there
                new ScareEvent { Name = "darkness", Weight = 1, Run = async boat =>
                {
                    WorldState.SetBlackout(true);
                    await Task.WhenAll(
                        WorldState.FlickerLight(Offset(boat, -40, 100, 3), 8000, 120, 255, 140, 30f, 12f), // sickly green
                        WorldState.KillEngine(boat, 6000));
                    WorldState.SetBlackout(false);
                }},

                new ScareEvent { Name = "jolt", Weight = 4, Run = boat => CameraFx.ShakeCamera("JOLT_SHAKE", 1.5f, 1000) },
                new ScareEvent { Name = "tremor", Weight = 2, Run = boat => CameraFx.ShakeCamera("VIBRATE_SHAKE", 0.8f, 5000) },      

                // hull hit + engine dies
                new ScareEvent { Name = "impact", Weight = 2, Run = boat => Task.WhenAll( CameraFx.ShakeCamera("MEDIUM_EXPLOSION_SHAKE", 1f, 800), WorldState.KillEngine(boat, 4000)) },

            };
        }

        private async Task OnTick()
        {
            await Delay(_rng.Next(MinGap, MaxGap));

            if (!_enabled || _running) return;

            int boat = GetPlayerBoat();
            if (boat == 0) return; // only while on boat

            await Fire(PickWeighted(), boat);
        }

        private async Task Fire(ScareEvent ev, int boat)
        {
            _running = true;
            _lastEvent = ev.Name;
            Debug.WriteLine($"[Effects] {ev.Name}");
            try { await ev.Run(boat); }
            catch (Exception e) { Debug.WriteLine($"[Effects] {ev.Name} failed: {e.Message}"); }
            _running = false;
        }

        private ScareEvent PickWeighted()
        {
            var pool = _events.Where(e => e.Name != _lastEvent).ToList();
            if (pool.Count == 0) pool = _events;

            int roll = _rng.Next(pool.Sum(e => e.Weight));
            foreach (var e in pool)
            {
                if (roll < e.Weight) return e;
                roll -= e.Weight;
            }
            return pool[0];
        }

        private static int GetPlayerBoat()
        {
            int ped = PlayerPedId();
            return IsPedInAnyBoat(ped) ? GetVehiclePedIsIn(ped, false) : 0;
        }

        private static Vector3 Offset(int boat, float x, float y, float z = 0f)
        {
            return GetOffsetFromEntityInWorldCoords(boat, x, y, z);
        }

        // DEBUG 
        // /scare <name>  fires scare 
        // /scares        toggles random scares 
        private void RegisterDebugCommands()
        {
            RegisterCommand("scare", new Action<int, List<object>, string>(async (src, args, raw) =>
            {
                int boat = GetPlayerBoat();
                if (boat == 0) { Debug.WriteLine("[Effects] get in a boat first"); return; }

                var ev = args.Count > 0
                    ? _events.FirstOrDefault(e => e.Name == args[0].ToString())
                    : PickWeighted();

                if (ev == null)
                {
                    Debug.WriteLine("[Effects] options: " + string.Join(", ", _events.Select(e => e.Name)));
                    return;
                }
                await Fire(ev, boat);
            }), false);

            RegisterCommand("scares", new Action<int, List<object>, string>((src, args, raw) =>
            {
                _enabled = !_enabled;
                Debug.WriteLine($"[Effects] random scares {(_enabled ? "ON" : "OFF")}");
            }), false);

            RegisterCommand("clear", new Action<int, List<object>, string>((src, args, raw) =>
            {
                WorldState.Reset();
                StopGameplayCamShaking(true);

                int boat = GetPlayerBoat();
                if (boat != 0)
                {
                    SetVehicleEngineOn(boat, true, true, false);
                    SetVehicleLights(boat, 0);
                }

                Spawner.ClearAll();
                _running = false;
                Debug.WriteLine("[Effects] cleared");
            }), false);
        }

    }
}