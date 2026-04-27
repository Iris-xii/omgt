using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

public partial class GraphicsEditor : Node2D {
  [Export] public TileMapLayer hexGridMath = null!;
  [Export] Texture2D hoverGlowIcon = null!;
  [Export] Material onlyGreenShader = null!;
  [Export] ColorRect glowRenderNode = null!;
  [Export] Node2D origMarker = null!;
  [Export] Shader strokeRenderHackShader = null!;
  [ExportGroup("BaseTextures")]
  [Export] Texture2D base_tr = null!;
  [Export] Texture2D base_tl = null!;
  [Export] Texture2D base_r = null!;
  [Export] Texture2D base_l = null!;
  [Export] Texture2D base_br = null!;
  [Export] Texture2D base_bl = null!;
  [ExportGroup("StrokeTextures")]
  [Export] public Texture2D[] stroke_textures = null!;
  [ExportGroup("Debug")]
  [Export] public bool strokeDebug = false;
  [Export] public bool strokeDebugBox = false;

  EditorState editorState = EditorState.DRAW_BG;
  Vector2I drawHoverAt = Vector2I.Zero;

  enum EditorState {
    NONE_EXPORTING, NONE_EXPORTING_DRAW_NOTHING, NONE_EXPORTING_DRAW_STROKE, DRAW_BG
  }
  private bool CanRenderTiles() =>
    editorState != EditorState.NONE_EXPORTING_DRAW_NOTHING &&
    editorState != EditorState.NONE_EXPORTING_DRAW_STROKE;
  public bool CanRenderStroke() => strokeDebug || editorState == EditorState.NONE_EXPORTING_DRAW_STROKE;//todo 
  private bool CanRenderGlow() => editorState == EditorState.DRAW_BG;

  public override void _Ready() {
    hexGridMath.Visible = false;
    origMarker.Visible = true;
    origMarker.Position = GridToLocal(new(0, 0), hexGridMath);
    QueueRedraw();
  }

  private void DrawAtCenter(Texture2D t, Vector2 at, CanvasItem? cim = null) {
    DrawTexture(t, at - (t.GetSize() / 2));
  }
  private int NeighborSourceId(Vector2I cellPos, TileSet.CellNeighbor neighbor) {
    var cell = hexGridMath.GetNeighborCell(cellPos, neighbor);
    return hexGridMath.GetCellSourceId(cell);
  }
  public override void _Draw() {
    Dictionary<Vector2I, int> texIDs = [];

    foreach (var hex in hexGridMath.GetUsedCells()) {
      //Adapted from https://github.com/icwass/Devmodeus/blob/main/Devmodeus/strokebuilder.cs
      if (!CanRenderStroke()) break;
      var left = hexGridMath.GetNeighborCell(hex, TileSet.CellNeighbor.LeftSide);
      var downleft = hexGridMath.GetNeighborCell(hex, TileSet.CellNeighbor.BottomLeftSide);
      var downright = hexGridMath.GetNeighborCell(hex, TileSet.CellNeighbor.BottomRightSide);
      if (!texIDs.ContainsKey(left)) { texIDs[left] = 0; }
      texIDs[left] += 1;
      if (!texIDs.ContainsKey(downleft)) { texIDs[downleft] = 0; }
      texIDs[downleft] += 2;
      if (!texIDs.ContainsKey(downright)) { texIDs[downright] = 0; }
      texIDs[downright] += 4;
      if (!texIDs.ContainsKey(hex)) { texIDs[hex] = 0; }
      texIDs[hex] += 8;
    }

    foreach (var cellPos in hexGridMath.GetUsedCells()) {
      var sourceID = hexGridMath.GetCellSourceId(cellPos);
      var drawPosA = hexGridMath.MapToLocal(cellPos);
      var drawPos = hexGridMath.Transform * drawPosA;
      if (CanRenderTiles()) {
        var sourceA = hexGridMath.TileSet.GetSource(sourceID);
        if (sourceA is TileSetAtlasSource source) {
          var sourceTex = source.Texture;
          DrawTexture(sourceTex, drawPos - (sourceTex.GetSize() / 2));
          // borders: 
          if (NeighborSourceId(cellPos, TileSet.CellNeighbor.LeftSide) != 0) { DrawAtCenter(base_l, drawPos); }
          if (NeighborSourceId(cellPos, TileSet.CellNeighbor.RightSide) != 0) { DrawAtCenter(base_r, drawPos); }
          if (NeighborSourceId(cellPos, TileSet.CellNeighbor.TopLeftSide) != 0) { DrawAtCenter(base_tl, drawPos); }
          if (NeighborSourceId(cellPos, TileSet.CellNeighbor.TopRightSide) != 0) { DrawAtCenter(base_tr, drawPos); }
          if (NeighborSourceId(cellPos, TileSet.CellNeighbor.BottomLeftSide) != 0) { DrawAtCenter(base_bl, drawPos); }
          if (NeighborSourceId(cellPos, TileSet.CellNeighbor.BottomRightSide) != 0) { DrawAtCenter(base_br, drawPos); }
        }
      }
    }
    if (CanRenderStroke()) {
      //Vector2I textureOffset = (Vector2I)(hexGridMath.TileSet.TileSize * new Vector2(0.75f, 0.0f));
      foreach (var (hPos, ID) in texIDs) { //Adapted from https://github.com/icwass/Devmodeus/blob/main/Devmodeus/strokebuilder.cs
        if (ID < 0 || ID >= 15) continue;
        var tex = stroke_textures[ID];
        var drawPosA = hexGridMath.MapToLocal(hPos);
        var drawPosB = hexGridMath.Transform * drawPosA;
        var finalPos = (drawPosB - tex.GetSize() + new Vector2(61.0f, 0.0f)).Floor();
        DrawTexture(tex, finalPos);
        if (strokeDebugBox) {
          DrawRect(new Rect2(finalPos, tex.GetSize()), Colors.Red, false);
          DrawMultilineString(ThemeDB.FallbackFont,
            finalPos + new Vector2(0, 10),
            $"{ID}\np:{finalPos}\ns:{tex.GetSize()}", HorizontalAlignment.Left, -1, 8, -1, Colors.Red);
        }
      }
    }
    if (CanRenderGlow()) {
      DrawTexture(hoverGlowIcon, GridToLocal(drawHoverAt, hexGridMath) - (hoverGlowIcon.GetSize() / 2),
      new Color(1f, 1f, 1f, 0.5f));
    }
  }

