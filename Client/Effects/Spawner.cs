using System.Collections.Generic;
using System.Threading.Tasks;
using CitizenFX.Core;
using static CitizenFX.Core.Native.API;

namespace DredgeClient.Effects
{
    public static class Spawner
    {

        private static readonly List<int> Spawned = new List<int>();

        public static async Task<int> Spawn(string model, Vector3 position, float heading = 0f)
        {

            //check model exists
            uint hash = (uint)GetHashKey(model);
            if (!IsModelInCdimage(hash)) return 0;

            RequestModel(hash);

            int timeout = GetGameTimer() + 5000;
            while (!HasModelLoaded(hash))
            {
                if (GetGameTimer() > timeout) return 0; // gave up loading
                await BaseScript.Delay(0);
            }

            //create either a vehicle or obj 
            int entity = IsModelAVehicle(hash) ? CreateVehicle(hash, position.X, position.Y, position.Z, heading, false, false) : CreateObject((int)hash, position.X, position.Y, position.Z, false, false, false);

            SetEntityHeading(entity, heading);
            FreezeEntityPosition(entity, true);
            SetEntityCollision(entity, false, false); // pass through
            SetModelAsNoLongerNeeded(hash);

            Spawned.Add(entity);
            return entity;
        }

        private static async Task Move(int entity, Vector3 start, Vector3 end, float speed)
        {
            float distance = Vector3.Distance(start, end);
            int durationMs = (int)(distance / speed * 1000f);
            int startTime = GetGameTimer();

            // face the direction of travel
            SetEntityHeading(entity, GetHeadingFromVector_2d(end.X - start.X, end.Y - start.Y));

            while (DoesEntityExist(entity))
            {
                float t = (GetGameTimer() - startTime) / (float)durationMs; // 0 at start, 1 at end
                if (t >= 1f) break;

                Vector3 pos = Vector3.Lerp(start, end, t);
                SetEntityCoordsNoOffset(entity, pos.X, pos.Y, pos.Z, false, false, false);

                await BaseScript.Delay(0);
            }
        }

        public static async Task SpawnAndMove(string model, Vector3 start, Vector3 end, float speed, bool deleteAtEnd = true)
        {
            int entity = await Spawn(model, start);
            if (entity == 0) return;

            await Move(entity, start, end, speed);

            if (deleteAtEnd && DoesEntityExist(entity))
                DeleteEntity(ref entity);
        }

        public static void ClearAll()
        {
            foreach (int e in Spawned)
            {
                int ent = e;
                if (DoesEntityExist(ent)) DeleteEntity(ref ent);
            }
            Spawned.Clear();
        }
    }

}