/**
  *
  * WMI
  * @author Thomas Sparber (2016)
  *
 **/

#pragma once

#include <map>
#include <string>
#include <vector>

namespace Wmi
{
    class WmiResult
    {
    public:
        WmiResult() :
            result()
        {
        }

        void set(std::size_t index, std::wstring name, const std::wstring& value);

        std::vector<std::map<std::wstring, std::wstring>>::iterator begin()
        {
            return result.begin();
        }

        std::vector<std::map<std::wstring, std::wstring>>::iterator end()
        {
            return result.end();
        }

        std::vector<std::map<std::wstring, std::wstring>>::const_iterator cbegin() const
        {
            return result.cbegin();
        }

        std::vector<std::map<std::wstring, std::wstring>>::const_iterator cend() const
        {
            return result.cend();
        }

        std::size_t size() const
        {
            return result.size();
        }

        template<typename T>
        bool extract(std::size_t index, const std::string& name, T* out, bool(*cast)(std::string, T*)) const;
        bool extract(std::size_t index, const std::string& name, std::wstring* out) const;
        bool extract(std::size_t index, const std::string& name, std::string* out) const;
        bool extract(std::size_t index, const std::string& name, bool* out) const;
        bool extract(std::size_t index, const std::string& name, int16_t* out) const;
        bool extract(std::size_t index, const std::string& name, int32_t* out) const;
        bool extract(std::size_t index, const std::string& name, int64_t* out) const;

    private:
        std::vector<std::map<std::wstring, std::wstring>> result;
    };
};
