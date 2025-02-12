#pragma once
namespace Backend
{

	/// Fields guaranteed to be 4 consecutive bytes
	struct MyColor
	{
		unsigned char r, g, b, a;
	};
	/// Fields guaranteed to be 3 consecutive sets of 4 bytes
	///	 radius is expressed as a percentage of the image width
	struct Circle {
		float x, y;
		float radius;
	};
	/// All functions of the library accept this callback as the last parameter. They call it whenever approximately 1% of the work is done.
	/// They pass the percentage of work done as the parameter.
	/// If the callback returns `false`, functions return immediately.
	typedef bool __cdecl TryReportCallback(float);

	/** Generating Functions
	 * Guarantee to write all of the pixels of the texture.
	**/

	/// Sets every pixel of the texture to one of the pre-set patterns.
	extern "C" __declspec(dllexport) void GenerateImage(
		MyColor* texture, int textureWidth, int textureHeight,
		TryReportCallback tryReportCallback);
	/// Sets every pixel of the texture to the result of the getColor callback.
	extern "C" __declspec(dllexport) void GenerateImage_Custom(
		MyColor* texture, int textureWidth, int textureHeight,
		MyColor __stdcall getColor(float, float),
		TryReportCallback tryReportCallback);

	/** Processing Functions
	 * Assume all pixels of the texture are initialized.
	**/

	/// Overlays every pixel of the texture with the result of the modifyColor callback. Uses the alpha value to blend with the existing image.
	extern "C" __declspec(dllexport) void ProcessPixels_Custom(
		MyColor* texture, int textureWidth, int textureHeight,
		MyColor __stdcall modifyColor(float, float, MyColor),
		TryReportCallback tryReportCallback);

	/// Applies a rectangular blur to the texture
	extern "C" __declspec(dllexport) void Blur(
		MyColor* texture, int textureWidth, int textureHeight,
		int w, int h,
		TryReportCallback tryReportCallback);

	/// Draws circles from the circles array
	extern "C" __declspec(dllexport) void DrawCircles(
		MyColor* texture, int textureWidth, int textureHeight,
		Circle* circles, int circleCount,
		TryReportCallback tryReportCallback);

	/// Performs color correction by addition
	extern "C" __declspec(dllexport) void ColorCorrection(
		MyColor* texture, int textureWidth, int textureHeight,
		float red, float green, float blue,
		TryReportCallback tryReportCallback);

	/// Performs Gamma correction
	extern "C" __declspec(dllexport) void GammaCorrection(
		MyColor* texture, int textureWidth, int textureHeight,
		float gamma,
		TryReportCallback tryReportCallback);

	/// ?
	extern "C" __declspec(dllexport) void GOL(
		MyColor* texture, int textureWidth, int textureHeight,
		TryReportCallback tryReportCallback);
}