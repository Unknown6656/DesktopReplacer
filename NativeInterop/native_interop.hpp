#pragma once

#include "wmi.hpp"

#define TO_LPCWSTR(clr_str) pin_ptr<const wchar_t>(PtrToStringChars(clr_str))


typedef System::String clrstring;


namespace NativeInterop
{
    struct Win32_Processor
    {
        int32_t CurrentClockSpeed;
        int16_t LoadPercentage;
        int32_t MaxClockSpeed;


        Win32_Processor() noexcept
            : CurrentClockSpeed()
            , LoadPercentage()
            , MaxClockSpeed()
        {
        }

        void setProperties(const Wmi::WmiResult& result, std::size_t index) noexcept
        {
            result.extract(index, "CurrentClockSpeed", &this->CurrentClockSpeed);
            result.extract(index, "LoadPercentage", &this->LoadPercentage);
            result.extract(index, "MaxClockSpeed", &this->MaxClockSpeed);
        }

        static std::string getWmiClassName() noexcept
        {
            return "Win32_Processor";
        }
    };

    struct MSAcpi_ThermalZoneTemperature
    {
        int32_t CurrentTemperature;


        void setProperties(const Wmi::WmiResult& result, std::size_t index) noexcept
        {
            result.extract(index, "CurrentTemperature", &this->CurrentTemperature);
        }

        static std::string getWmiClassName() noexcept
        {
            return "MSAcpi_ThermalZoneTemperature";
        }
    };

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

        static Win32_Processor __cdecl fetch_processor()
        {
            return Wmi::retrieveWmi<Win32_Processor>();
        }

        static float __cdecl fetch_temperature()
        {
            MSAcpi_ThermalZoneTemperature temp = Wmi::retrieveWmi<MSAcpi_ThermalZoneTemperature>();

            return (temp.CurrentTemperature / 10.0f) - 273.15f;
        }
    };
}