  private static Vector2I LocalToGrid(Vector2 pos, TileMapLayer hexGridMath) {
    var correctedPos = hexGridMath.Transform.AffineInverse() * pos;
    return hexGridMath.LocalToMap(correctedPos);
  }
  private static Vector2 GridToLocal(Vector2I gridPos, TileMapLayer hexGridMath) {
    var posA = hexGridMath.MapToLocal(gridPos);
    var pos = hexGridMath.Transform * posA;
    return pos;
  }
 
  private Rect2I AllTilesRectWithMirrored(Vector2? sizeM = null) {
    HashSet<Rect2> tilesRectWMirror = [];
    var size = sizeM ?? new Vector2(82,96);
    var halfSize = size/2;
    foreach (var hex in hexGridMath.GetUsedCells()) {
      tilesRectWMirror.Add(new Rect2(GetScreenTransform()*(GridToLocal(hex,hexGridMath) - halfSize),size));
      tilesRectWMirror.Add(new Rect2(GetScreenTransform()*(GridToLocal(-hex,hexGridMath) - halfSize),size));
    }
    Rect2 finalRect = new(GetScreenTransform()*Position,Vector2.Zero);
    foreach(var rect in tilesRectWMirror) {
      finalRect = finalRect.Merge(rect);
    } 
    return (Rect2I) finalRect;
  }


