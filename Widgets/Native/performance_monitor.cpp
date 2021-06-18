#pragma once

#include <string>
#include <iostream>

#include "wmi.hpp"
#include "wmiresult.hpp"

// #include "nameof.hpp"

using namespace Wmi;


std::string errorMessage;
long errorCode;


struct Win32_Processor
{
    int32_t CurrentClockSpeed;
    int16_t LoadPercentage;
    int32_t MaxClockSpeed;


    Win32_Processor()
        : CurrentClockSpeed()
        , LoadPercentage()
        , MaxClockSpeed()
    {
    }

    void setProperties(const WmiResult& result, std::size_t index)
    {
        result.extract(index, "CurrentClockSpeed", &this->CurrentClockSpeed);
        result.extract(index, "LoadPercentage", &this->LoadPercentage);
        result.extract(index, "MaxClockSpeed", &this->MaxClockSpeed);
    }

    static std::string getWmiClassName()
    {
        return "Win32_Processor";
    }
};

struct MSAcpi_ThermalZoneTemperature
{
    int32_t CurrentTemperature;


    void setProperties(const WmiResult& result, std::size_t index)
    {
        result.extract(index, "CurrentTemperature", &this->CurrentTemperature);
    }

    static std::string getWmiClassName()
    {
        return "MSAcpi_ThermalZoneTemperature";
    }
};



extern "C" __declspec(dllexport) Win32_Processor __cdecl fetch_proc()
{
    return retrieveWmi<Win32_Processor>();
}

extern "C" __declspec(dllexport) float __cdecl fetch_temp()
{
    MSAcpi_ThermalZoneTemperature temp = retrieveWmi<MSAcpi_ThermalZoneTemperature>();

    return (temp.CurrentTemperature / 10.0f) - 273.15f;
}
