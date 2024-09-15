using System;
using System.Numerics;

using Dalamud.Interface.Components;
using Dalamud.Interface.Windowing;

using ImGuiNET;
using ImGuizmoNET;

using BDTHPlugin.Interface.Components;

namespace BDTHPlugin.Interface.Windows
{
  public class MainWindow : Window
  {
    private static PluginMemory Memory => Plugin.GetMemory();
    private static Configuration Configuration => Plugin.GetConfiguration();

    private static readonly Vector4 RED_COLOR = new(1, 0, 0, 1);

    private readonly Gizmo Gizmo;
    private readonly ItemControls ItemControls = new();

    public bool Reset;

    public MainWindow(Gizmo gizmo) : base(
      "Burning Down the House##BDTH",
      ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoResize |
      ImGuiWindowFlags.AlwaysAutoResize
    )
    {
      Gizmo = gizmo;
    }

    public override void PreDraw()
    {
      if (Reset)
      {
        Reset = false;
        ImGui.SetNextWindowPos(new Vector2(69, 69), ImGuiCond.Always);
      }
    }

    public unsafe override void Draw()
    {
      ImGui.BeginGroup();

      var placeAnywhere = Configuration.PlaceAnywhere;
      if (ImGui.Checkbox("解除摆放限制   ", ref placeAnywhere))
      {
        // Set the place anywhere based on the checkbox state.
        Memory.SetPlaceAnywhere(placeAnywhere);
        Configuration.PlaceAnywhere = placeAnywhere;
        Configuration.Save();
      }
      DrawTooltip("允许不受游戏限制地摆放家具。");

      ImGui.SameLine();

      // Checkbox is clicked, set the configuration and save.
      var useGizmo = Configuration.UseGizmo;
      if (ImGui.Checkbox("坐标轴   ", ref useGizmo))
      {
        Configuration.UseGizmo = useGizmo;
        Configuration.Save();
      }
      DrawTooltip("在选中的物品上显示一个可拖拽的坐标轴，使其根据轴方向进行移动。");

      ImGui.SameLine();

      // Checkbox is clicked, set the configuration and save.
      var doSnap = Configuration.DoSnap;
      if (ImGui.Checkbox("网格   ", ref doSnap))
      {
        Configuration.DoSnap = doSnap;
        Configuration.Save();
      }
      DrawTooltip("使坐标轴基于下面设置的网格值来进行精确移动（网格吸附）。");

      ImGui.SameLine();
      if (ImGuiComponents.IconButton(1, Gizmo.Mode == MODE.LOCAL ? Dalamud.Interface.FontAwesomeIcon.ArrowsAlt : Dalamud.Interface.FontAwesomeIcon.Globe))
        Gizmo.Mode = Gizmo.Mode == MODE.LOCAL ? MODE.WORLD : MODE.LOCAL;

      DrawTooltip(
      [
        $"Mode: {(Gizmo.Mode == MODE.LOCAL ? "本地" : "世界")}",
        "使坐标轴方向在本地坐标轴与世界坐标轴间切换。"
      ]);

      ImGui.Separator();

      if (Memory.HousingStructure->Mode == HousingLayoutMode.None)
        DrawError("进入房屋布置模式开始");
      else if (PluginMemory.GamepadMode)
        DrawError("不支持手柄");
      else if (Memory.HousingStructure->ActiveItem == null || Memory.HousingStructure->Mode != HousingLayoutMode.Rotate)
      {
        DrawError("请在旋转模式下选中家具。");
        ImGuiComponents.HelpMarker("已经跟着提示做了还是不行？尝试输入'/bdth debug'命令，将数据提交到Discord！");
      }
      else
        ItemControls.Draw();

      ImGui.Separator();

      // Drag amount for the inputs.
      var drag = Configuration.Drag;
      if (ImGui.InputFloat("网格值", ref drag, 0.05f))
      {
        drag = Math.Min(Math.Max(0.001f, drag), 10f);
        Configuration.Drag = drag;
        Configuration.Save();
      }
      DrawTooltip("设置家具移动的数值，也影响坐标轴的网格模式。");

      var dummyHousingGoods = PluginMemory.HousingGoods != null && PluginMemory.HousingGoods->IsVisible;
      var dummyInventory = Memory.InventoryVisible;

      if (ImGui.Checkbox("显示家具设置界面   ", ref dummyHousingGoods))
      {
        Memory.ShowFurnishingList(dummyHousingGoods);

        Configuration.DisplayFurnishingList = dummyHousingGoods;
        Configuration.Save();
      }
      ImGui.SameLine();

      if (ImGui.Checkbox("显示物品栏界面", ref dummyInventory))
      {
        Memory.ShowInventory(dummyInventory);

        Configuration.DisplayInventory = dummyInventory;
        Configuration.Save();
      }

      if (ImGui.Button("打开家具列表"))
        Plugin.CommandManager.ProcessCommand("/bdth list");
      DrawTooltip(
      [
        "打开可按距离排序显示的家具列表，方便选中家具。",
        "注意：目前不能在室外使用！"
      ]);

      var autoVisible = Configuration.AutoVisible;
      if (ImGui.Checkbox("自动开启本界面", ref autoVisible))
      {
        Configuration.AutoVisible = autoVisible;
        Configuration.Save();
      }
    }

    private static void DrawTooltip(string[] text)
    {
      if (ImGui.IsItemHovered())
      {
        ImGui.BeginTooltip();
        foreach (var t in text)
          ImGui.Text(t);
        ImGui.EndTooltip();
      }
    }

    private static void DrawTooltip(string text)
    {
      DrawTooltip([text]);
    }

    private void DrawError(string text)
    {
      ImGui.PushStyleColor(ImGuiCol.Text, RED_COLOR);
      ImGui.Text(text);
      ImGui.PopStyleColor();
    }
  }
}
