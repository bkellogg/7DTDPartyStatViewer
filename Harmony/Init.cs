using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace PartyStatViewer
{
    [SuppressMessage("ReSharper", "UnusedType.Global")]
    public class Init : IModApi
    {
        public void InitMod(Mod _modInstance)
        {
            Log.Out("[PartyStatViewer] Initializing mod...");

            var harmony = new HarmonyLib.Harmony("com.partystatviewer.patches");
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            Log.Out("[PartyStatViewer] Mod initialized successfully");
        }
    }
}
