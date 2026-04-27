shader_type canvas_item; 
render_mode blend_premul_alpha,unshaded;
uniform sampler2D IN_TEXTURE: repeat_disable, filter_nearest;//repeat_disable, filter_nearest,source_color;

uniform float strength : hint_range(0.1, 20.0) = 5.0; 

float gaussianDistribution(float x, float STD){ // STD stands for standard deviation
	return exp(-(x*x)/(2.*STD*STD))/(sqrt(2.*PI)*STD);
}

float Convert_sRGB_FromLinear (float theLinearValue) {
  return theLinearValue <= 0.0031308f
       ? theLinearValue * 12.92f
       : pow(theLinearValue, 1.0f/2.4f) * 1.055f - 0.055f;
}
float Convert_sRGB_ToLinear (float thesRGBValue) {
  return thesRGBValue <= 0.04045f
       ? thesRGBValue / 12.92f
       : pow((thesRGBValue + 0.055f) / 1.055f, 2.4f);
}
vec4 gaussianblur(sampler2D sampler, vec2 pos, vec2 pixel_size, float sigmaUsed, int radius){
    vec4 blurredPixel = vec4(0.0);
    float total_weight = 0.0; 
	// Loop over the radius (tecnically its a square)
    for(int i = -radius ; i <= radius; i++){ 
        for(int j = -radius; j <= radius; j++){
			// Calculate the offset from the current pixel
			vec2 offset = vec2(float(i), float(j))*pixel_size;
			vec2 changedPos = pos + offset;        
			
			// Calculate the weight based on the Gaussian distribution multiplying both dimentions (how far are X and Y form the center (pos))
			float weight = gaussianDistribution(float(i), sigmaUsed)*gaussianDistribution(float(j), sigmaUsed);
			// Add the weighted color value to the blurred pixel
			blurredPixel.rgb += texture(IN_TEXTURE, changedPos).rgb * weight;
			blurredPixel.a += texture(IN_TEXTURE, changedPos).a * weight; 
			total_weight += weight;
        }
    }
	// Normalize the blurred pixel color by the total weight
	blurredPixel.rgba/=total_weight; 
	//blurredPixel.rgb=pow(blurredPixel.rgb,vec3(1.0/2.2)); 
    return blurredPixel;
}
void fragment() {
    vec4 PixelBlurred = gaussianblur(IN_TEXTURE, SCREEN_UV, SCREEN_PIXEL_SIZE, strength, int(round(3.0 * strength)));
	
    // The radius is 3*strength because it is the point where the Gaussian weight is near zero.
    //COLOR = vec4(vec3(0.529, 0.773, 0.631),PixelBlurred.a);
	COLOR = PixelBlurred;
	COLOR.a = 0.0;
}
