using Godot;
using System;
using System.Threading.Tasks;

public partial class Main : Control {

  [Export] GraphicsEditor graphicsEditor = null!;
  [Export] Button doAutoshift = null!;
  [Export] CanvasLayer[] disableOnExport = [];

  public async void OnExportButtonPressed() { 
    foreach (var item in disableOnExport) {item.Visible = false;}
    await graphicsEditor.PushImg(doAutoshift.ButtonPressed);
    foreach (var item in disableOnExport) {item.Visible = true;}
  }
}
