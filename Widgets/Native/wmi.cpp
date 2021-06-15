/** WMI
  * @author Thomas Sparber (2016)
 **/

#include <algorithm>
#include <stdio.h>
#include <comdef.h>
#include <functional>
#include <WbemCli.h>
#include <windows.h>

#include "wmiresult.hpp"
#include "wmi.hpp"

#pragma comment(lib, "wbemuuid.lib")


using std::function;
using std::string;
using std::wstring;
using std::wstringstream;
using std::transform;

using namespace Wmi;


IWbemLocator* createWbemLocator()
{
    IWbemLocator* pLocator = nullptr;
    HRESULT hr = CoCreateInstance(CLSID_WbemLocator, nullptr, CLSCTX_INPROC_SERVER, IID_IWbemLocator, (void**)&pLocator);
    if (FAILED(hr))
    {
        switch (hr)
        {
            case REGDB_E_CLASSNOTREG:	throw WmiException("Error initializing IWbemLocator: REGDB_E_CLASSNOTREG", hr);
            case CLASS_E_NOAGGREGATION:	throw WmiException("Error initializing IWbemLocator: CLASS_E_NOAGGREGATION", hr);
            case E_NOINTERFACE:			throw WmiException("Error initializing IWbemLocator: E_NOINTERFACE", hr);
            case E_POINTER:				throw WmiException("Error initializing IWbemLocator: E_POINTER", hr);
            default:					throw WmiException("Error initializing IWbemLocator: Unknown Error", hr);
        }
    }

    return pLocator;
}

IWbemServices* connect(IWbemLocator* pLocator)
{
    IWbemServices* pServices;
    HRESULT hr = pLocator->ConnectServer(_bstr_t("\\\\.\\root\\cimv2"), nullptr, nullptr, nullptr, 0, nullptr, nullptr, &pServices);
    if (FAILED(hr))
    {
        switch (hr)
        {
            case WBEM_E_ACCESS_DENIED:		throw WmiException("Error initializing IWbemServices: WBEM_E_ACCESS_DENIED", hr);
            case WBEM_E_FAILED:				throw WmiException("Error initializing IWbemServices: WBEM_E_FAILED", hr);
            case WBEM_E_INVALID_NAMESPACE:	throw WmiException("Error initializing IWbemServices: WBEM_E_INVALID_NAMESPACE", hr);
            case WBEM_E_INVALID_PARAMETER:	throw WmiException("Error initializing IWbemServices: WBEM_E_INVALID_PARAMETER", hr);
            case WBEM_E_OUT_OF_MEMORY:		throw WmiException("Error initializing IWbemServices: WBEM_E_OUT_OF_MEMORY", hr);
            case WBEM_E_TRANSPORT_FAILURE:	throw WmiException("Error initializing IWbemServices: WBEM_E_TRANSPORT_FAILURE", hr);
            case WBEM_E_LOCAL_CREDENTIALS:	throw WmiException("Error initializing IWbemServices: WBEM_E_LOCAL_CREDENTIALS", hr);
            default:						throw WmiException("Error initializing IWbemServices: Unknown Error", hr);
        }
    }

    //Set authentication proxy
    hr = CoSetProxyBlanket(pServices,
        RPC_C_AUTHN_DEFAULT,
        RPC_C_AUTHZ_NONE,
        COLE_DEFAULT_PRINCIPAL,
        RPC_C_AUTHN_LEVEL_DEFAULT,
        RPC_C_IMP_LEVEL_IMPERSONATE,
        nullptr,
        EOAC_NONE
    );

    if (FAILED(hr))
    {
        switch (hr)
        {
            case E_INVALIDARG:	throw WmiException("Coult not set proxy blanket: E_INVALIDARG", hr);
            default:			throw WmiException("Coult not set proxy blanket: Unknown Error", hr);
        }
    }

    return pServices;
}

