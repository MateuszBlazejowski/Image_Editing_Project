#include "ImageGenerator.h"
#include<cmath>
#include <vector>

namespace Backend {
	inline MyColor Color(float r, float g, float b, float a)
	{
		return {
			(unsigned char)(r * 255),
			(unsigned char)(g * 255),
			(unsigned char)(b * 255),
			(unsigned char)(a * 255)
		};
	}
	inline MyColor Lerp(MyColor left, MyColor right, float t)
	{
		return {
			(unsigned char)(left.r * (1 - t) + t * right.r),
			(unsigned char)(left.g * (1 - t) + t * right.g),
			(unsigned char)(left.b * (1 - t) + t * right.b),
			(unsigned char)(left.a * (1 - t) + t * right.a)
		};
	}
}

void Backend::GenerateImage(MyColor* array, int width, int height, TryReportCallback tryReportCallback)
{
	int size = width * height;
	srand((unsigned int)array);
	int c = rand();
	int lastReport = 0;
	for (int y = 0; y < height; y++)
	{
		float normalized_y = (float)y / (height - 1);
		for (int x = 0; x < width; x++)
		{
			float normalized_x = (float)x / (width - 1);
			int ind = y * width + x;

			float r = ((c & 1) ? (sin(normalized_x * 3.141592 * 5) + 1) * 0.5f : normalized_y * 4) *
				((c & 2) ? (sin(normalized_y * 3.141592 * 5) + 1) * 0.5f : normalized_y * 5);
			float g = ((c & 4) ? (cos(normalized_x * 3.141592 * 5) + 1) * 0.5f : normalized_x * 4) *
				((c & 8) ? (cos(normalized_y * 3.141592 * 5) + 1) * 0.5f : normalized_x * 5);
			float b =
				((c & 16) ?
					((normalized_y - normalized_x) * normalized_y - normalized_x) :
					(1 + atan2(normalized_y - 0.5f, normalized_x - 0.5f) / 3.141592)) *
				((c & 32) ?
					((normalized_x - normalized_y) * normalized_x - normalized_y) :
					(1 - atan2(normalized_y - 0.5f, normalized_x - 0.5f) / 3.141592));


			array[ind].r = r * 255;
			array[ind].g = g * 255;
			array[ind].b = b * 255;
			array[ind].a = 255;

			if ((float)(ind - lastReport) / size >= 0.01f)
			{
				if (!tryReportCallback((float)ind / size))
					return;
				lastReport = ind;
			}
		}
	}
	tryReportCallback(1);
}

void Backend::GenerateImage_Custom(MyColor* array, int width, int height,
	MyColor __stdcall getColor(float, float),
	TryReportCallback tryReportCallback)
{
	int size = width * height;
	int lastReport = 0;
	for (int y = 0; y < height; y++)
	{
		float normalized_y = (float)y / (height - 1);
		for (int x = 0; x < width; x++)
		{
			float normalized_x = (float)x / (width - 1);
			int ind = y * width + x;

			array[ind] = getColor(normalized_x, normalized_y);

			if ((float)(ind - lastReport) / size >= 0.01f)
			{
				if (!tryReportCallback((float)ind / size))
					return;
				lastReport = ind;
			}
		}
	}
	tryReportCallback(1);
}


void Backend::ProcessPixels_Custom(MyColor* array, int width, int height,
	MyColor __stdcall getColor(float, float, MyColor),
	TryReportCallback tryReportCallback)
{
	int size = width * height;
	int lastReport = 0;
	for (int y = 0; y < height; y++)
	{
		float normalized_y = (float)y / (height - 1);
		for (int x = 0; x < width; x++)
		{
			float normalized_x = (float)x / (width - 1);
			int ind = y * width + x;

			auto newColor = getColor(normalized_x, normalized_y, array[ind]);
			array[ind] = Lerp(array[ind], newColor, newColor.a);

			if ((float)(ind - lastReport) / size >= 0.01f)
			{
				if (!tryReportCallback((float)ind / size))
					return;
				lastReport = ind;
			}
		}
	}
	tryReportCallback(1);
}

void Backend::Blur(MyColor* array, int width, int height, int w, int h, TryReportCallback tryReportCallback)
{
	int size = width * height;
	int lastReport = 0;
	std::vector<long long> partialSums[4];
	for (int i = 0; i < 4; i++)
		partialSums[i].resize(width * height);

	for (int x = 0; x < width; x++)
		for (int i = 0; i < 4; i++)
			partialSums[i][x] = (&array[x].r)[i];
	for (int y = 1; y < height; y++)
		for (int i = 0; i < 4; i++)
			partialSums[i][y * width] = (&array[y * width].r)[i];

	for (int y = 1; y < height; y++)
		for (int x = 1; x < width; x++)
		{
			for (int i = 0; i < 4; i++)
				partialSums[i][y * width + x] =
				(&array[y * width + x].r)[i]
				+ partialSums[i][(y - 1) * width + x]
				+ partialSums[i][y * width + x - 1]
				- partialSums[i][(y - 1) * width + x - 1];

			if ((float)(y * width + x - lastReport) / size >= 0.01f)
			{
				if (!tryReportCallback((float)(y * width + x) / size))
					return;
				lastReport = y * width + x;
			}
		}

	for (int y = 0; y < height; y++)
	{
		for (int x = 0; x < width; x++)
		{
			int minx = fmaxf(0, x - w - 1);
			int maxx = fminf(width - 1, x + w);
			int miny = fmaxf(0, y - h - 1);
			int maxy = fminf(height - 1, y + h);

			int area = (maxx - minx) * (maxy - miny);

			for (int i = 0; i < 4; i++)
				(&array[y * width + x].r)[i] = (
					partialSums[i][maxy * width + maxx]
					- partialSums[i][maxy * width + minx]
					- partialSums[i][miny * width + maxx]
					+ partialSums[i][miny * width + minx]) / area;

		}
	}
	tryReportCallback(1);
}

