using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CitizenFX.Core;
using static CitizenFX.Core.Native.API;

namespace DredgeClient.Effects
{
    public enum DreadLevel
    {
        Calm = 1,
        Uneasy = 2,
        Fearful = 3,
        Doom = 4
    }

    public class EffectsManager : BaseScript
    {
        private class ScareEvent
        {
            public string Name;
            public int Weight;           // higher -> more often
            public DreadLevel MinLevel;
            public Func<int, Task> Run;
        }

        private readonly Random _rng = new Random();
        private readonly List<ScareEvent> _events;
        private bool _enabled = true;
        private bool _running;
        private bool _endgame;
        private string _lastEvent;

        public static DreadLevel CurrentLevel { get; private set; } = DreadLevel.Calm;

        // Chance per tick of the endgame firing while at Doom
        private const double EndgameChance = 0.3;

        // Gap between scares
        private const int MinGap = 20000;
        private const int MaxGap = 60000;
        private const float TugZAdjust = -0.5f; //ghost boat offset (origin is funky)

        public EffectsManager()
        {
            _events = BuildEvents();
            Tick += OnTick;

            EventHandlers["dredge:dreadChanged"] += new Action<float>(OnDreadChanged);
            RegisterDebugCommands();
        }

        private static readonly string[] RadioStations =
        {
            "RADIO_01_CLASS_ROCK", "RADIO_02_POP", "RADIO_03_HIPHOP_NEW", "RADIO_04_PUNK",
            "RADIO_05_TALK_01", "RADIO_06_COUNTRY", "RADIO_07_DANCE_01", "RADIO_08_MEXICAN",
            "RADIO_09_HIPHOP_OLD", "RADIO_12_REGGAE", "RADIO_13_JAZZ", "RADIO_15_MOTOWN"
        };

        private List<ScareEvent> BuildEvents()
        {
            return new List<ScareEvent>
            {
                //Level 1
                new ScareEvent { Name = "debris", Weight = 5, MinLevel = DreadLevel.Calm, Run = boat => Spawner.SpawnAndMove("prop_barrel_02a", Offset(boat, 15, 40), Offset(boat, 15, -40), 1.5f) },
                new ScareEvent { Name = "shorelight", Weight = 4, MinLevel = DreadLevel.Calm, Run = boat => WorldState.FlickerLight(Offset(boat, 0, 120, 5), 10000, range: 40f, intensity: 15f) },
                new ScareEvent { Name = "jolt", Weight = 4, MinLevel = DreadLevel.Calm, Run = boat => Task.WhenAll(CameraFx.ShakeCamera("JOLT_SHAKE", 1.5f, 600),BoatFx.Jolt(boat, 4f)) },
 
                //Level 2
                
                new ScareEvent { Name = "ghostboat", Weight = 3, MinLevel = DreadLevel.Uneasy, Run = boat => Spawner.SpawnAndMove("tug", OnWater(Offset(boat, -150, 150), TugZAdjust),OnWater(Offset(boat, 150, 150), TugZAdjust), 4f) },
                new ScareEvent { Name = "enginecut", Weight = 3, MinLevel = DreadLevel.Uneasy, Run = boat =>
                {
                    PlaySoundFromEntity(-1, "Engine_fail", boat, "DLC_PILOT_ENGINE_FAILURE_SOUNDS", false, 0); // untested sound, swap if silent
                    return Task.WhenAll(
                        CameraFx.ShakeCamera("JOLT_SHAKE", 0.6f, 400),
                        WorldState.KillEngine(boat, 3000));
                }},

                new ScareEvent { Name = "lightscut", Weight = 3, MinLevel = DreadLevel.Uneasy, Run = boat => WorldState.KillLights(boat, 4000) },
                new ScareEvent { Name = "tremor", Weight = 2, MinLevel = DreadLevel.Uneasy, Run = boat => Task.WhenAll(
                    CameraFx.ShakeCamera("VIBRATE_SHAKE", 0.8f, 3000),
                    BoatFx.Rock(boat, 3000, 1.2f)) },
 
                //Level 3
                new ScareEvent { Name = "blackout", Weight = 2, MinLevel = DreadLevel.Fearful, Run = async boat =>
                {
                    WorldState.SetBlackout(true);
                    await Task.WhenAll(
                        ScreenFx.Dim(8000, 200),
                        WorldState.KillLights(boat, 8000));
                    WorldState.SetBlackout(false);
                }},

                new ScareEvent { Name = "radio", Weight = 3, MinLevel = DreadLevel.Uneasy, Run = async boat =>
                {
                    SetVehicleRadioEnabled(boat, true);
                    int hops = _rng.Next(4, 8);
                    for (int i = 0; i < hops; i++)
                    {
                        SetVehRadioStation(boat, RadioStations[_rng.Next(RadioStations.Length)]);
                        await Delay(_rng.Next(250, 600));
                    }
                }},
 
                // hull hit + engine dies
                new ScareEvent { Name = "impact", Weight = 2, MinLevel = DreadLevel.Fearful, Run = boat => Task.WhenAll(
                    CameraFx.ShakeCamera("MEDIUM_EXPLOSION_SHAKE", 1f, 800),
                    BoatFx.Jolt(boat, 8f),
                    WorldState.KillEngine(boat, 4000)) },
 
                //Level 4
                // blackout + something out there
                new ScareEvent { Name = "darkness", Weight = 2, MinLevel = DreadLevel.Doom, Run = async boat =>
                {
                    WorldState.SetBlackout(true);
                    await Task.WhenAll(
                        ScreenFx.Dim(9000, 225),
                        WorldState.KillLights(boat, 9000),
                        WorldState.KillEngine(boat, 9000),
                        WorldState.FlickerLight(Offset(boat, -15, 35, 2), 8000, 120, 255, 140, 25f, 30f)); // sickly green
                    WorldState.SetBlackout(false);
                }},
            };
        }