  private bool pushimglock = false;
  public async Task PushImg(bool doAutoshift) { // TODO: not swallow errors
    if (pushimglock) { return; } 
    pushimglock = true;
    var previousState = editorState;
    editorState = EditorState.NONE_EXPORTING;
    origMarker.Visible = false;
    GetViewport().TransparentBg = true;
    QueueRedraw();
    await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    var imageBaseUncut = GetViewport().GetTexture().GetImage();
    var usedRect = doAutoshift ? imageBaseUncut.GetUsedRect().Merge(AllTilesRectWithMirrored()) : imageBaseUncut.GetUsedRect();
    var imageBase = imageBaseUncut.GetRegion(usedRect);
    DirAccess.Open("user://").MakeDir("user://glyph/");
    imageBase.SavePng("user://glyph/base.png");
    //

    List<(CanvasItem, bool)> visUndo = [];
    foreach (var node in GetChildren()) {
      if (node is CanvasItem ci) {
        if (ci is Camera2D) continue;
        visUndo.Add((ci, ci.Visible));
        ci.Visible = false;
      }
    }
    Sprite2D newBaseSprite = new() {
      Texture = ImageTexture.CreateFromImage(imageBaseUncut),
      Material = onlyGreenShader,
      SelfModulate = new Color("#87c5a1ff"),
      //SelfModulate = new Color(10.0f,10.0f,10.0f,1.0f),
    };
    AddChild(newBaseSprite);
    QueueRedraw();
    await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

    Image imageGlowUncutNoBlur = GetViewport().GetTexture().GetImage();
    imageGlowUncutNoBlur.Decompress();
    imageGlowUncutNoBlur.FixAlphaEdges();
    imageGlowUncutNoBlur.SavePng("user://glyph/glow_no_blur.png");
    glowRenderNode.Visible = true;
    if (glowRenderNode.Material is ShaderMaterial sm) {
      var im = Image.LoadFromFile("user://glyph/glow_no_blur.png");
      //im.SrgbToLinear();    
      var imt = ImageTexture.CreateFromImage(im);
      sm.SetShaderParameter("IN_TEXTURE", imt);
    }

    RemoveChild(newBaseSprite);
    newBaseSprite.QueueFree();
    editorState = EditorState.NONE_EXPORTING_DRAW_NOTHING;
    QueueRedraw();
    await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

    var imageGlowUncut = GetViewport().GetTexture().GetImage();
    var usedRectGlow = doAutoshift ? imageGlowUncut.GetUsedRect().Merge(AllTilesRectWithMirrored(new Vector2(110,120))) : imageGlowUncut.GetUsedRect();
    var imageGlow = imageGlowUncut.GetRegion(usedRectGlow);
    //imageGlow.LinearToSrgb();
    imageGlow.SavePng("user://glyph/glow.png");
    //
    glowRenderNode.Visible = false;
    editorState = EditorState.NONE_EXPORTING_DRAW_STROKE;
    QueueRedraw();
    await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    var imageStrokeFirstStage = GetViewport().GetTexture().GetImage();
    Sprite2D newStrokeSprite = new() {
      Texture = ImageTexture.CreateFromImage(imageStrokeFirstStage),
      Material = new ShaderMaterial() { Shader = strokeRenderHackShader }
    };
    AddChild(newStrokeSprite);
    editorState = EditorState.NONE_EXPORTING_DRAW_NOTHING;
    QueueRedraw();
    await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    var imageStrokeUncut = GetViewport().GetTexture().GetImage();
    var usedStrokeRect = doAutoshift ? imageStrokeUncut.GetUsedRect().Merge(AllTilesRectWithMirrored(new Vector2(86,105))) : imageStrokeUncut.GetUsedRect();
    var imageStroke = imageStrokeUncut.GetRegion(usedStrokeRect);
    imageStroke.LinearToSrgb();
    imageStroke.SavePng("user://glyph/stroke.png");
    newStrokeSprite.QueueFree();

    foreach (var (ci, oldVis) in visUndo) {
      ci.Visible = oldVis;
    }
    //
    editorState = previousState;
    origMarker.Visible = true;
    GetViewport().TransparentBg = false;

    //var timer = GetTree().CreateTimer(4.10);
    //await ToSignal(timer, SceneTreeTimer.SignalName.Timeout);
    Godot.OS.ShellShowInFileManager(ProjectSettings.GlobalizePath("user://glyph"));
    pushimglock = false;
  } 

  public override void _UnhandledInput(InputEvent e) {
    switch (editorState) {
      case EditorState.DRAW_BG when e is InputEventMouse emg: {
          var em = (InputEventMouse)MakeInputLocal(emg);
          var localPos = em.Position;
          drawHoverAt = LocalToGrid(localPos, hexGridMath);
          if ((em.ButtonMask & MouseButtonMask.Left) > 0) {
            hexGridMath.SetCell(LocalToGrid(localPos, hexGridMath), 0, new Vector2I(0, 0));
          }
          else if ((em.ButtonMask & MouseButtonMask.Right) > 0) {
            hexGridMath.SetCell(LocalToGrid(localPos, hexGridMath), -1);
          }
          QueueRedraw();
          break;
        }
    }
  }




}