IEnumWbemClassObject* execute(IWbemServices* pServices, const string& q)
{
    IEnumWbemClassObject* pClassObject;
    HRESULT hr = pServices->ExecQuery(_bstr_t("WQL"), _bstr_t(q.c_str()), WBEM_FLAG_RETURN_IMMEDIATELY | WBEM_FLAG_FORWARD_ONLY, nullptr, &pClassObject);
    if (FAILED(hr))
    {
        switch (hr)
        {
            case WBEM_E_ACCESS_DENIED:		throw WmiException("Error executing query: WBEM_E_ACCESS_DENIED", hr);
            case WBEM_E_FAILED:				throw WmiException("Error executing query: WBEM_E_FAILED", hr);
            case WBEM_E_INVALID_CLASS:		throw WmiException("Error executing query: WBEM_E_INVALID_CLASS", hr);
            case WBEM_E_INVALID_PARAMETER:	throw WmiException("Error executing query: WBEM_E_INVALID_PARAMETER", hr);
            case WBEM_E_OUT_OF_MEMORY:		throw WmiException("Error executing query: WBEM_E_OUT_OF_MEMORY", hr);
            case WBEM_E_SHUTTING_DOWN:		throw WmiException("Error executing query: WBEM_E_SHUTTING_DOWN", hr);
            case WBEM_E_TRANSPORT_FAILURE:	throw WmiException("Error executing query: WBEM_E_TRANSPORT_FAILURE", hr);
            default:						throw WmiException("Error executing query: Unknown Error", hr);
        }
    }

    return pClassObject;
}

void foreachObject(IEnumWbemClassObject* pClassObject, function<bool(IWbemClassObject*)> fn)
{
    bool cont = true;
    HRESULT hr = WBEM_S_NO_ERROR;

    //The final Next will return WBEM_S_FALSE
    while (cont && hr == WBEM_S_NO_ERROR)
    {
        ULONG uReturned = 0;
        IWbemClassObject* apObj;
        hr = pClassObject->Next(WBEM_INFINITE, 1, &apObj, &uReturned);

        if (FAILED(hr))
        {
            switch (hr)
            {
                case WBEM_S_FALSE: break;
                case WBEM_E_INVALID_PARAMETER:	throw WmiException("Error getting next element: WBEM_E_INVALID_PARAMETER", hr);
                case WBEM_E_OUT_OF_MEMORY:		throw WmiException("Error getting next element: WBEM_E_OUT_OF_MEMORY", hr);
                case WBEM_E_UNEXPECTED:			throw WmiException("Error getting next element: WBEM_E_UNEXPECTED", hr);
                case WBEM_E_TRANSPORT_FAILURE:	throw WmiException("Error getting next element: WBEM_E_TRANSPORT_FAILURE", hr);
                case WBEM_S_TIMEDOUT:			throw WmiException("Error getting next element: WBEM_S_TIMEDOUT", hr);
                default:						throw WmiException("Error getting next element: Unknown Error", hr);
            }
        }

        if (uReturned == 1)
        {
            cont = (cont && fn(apObj));
            apObj->Release();
        }
    }
}