        private void OnDreadChanged(float dread)
        {
            SetLevel(LevelFromDread(dread));
        }

        private static DreadLevel LevelFromDread(float dread)
        {
            if (dread >= 85f) return DreadLevel.Doom;
            if (dread >= 55f) return DreadLevel.Fearful;
            if (dread >= 25f) return DreadLevel.Uneasy;
            return DreadLevel.Calm;
        }

        private void SetLevel(DreadLevel level)
        {
            if (level == CurrentLevel) return;
            CurrentLevel = level;

            if (level < DreadLevel.Doom) _endgame = false;
            Debug.WriteLine($"[Effects] dread level -> {level}");
        }

        private async Task OnTick()
        {
            await Delay(_rng.Next(MinGap, MaxGap));

            if (!_enabled || _running || _endgame) return;

            int boat = GetPlayerBoat();
            if (boat == 0) return; // only while on boat

            if (CurrentLevel == DreadLevel.Doom && _rng.NextDouble() < EndgameChance)
            {
                _endgame = true;
                TriggerEvent("dredge:runEndgame");
                return;
            }

            var ev = PickWeighted(CurrentLevel);
            if (ev != null) await Fire(ev, boat);
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

        private ScareEvent PickWeighted(DreadLevel level)
        {
            var eligible = _events.Where(e => e.MinLevel <= level).ToList();
            if (eligible.Count == 0) return null;

            var pool = eligible.Where(e => e.Name != _lastEvent).ToList();
            if (pool.Count == 0) pool = eligible;

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

        // Attempt to make ghost boat less buggy 
        private static Vector3 OnWater(Vector3 pos, float zAdjust = 0f)
        {
            float h = 0f;
            if (GetWaterHeight(pos.X, pos.Y, pos.Z + 5f, ref h))
                pos.Z = h + zAdjust;
            return pos;
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
                    : PickWeighted(CurrentLevel);

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

            RegisterCommand("clearDredge", new Action<int, List<object>, string>((src, args, raw) =>
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