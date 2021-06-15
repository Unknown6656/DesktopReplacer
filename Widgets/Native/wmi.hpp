/** WMI
  * @author Thomas Sparber (2016)
 **/

#pragma once

#include <string>

#include "wmiexception.hpp"
#include "wmiresult.hpp"


namespace Wmi
{
    /*class WmiClass
    {
    public:
        virtual void setProperties(const WmiResult&, std::size_t) const = 0;
        static std::string getWmiClassName();
    };*/


    void query(const std::string& q, WmiResult& out);

    inline WmiResult query(const std::string& q)
    {
        WmiResult result;
        query(q, result);
        return std::move(result);
    }

    // template<class T, typename std::enable_if<std::is_base_of<WmiClass, T>::value>::type* = nullptr>
    template <class T>
    inline void retrieveWmi(T* out)
    {
        const std::string q = std::string("SELECT * FROM ") + T::getWmiClassName();
        WmiResult result;

        query(q, result);
        out->setProperties(result, 0);
    }

    template <class T>
    inline T retrieveWmi()
    {
        T temp;

        retrieveWmi(&temp);
        
        return std::move(temp);
    }

    template <class T>
    inline void retrieveAllWmi(std::vector<T>& out)
    {
        const std::string q = std::string("Select * From ") + T::getWmiClassName();
        T result;

        query(q, result);
        out.clear();

        for (std::size_t index = 0; index < result.size(); ++index)
        {
            T temp;

            temp.setProperties(result, index);
            out.push_back(std::move(temp));
        }
    }

    template <class T>
    inline std::vector<T> retrieveAllWmi()
    {
        std::vector<T> ret;
        retrieveAllWmi(ret);

        return std::move(ret);
    }
};