wstring convertVariant(const VARIANT& value)
{
    bool handled = true;
    std::wstringstream ss;

    switch (value.vt)
    {
        case VT_EMPTY:
            break;
        case VT_NULL:
            ss << "NULL";
            break;
        case VT_I2:
            ss << value.iVal;
            break;
        case VT_I4:
            ss << value.lVal;
            break;
        case VT_R4:
            ss << value.fltVal;
            break;
        case VT_R8:
            ss << value.dblVal;
            break;
        case VT_BSTR:
            ss << value.bstrVal;
            break;
        case VT_BOOL:
            ss << (value.boolVal ? "true" : "false");
            break;
        case VT_DECIMAL:
            ss << value.pdecVal;
            break;
        case VT_I1:
            ss << value.cVal;
            break;
        case VT_UI1:
            ss << value.bVal;
            break;
        case VT_UI2:
            ss << value.uiVal;
            break;
        case VT_UI4:
            ss << value.ulVal;
            break;
        case VT_I8:
            ss << value.llVal;
            break;
        case VT_UI8:
            ss << value.ullVal;
            break;
        case VT_INT:
            ss << value.intVal;
            break;
        case VT_UINT:
            ss << value.uintVal;
            break;
        case VT_VOID:
            break;


        case VT_CY:				throw WmiException("Data type not yet supported: VT_CY", value.vt);
        case VT_DATE:			throw WmiException("Data type not yet supported: VT_DATE", value.vt);
        case VT_DISPATCH:		throw WmiException("Data type not yet supported: VT_DISPATCH", value.vt);
        case VT_ERROR:			throw WmiException("Data type not yet supported: VT_ERROR", value.vt);
        case VT_VARIANT:		throw WmiException("Data type not yet supported: VT_VARIANT", value.vt);
        case VT_UNKNOWN:		throw WmiException("Data type not yet supported: VT_UNKNOWN", value.vt);
        case VT_HRESULT:		throw WmiException("Data type not yet supported: VT_HRESULT", value.vt);
        case VT_PTR:			throw WmiException("Data type not yet supported: VT_PTR", value.vt);
        case VT_SAFEARRAY:		throw WmiException("Data type not yet supported: VT_SAFEARRAY", value.vt);
        case VT_CARRAY:			throw WmiException("Data type not yet supported: VT_CARRAY", value.vt);
        case VT_USERDEFINED:	throw WmiException("Data type not yet supported: VT_USERDEFINED", value.vt);
        case VT_RECORD:			throw WmiException("Data type not yet supported: VT_RECORD", value.vt);
        case VT_INT_PTR:		throw WmiException("Data type not yet supported: VT_INT_PTR", value.vt);
        case VT_UINT_PTR:		throw WmiException("Data type not yet supported: VT_UINT_PTR", value.vt);
        case VT_LPSTR:			throw WmiException("Data type not yet supported: VT_LPSTR", value.vt);
        case VT_LPWSTR:			throw WmiException("Data type not yet supported: VT_LPWSTR", value.vt);
        default:
            handled = false;
            break;
    }

    if ((value.vt & VT_ARRAY) != 0)
    {
        long lLower, lUpper;
        SafeArrayGetLBound(value.parray, 1, &lLower);
        SafeArrayGetUBound(value.parray, 1, &lUpper);

        ss << "[";
        for (long i = lLower; i <= lUpper; ++i)
        {
            /*VARIANT inner;
            HRESULT hr = SafeArrayGetElement(value.parray, &i, &inner);
            inner.vt = value.vt ^ VT_ARRAY;

            if(!FAILED(hr))
            {
                ss<<convertVariant(inner);
            }*/

            if (i != lUpper)ss << ",";
        }
        ss << "]";
        handled = true;
    }
    else if ((value.vt & VT_BYREF) != 0)
    {
        handled = true;
    }

    if (!handled)
    {
        throw WmiException("Unknown data type", value.vt);
    }

    return ss.str();
}

void foreachProperty(IWbemClassObject* object, function<bool(const wstring&, const std::wstring&)> fn)
{
    SAFEARRAY* psaNames = nullptr;
    HRESULT hr = object->GetNames(nullptr, WBEM_FLAG_ALWAYS | WBEM_FLAG_NONSYSTEM_ONLY, nullptr, &psaNames);

    if (FAILED(hr))
    {
        switch (hr)
        {
            case WBEM_E_FAILED:				throw WmiException("Could not get properties: WBEM_E_FAILED", hr);
            case WBEM_E_INVALID_PARAMETER:	throw WmiException("Could not get properties: WBEM_E_INVALID_PARAMETER", hr);
            case WBEM_E_OUT_OF_MEMORY:		throw WmiException("Could not get properties: WBEM_E_OUT_OF_MEMORY", hr);
            default:						throw WmiException("Could not get properties: WBEM_E_FAILED", hr);
        }
    }

    long lLower, lUpper;
    BSTR propName = nullptr;
    SafeArrayGetLBound(psaNames, 1, &lLower);
    SafeArrayGetUBound(psaNames, 1, &lUpper);

    for (long i = lLower; i <= lUpper; ++i)
    {
        hr = SafeArrayGetElement(psaNames, &i, &propName);

        if (FAILED(hr))
        {
            switch (hr)
            {
                case DISP_E_BADINDEX:	throw WmiException("Could not get name from SafeArray: DISP_E_BADINDEX", hr);
                case E_INVALIDARG:		throw WmiException("Could not get name from SafeArray: E_INVALIDARG", hr);
                case E_OUTOFMEMORY:		throw WmiException("Could not get name from SafeArray: E_OUTOFMEMORY", hr);
                default:				throw WmiException("Could not get name from SafeArray: Unknown Error", hr);
            }
        }

        VARIANT value;
        hr = object->Get(propName, 0, &value, nullptr, nullptr);

        if (FAILED(hr))
        {
            switch (hr)
            {
                case WBEM_E_FAILED:				throw WmiException("Could not get property: WBEM_E_FAILED", hr);
                case WBEM_E_INVALID_PARAMETER:	throw WmiException("Could not get property: WBEM_E_INVALID_PARAMETER", hr);
                case WBEM_E_NOT_FOUND:			throw WmiException("Could not get property: WBEM_E_NOT_FOUND", hr);
                case WBEM_E_OUT_OF_MEMORY:		throw WmiException("Could not get property: WBEM_E_OUT_OF_MEMORY", hr);
                default:						throw WmiException("Could not get property: Unknown Error", hr);
            }
        }

        bool cont;
        try
        {
            cont = fn(propName, convertVariant(value));
        }
        catch (const WmiException& e)
        {
            wstring temp(propName);
            throw WmiException(string("Can't convert parameter: ") + string(temp.begin(), temp.end()) + ": " + e.errorMessage, e.errorCode);
        }

        VariantClear(&value);
        SysFreeString(propName);
        if (!cont)break;
    }

    SafeArrayDestroy(psaNames);
}

