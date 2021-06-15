/** WMI
  * @author Thomas Sparber (2016)
 **/

#pragma once

#include <sstream>
#include <string>

namespace Wmi
{
    struct WmiException
    {
        std::string errorMessage;
        long errorCode;


        WmiException(const std::string& str_errorMessage, long l_errorCode) :
            errorMessage(str_errorMessage),
            errorCode(l_errorCode)
        {
        }

        std::string hexErrorCode() const
        {
            std::stringstream ss;
            ss << "0x" << std::hex << errorCode;
            return ss.str();
        }
    };
};
