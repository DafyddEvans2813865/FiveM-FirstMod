using System.Threading.Tasks;
using CitizenFX.Core;
using static CitizenFX.Core.Native.API;


namespace DredgeClient.Effects
{
    public static class CameraFx
    {
        public static async Task ShakeCamera(string shakeType, float intensity, int duration)
        {
            ShakeGameplayCam(shakeType, intensity);
            await BaseScript.Delay(duration);
            StopGameplayCamShaking(true);
        }

    }


}