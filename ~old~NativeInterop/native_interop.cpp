#pragma once

#include <Windows.h>
#include <wingdi.h>
#include <WinUser.h>
#include <vcclr.h>


#define TO_LPCWSTR(clr_str) pin_ptr<const wchar_t>(PtrToStringChars(clr_str))

typedef System::String clrstring;


namespace DesktopReplacer::Native
{
    public ref class NativeInterop
    {
    public:
        static DEVMODEW GetDisplayInfo(clrstring^ devname)
        {
            DEVMODEW dev;
            bool hres = EnumDisplaySettingsW(TO_LPCWSTR(devname), ENUM_CURRENT_SETTINGS, &dev);

            if (!hres)
                dev.dmSize = 0;

            return dev;
        }

        static int GetDisplayRefreshRate(clrstring^ devname)
        {
            DEVMODEW dev = GetDisplayInfo(devname);

            return dev.dmSize ? dev.dmDisplayFrequency : 0;
        }
    };
};