void Wmi::query(const string& q, WmiResult& out)
{
    CoInitialize(nullptr);

    IWbemLocator* pLocator;
    IWbemServices* pServices;
    IEnumWbemClassObject* pClassObject;

    //Create the WBEM locator
    try
    {
        pLocator = createWbemLocator();
    }
    catch (const WmiException&)
    {
        CoUninitialize();
        throw;
    }

    //Open connection to computer
    try
    {
        pServices = connect(pLocator);
    }
    catch (const WmiException&)
    {
        pLocator->Release();
        CoUninitialize();
        throw;
    }

    //Execute the query
    try
    {
        pClassObject = execute(pServices, q);
    }
    catch (const WmiException&)
    {
        pServices->Release();
        pLocator->Release();
        CoUninitialize();
        throw;
    }

    try
    {
        std::size_t index = 0;

        foreachObject(pClassObject, [&out, &index](IWbemClassObject* object)
            {
                foreachProperty(object, [&out, index](const wstring& name, const std::wstring& value)
                    {
                        out.set(index, name, value);
                        return true;
                    });
                index++;
                return true;
            });
    }
    catch (const WmiException&)
    {
        pServices->Release();
        pLocator->Release();
        CoUninitialize();
        throw;
    }

    pClassObject->Release();

    pServices->Release();
    pLocator->Release();
    CoUninitialize();
}

void WmiResult::set(std::size_t index, wstring name, const wstring& value)
{
    while (index >= result.size())result.emplace_back();

    transform(name.begin(), name.end(), name.begin(), ::tolower);
    result[index][name] = value;
}

bool WmiResult::extract(std::size_t index, const string& name, wstring* out) const
{
    if (index >= result.size())return false;

    wstring key(name.cbegin(), name.cend());
    transform(key.begin(), key.end(), key.begin(), ::tolower);

    auto found = result[index].find(key);

    if (found == result[index].cend())
        return false;

    *out = found->second;

    return true;
}

bool WmiResult::extract(std::size_t index, const string& name, string* out) const
{
    wstring temp;
    if (!extract(index, name, &temp))
        return false;

    *out = string(temp.cbegin(), temp.cend());

    return true;
}

template<typename T>
bool WmiResult::extract(std::size_t index, const string& name, T* out, bool(*cast)(string, T*)) const
{
    bool success = false;
    string temp;

    if (extract(index, name, &temp))
        return cast(temp, out);

    return success;
}

bool WmiResult::extract(std::size_t index, const string& name, bool* out) const
{
    return extract<bool>(index, name, out, [](string temp, bool* out)
    {
        transform(temp.begin(), temp.end(), temp.begin(), ::tolower);

        if (temp == "true" || temp == "1")
            *out = true;
        else if (temp == "false" || temp == "0")
            *out = false;
        else
            return false;

        return true;
    });
}

bool WmiResult::extract(std::size_t index, const string& name, int16_t* out) const
{
    return extract<int16_t>(index, name, out, [](string temp, int16_t* out)
    {
        char* test;
        *out = (int32_t)strtol(temp.c_str(), &test, 0);
        return (test == temp.c_str() + temp.length());
    });
}

bool WmiResult::extract(std::size_t index, const string& name, int32_t* out) const
{
    return extract<int32_t>(index, name, out, [](string temp, int32_t* out)
    {
        char* test;
        *out = strtol(temp.c_str(), &test, 0);
        return (test == temp.c_str() + temp.length());
    });
}

bool WmiResult::extract(std::size_t index, const string& name, int64_t* out) const
{
    return extract<int64_t>(index, name, out, [](string temp, int64_t* out)
    {
        char* test;
        *out = strtoull(temp.c_str(), &test, 0);
        return (test == temp.c_str() + temp.length());
    });
}
