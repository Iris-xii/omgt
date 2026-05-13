using Godot;
using System;
using System.Threading.Tasks;

public partial class Main : Control {

  [Export] GraphicsEditor graphicsEditor = null!;
  [Export] SpinBox hhoX = null!;
  [Export] SpinBox hhoY = null!;
  [Export] CheckBox shouldAutoAdjustButton = null!;
  [Export] CanvasLayer[] disableOnExport = [];

  public override void _Ready() {
    hhoX.GetLineEdit().ContextMenuEnabled = false; 
    hhoY.GetLineEdit().ContextMenuEnabled = false;
    graphicsEditor.updateHHODisplay = UpdateHHODisplay;
    OnAutoAdjustButtonPressed();
  }

  public void UpdateHHODisplay() {
    var newHHO = graphicsEditor.pixelHexOffset;
    hhoX.Value = newHHO.X;
    hhoY.Value = newHHO.Y;
  }
  public void OnAutoAdjustButtonPressed(bool _=false) {
    graphicsEditor.pixelAutoAdjust = shouldAutoAdjustButton.ButtonPressed;
    graphicsEditor.AttemptAutoAdjust();
  }
  public void OnHHOValueChanged(float _) {
    graphicsEditor.pixelHexOffset = new Vector2((float)hhoX.Value, (float)hhoY.Value);
    graphicsEditor.QueueRedraw();
  }

  public async void OnExportButtonPressed() {
    foreach (var item in disableOnExport) { item.Visible = false; }
    await graphicsEditor.PushImg();
    foreach (var item in disableOnExport) { item.Visible = true; }
  }
}
