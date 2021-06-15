#include <Windows.h>
#include <wingdi.h>
#include <WinUser.h>


extern "C" __declspec(dllexport) int __cdecl GetDisplayRefreshRate(LPCSTR devname)
{
    DEVMODEA dev;
    bool hres = EnumDisplaySettingsA(devname, ENUM_CURRENT_SETTINGS, &dev);

    return hres ? dev.dmDisplayFrequency : 0;
}

