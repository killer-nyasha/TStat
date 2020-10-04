#include "pch.h"

#include <Windows.h>
#include <d3d9.h>
#include <d3dx9.h>

#include <thread>

#include <iostream>

#pragma comment (lib, "d3d9.lib")
#pragma comment (lib, "d3dx9.lib")
//#pragma comment (lib, "Xaudio2.lib")
//#pragma comment (lib, "Ole32.lib")

using namespace System;
using namespace System::Windows::Forms;

HWND hWnd;
IDirect3D9* d3d;
D3DPRESENT_PARAMETERS        params;
IDirect3DDevice9* d3ddev;
LPD3DXSPRITE m_pSprite;
DWORD msaaSamples, maxAnisotrophy;

LPD3DXFONT mpFont;

const DWORD line_fvf = D3DFVF_XYZRHW | D3DFVF_DIFFUSE;

struct line_vertex {
	float x, y, z, rhw;  // The transformed(screen space) position for the vertex.
	DWORD colour;        // The vertex colour.
};

bool initDirectX()
{
	HRESULT hr = 0;
	D3DDISPLAYMODE desktop = { 0 };

	d3d = Direct3DCreate9(D3D_SDK_VERSION);

	if (!d3d)
		return false;

	// Just use the current desktop display mode.
	hr = d3d->GetAdapterDisplayMode(D3DADAPTER_DEFAULT, &desktop);

	if (FAILED(hr))
	{
		d3d->Release();
		d3d = nullptr;
		return false;
	}

	ZeroMemory(&params, sizeof(params)); // clear out the struct for use

	// Setup Direct3D for windowed rendering.
	params.BackBufferWidth = 0;
	params.BackBufferHeight = 0;
	params.BackBufferFormat = desktop.Format;
	params.BackBufferCount = 1;

	//windowSize = ivect2(desktop.Width, desktop.Height);

	params.hDeviceWindow = hWnd;
	params.Windowed = TRUE;

	params.EnableAutoDepthStencil = FALSE;
	params.AutoDepthStencilFormat = D3DFMT_D24S8;
	//params.Flags = D3DPRESENTFLAG_DISCARD_DEPTHSTENCIL;
	//params.FullScreen_RefreshRateInHz = 0;
	params.MultiSampleType = D3DMULTISAMPLE_NONE;

	//if (enableVerticalSync)
	//	params.PresentationInterval = D3DPRESENT_INTERVAL_DEFAULT;
	//else
	//	params.PresentationInterval = D3DPRESENT_INTERVAL_IMMEDIATE;

	// Swap effect must be D3DSWAPEFFECT_DISCARD for multi-sampling support.
	//params.SwapEffect = D3DSWAPEFFECT_DISCARD;

	params.SwapEffect = D3DSWAPEFFECT_DISCARD;

	// Select the highest quality multi-sample anti-aliasing (MSAA) mode.
	//ChooseBestMSAAMode(params.BackBufferFormat, params.AutoDepthStencilFormat,
	//	params.Windowed, params.MultiSampleType, params.MultiSampleQuality,
	//	msaaSamples);

	// Most modern video cards should have no problems creating pure devices.
	// Note that by creating a pure device we lose the ability to debug vertex
	// and pixel shaders.
	//if (false)
	hr = d3d->CreateDevice(D3DADAPTER_DEFAULT, D3DDEVTYPE_HAL, hWnd,
		D3DCREATE_HARDWARE_VERTEXPROCESSING | D3DCREATE_PUREDEVICE,
		&params, &d3ddev);

	if (FAILED(hr))
	{
		// Fall back to software vertex processing for less capable hardware.
		// Note that in order to debug vertex shaders we must use a software
		// vertex processing device.
		hr = d3d->CreateDevice(D3DADAPTER_DEFAULT, D3DDEVTYPE_HAL, hWnd,
			D3DCREATE_SOFTWARE_VERTEXPROCESSING, &params, &d3ddev);
	}

	//std::cout << hr;

	if (FAILED(hr))
	{
		d3d->Release();
		d3d = nullptr;
		return false;
	}

	//std::cout << "success\n";

	D3DCAPS9 caps;

	// Prefer anisotropic texture filtering if it's supported.
	if (SUCCEEDED(d3ddev->GetDeviceCaps(&caps)))
	{
		if (caps.RasterCaps & D3DPRASTERCAPS_ANISOTROPY)
			maxAnisotrophy = caps.MaxAnisotropy;
		else
			maxAnisotrophy = 1;
	}

	//initGeometry();

	if (FAILED(D3DXCreateSprite(d3ddev, &m_pSprite)))
		;
	//	throw Exception("Ошибка при создании спрайта");

	//// Enable alpha blending.
	//d3ddev->SetRenderState(D3DRS_ALPHABLENDENABLE,
	//	TRUE);

	//// Set the source blend state.
	//d3ddev->SetRenderState(D3DRS_SRCBLEND,
	//	D3DBLEND_SRCALPHA);

	//// Set the destination blend state.
	//d3ddev->SetRenderState(D3DRS_DESTBLEND,
	//	D3DBLEND_INVSRCALPHA);

	return true;
}

#define FONT_HEIGHT 24
#define FONT_WIDTH 12
#define FONT_WEIGHT 500
#define __COLOR(a,r,g,b) \
    ((int)((((a)&0xff)<<24)|(((r)&0xff)<<16)|(((g)&0xff)<<8)|((b)&0xff)))
