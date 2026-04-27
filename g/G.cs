using Godot;
using System;
using System.Collections.Generic;

public partial class G : Node
{ 
	public static G? I = null;

	public static bool IsDebug()  {
		return OS.IsDebugBuild();
	}   
	 public override void _Ready() {
		G.I = this;
		//DisplayServer.SetIcon( GD.Load<CompressedTexture2D>("res://icon.png").GetImage() );
		GetWindow().SizeChanged += OnSizeChanged;
		OnSizeChanged(); 
	 }

	public void OnSizeChanged() {
		var window = GetWindow();
		var intendedRes = new Vector2
		{X = (float)ProjectSettings.GetSetting("display/window/size/viewport_width"),
		Y = (float)ProjectSettings.GetSetting("display/window/size/viewport_height")} ;
		//intendedRes = new Vector2 {X=300f,Y=300f};
		Vector2 ratio = window.Size / intendedRes; 
		ratio.X = Mathf.Floor(ratio.X);
		ratio.Y = Mathf.Floor(ratio.Y);
		var min = Mathf.Min(ratio.X,ratio.Y);
		window.ContentScaleFactor = min > 0 ? min : 1; 
	}
	
} 