void Backend::GammaCorrection(
	MyColor* array, int width, int height,
	float gamma,
	TryReportCallback tryReportCallback)
{
	int size = width * height;
	int lastReport = 0;
	for (int y = 0; y < height; y++)
	{
		for (int x = 0; x < width; x++)
		{
			int ind = y * width + x;
			array[ind].r = (unsigned char)(powf((float)array[ind].r / 255, gamma) * 255);
			array[ind].g = (unsigned char)(powf((float)array[ind].g / 255, gamma) * 255);
			array[ind].b = (unsigned char)(powf((float)array[ind].b / 255, gamma) * 255);


			if ((float)(ind - lastReport) / size >= 0.01f)
			{
				tryReportCallback((float)ind / size);
				lastReport = ind;
			}
		}
	}
	tryReportCallback(1);
}

void Backend::GOL(MyColor* texture, int textureWidth, int textureHeight, TryReportCallback tryReportCallback)
{
	int size = textureWidth * textureHeight;
	int lastReport = 0;
	std::vector<char> back;
	back.resize(textureWidth * textureHeight, 0);


	for (int y = 0; y < textureHeight; y++)
	{
		for (int x = 0; x < textureWidth; x++)
		{
			int ind = y * textureWidth + x;
			int neighbours = 0;
			bool me = false;
			for (int dy = -1; dy <= 1; dy++)
				for (int dx = -1; dx <= 1; dx++)
				{
					if (dx == 0 && dy == 0)
						continue;
					int myY = (y + dy + textureHeight) % textureHeight;
					int myX = (x + dx + textureWidth) % textureWidth;
					int ind = myY * textureWidth + myX;
					int brightness = 0;
					for (int i = 0; i < 4; i++)
						brightness += (&texture[ind].r)[i];
					if (brightness > 4 * 127)
						neighbours++;
				}
			int brightness = 0;
			for (int i = 0; i < 4; i++)
				brightness += (&texture[ind].r)[i];
			if (brightness > 4 * 127)
				me = true;

			if (me)
			{
				if (neighbours < 2 || neighbours > 3)
					back[ind] = 0;
				else
					back[ind] = 255;
			}
			else
			{
				if (neighbours == 3)
					back[ind] = 255;
			}

			if ((float)(y * textureWidth + x - lastReport) / size >= 0.02f)
			{
				if (!tryReportCallback((float)(y * textureWidth + x) / size * 0.5f + 0.5f))
					return;
				lastReport = y * textureWidth + x;
			}
		}
	}
	for (int y = 0; y < textureHeight; y++)
	{
		for (int x = 0; x < textureWidth; x++)
		{
			int ind = y * textureWidth + x;
			for (int i = 0; i < 4; i++)
				(&texture[ind].r)[i] = back[ind];
		}
	}
	tryReportCallback(1);
}

void Backend::DrawCircles(MyColor* array, int width, int height,
	Circle* circles, int circleCount,
	TryReportCallback tryReportCallback)
{
	int size = width * height;
	srand((unsigned)array);
	int lastReport = 0;
	int currentId = 0;

	std::vector<float> radiusSq;
	for (int i = 0; i < circleCount; i++)
		radiusSq.push_back(circles[i].radius * circles[i].radius);

	for (int y = 0; y < height; y++)
	{
		float normalized_y = (float)y / (height - 1);
		for (int x = 0; x < width; x++)
		{
			float normalized_x = (float)x / (width - 1);
			int ind = y * width + x;
			for (int i = 0; i < circleCount; i++)
			{
				float dx = circles[i].x - normalized_x;
				float dy = circles[i].y - normalized_y;
				if (fabsf(dx) > circles[i].radius || fabsf(dy) > circles[i].radius)
					continue;

				float d = 1 - (dx * dx + dy * dy) / radiusSq[i];
				if (d > 0.01f)
					array[ind] = Color(d, d, d, 1);
				else if (d > 0)
					array[ind] = Lerp(array[ind], Color(d, d, d, 1), d / 0.01f);
			}

			if ((float)(ind - lastReport) / size >= 0.01f)
			{
				tryReportCallback((float)ind / size);
				lastReport = ind;
			}
		}
	}
	tryReportCallback(1);
}

void Backend::ColorCorrection(
	MyColor* array, int width, int height,
	float red, float green, float blue,
	TryReportCallback tryReportCallback)
{
	int size = width * height;
	int lastReport = 0;
	for (int y = 0; y < height; y++)
	{
		for (int x = 0; x < width; x++)
		{
			int ind = y * width + x;

			array[ind].r = fminf(255, array[ind].r + red * 255);
			array[ind].g = fminf(255, array[ind].g + green * 255);
			array[ind].b = fminf(255, array[ind].b + blue * 255);

			if ((float)(ind - lastReport) / size >= 0.01f)
			{
				tryReportCallback((float)ind / size);
				lastReport = ind;
			}
		}
	}
	tryReportCallback(1);
}
