shader_type canvas_item;

void fragment() {
	COLOR.rgba = vec4(COLOR.rgb,1.0);
}

//void light() {
//	// Called for every pixel for every light affecting the CanvasItem.
//	// Uncomment to replace the default light processing function with this one.
//}
