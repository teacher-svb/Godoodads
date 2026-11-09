#if TOOLS
using System.Threading;
using Godot;
using Godoodads.Common;

namespace Godoodads.WfcGenerator.Editor;

[Tool]
public partial class WfcGeneratorPlugin : EditorPlugin, ISerializationListener
{
    /// <summary>Menu item, dialog title and undo action name. GDDS = Godoodads, this suite.</summary>
    private const string ToolName = "Generate WFC (GDDS)";

    private SpatialMenuHook _menu;
    private GridMap _target;

    // Reload spike for the WFC M0 thread card. Replaced by the solve runner in M2.
    private Thread _spikeThread;
    private volatile bool _spikeCancel;

    public override void _EnterTree()
    {
        _menu = new SpatialMenuHook(this, "GridMap", "GridMap");
        _menu.AddItem(ToolName, new Callable(this, MethodName.OpenDialog));
        _menu.AddItem("Start reload test thread (spike)", new Callable(this, MethodName.StartSpikeThread));
    }

    public override void _ExitTree()
    {
        StopSpikeThread("plugin exit");
        _menu?.Remove();
        _menu = null;
        _target = null;
    }

    public override bool _Handles(GodotObject @object) => @object is GridMap;

    public override void _Edit(GodotObject @object) => _target = @object as GridMap;

    public override void _MakeVisible(bool visible) => _menu?.SetFallbackVisible(visible);

    /// <summary>Godot calls this before it unloads the assembly. A live thread would block the unload.</summary>
    public void OnBeforeSerialize() => StopSpikeThread("before assembly reload");

    public void OnAfterDeserialize() => GD.Print("WFC spike: assembly reloaded.");

    private void OpenDialog() => EditorInterface.Singleton.GetEditorToaster().PushToast("WFC Generator: the dialog arrives in M2.");

    private void StartSpikeThread()
    {
        if (_spikeThread != null)
            return;
        _spikeCancel = false;
        _spikeThread = new Thread(SpikeLoop) { IsBackground = true, Name = "WFC reload spike" };
        _spikeThread.Start();
        GD.Print("WFC spike: thread started. Rebuild now, then focus the editor.");
    }

    private void SpikeLoop()
    {
        long value = 0;
        while (!_spikeCancel)
            value++;
    }

    private void StopSpikeThread(string reason)
    {
        if (_spikeThread == null)
            return;
        _spikeCancel = true;
        bool stopped = _spikeThread.Join(5000);
        _spikeThread = null;
        GD.Print($"WFC spike: stop on {reason}, thread stopped = {stopped}.");
    }
}
#endif