#define FONT_COLOR __COLOR(255, 255, 255, 220)

void loadFont(wchar_t* pathToFile, int size)
{
	D3DXFONT_DESCW m_fontDesc;

	ZeroMemory(&m_fontDesc, sizeof(D3DXFONT_DESC));
	m_fontDesc.Height = size;
	m_fontDesc.Width = size / 2;
	m_fontDesc.Weight = FONT_WEIGHT;
	m_fontDesc.MipLevels = D3DX_DEFAULT;
	m_fontDesc.Italic = false;
	m_fontDesc.CharSet = 0;
	m_fontDesc.OutputPrecision = 0;
	m_fontDesc.Quality = 0;
	m_fontDesc.PitchAndFamily = 0;
	// name will be something like "Arial"

	//wchar_t* name = stringptrToWchar(pathToFile);
	wcscpy_s(m_fontDesc.FaceName, pathToFile);


	if (FAILED(D3DXCreateFontIndirectW(d3ddev, &m_fontDesc, &mpFont)))
		;
		//throw Exception("Ошибка при загрузке шрифта");
}

void drawText(wchar_t* name, int x, int y)
{
	if (name == nullptr)
		return;

	RECT rect =
	{ (int)x,
		(int)y,
		(int)x + 600,
		(int)y + 100
	};

	//wchar_t* name = stringptrToWchar(text.text);

	m_pSprite->Begin(D3DXSPRITE_ALPHABLEND | D3DXSPRITE_SORT_TEXTURE); //  <--- must be called between LPD3DXSPRITE::Begin()
	mpFont->DrawTextW(m_pSprite, // <-- the sprite
		name,  // <-- the text
		-1, // <-- num characters in string  -1 means its null terminated
		&rect, // <--- position <limits>
		DT_TOP | DT_LEFT | DT_NOCLIP | DT_WORDBREAK, // <--- alignment in the rect
		/*(D3DCOLOR)text.color.distf0() ?*//* __COLOR(text.color.a, text.color.r, text.color.g, text.color.b) :*/ FONT_COLOR); // Color   
	m_pSprite->End();// <-- - End  sprite
}

line_vertex lines[2 * 365 * 4 * 500 / 100];

void randomLines(int k)
{
#define RNDCLR (rand()%200+55)
	DWORD color = __COLOR(0, RNDCLR, RNDCLR, RNDCLR);

	for (size_t i = 0; i < k; i++)
	{
		int yval = rand() % 1000;

		lines[i].x = 10 + i * 1.5;
		lines[i].y = 1000 - yval;
		lines[i].colour = color;
	}
}

int fps = 0;
int prevFps = 0;

void startFps()
{
	std::thread t([]() 
		{
			while (true)
			{
				prevFps = fps; fps = 0;
				Sleep(1000);
			}
		});
	t.detach();
}

public ref class Form1 : public Form
{
public:
	Form1()
	{
		hWnd = (HWND)(void*)Handle;
		//this->WindowState = FormWindowState::Maximized;
		this->Load += gcnew System::EventHandler(this, &Form1::OnLoad);
		//this->BackColor = System::Drawing::Color::Black;        
	}

	void OnPaint(PaintEventArgs^ e) override
	{
		static int j = 0;
		HRESULT hr;

		hr = d3ddev->BeginScene();
		//if (FAILED(hr))
		//	std::cout << j++ << "\n";

		//Clear the buffer to our new colour.
		d3ddev->Clear(0,  //Number of rectangles to clear, we're clearing everything so set it to 0
			NULL, //Pointer to the rectangles to clear, NULL to clear whole display
			D3DCLEAR_TARGET,   //What to clear.  We don't have a Z Buffer or Stencil Buffer
			0x00000000, //Colour to clear to (AARRGGBB)
			1.0f,  //Value to clear ZBuffer to, doesn't matter since we don't have one
			0);   //Stencil clear value, again, we don't have one, this value doesn't matter

		d3ddev->SetFVF(line_fvf);

		int k = 1500;
		for (int i = 0; i < 1; i++)
		{
			randomLines(k);
			d3ddev->DrawPrimitiveUP(D3DPT_LINESTRIP,         //PrimitiveType
				k - 1,              //PrimitiveCount
				lines,            //pVertexStreamZeroData
				sizeof(line_vertex));   //VertexStreamZeroStride
		}
			
		wchar_t wfps[32];
		_itow(prevFps, wfps, 10);
		fps++;
		drawText(wfps, 1500, 800);

		hr = d3ddev->EndScene();
		//if (FAILED(hr))
		//	std::cout << j++ << "\n";
		hr = d3ddev->Present(NULL, NULL, NULL, NULL);
		//if (FAILED(hr))
		//	std::cout << j++ << "\n";
	}

	void OnTick(System::Object^ sender, System::EventArgs^ e)
	{
		this->SuspendLayout();
		OnPaint(nullptr);
	}
	void OnLoad(System::Object^ sender, System::EventArgs^ e)
	{

		initDirectX();
		loadFont(L"Candara", 16);
		//DoubleBuffered = true;
		//Timer^ tmr = gcnew Timer();
		//tmr->Tick += gcnew System::EventHandler(this, &Form1::OnTick);
		//tmr->Interval = 1;
		//tmr->Start();
		//startFps();
	}
};

int main(array<System::String ^> ^args)
{
	Application::Run(gcnew Form1());
    return 0;
}