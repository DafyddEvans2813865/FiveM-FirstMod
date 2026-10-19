using System;
using System.Collections.Generic;
using CitizenFX.Core;
using System.Threading.Tasks;
using static CitizenFX.Core.Native.API;

namespace DredgeClient
{
    public class InitJob : BaseScript
    {
        public InitJob()
        {
            EventHandlers["onClientResourceStart"] += new Action<string>(OnClientResourceStart);
            EventHandlers["dredge:spawnBoat"] += new Func<Task>(StartJob);
        }

        private void OnClientResourceStart(string resourceName)
        {
            if (GetCurrentResourceName() != resourceName) return;

            //TO-DO REMOVE LATER 
            RegisterCommand("Docks", new Action(async () => await ToDock()), false);
            RegisterCommand("Atmo", new Action(() => setAtmosphere()), false);
            //END OF TO-DO 

            RegisterCommand("StartDredge", new Action(async () => await StartJob()), false);
        }

        private async Task ToDock()
        {
            var Boat = await World.CreateVehicle("tug", GameState.DockPos, 120);
            if (Boat == null) return;

            Game.PlayerPed.SetIntoVehicle(Boat, VehicleSeat.Driver);

            //DEBUG -> Speed up boat a little 
            SetVehicleEnginePowerMultiplier(Boat.Handle, 10.0f);
        }

        //T0-DO change to call effects 
        private void setAtmosphere()
        {
            NetworkOverrideClockTime(2, 0, 0);
            SetWeatherTypeNowPersist("FOGGY");
            SetTimecycleModifier("nervousRON_fog");
        }

        public async Task StartJob()
        {
            setAtmosphere();
            await ToDock();
        }

    }

}