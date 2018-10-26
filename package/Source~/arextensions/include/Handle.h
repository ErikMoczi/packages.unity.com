#pragma once

#include <functional>

namespace CameraImageApi
{

// A strongly typed handle. T is the storage for the handle, e.g., int.
// STRONG_TYPE can be anything; it is only used to ensure this type of
// handle cannot be assigned to a handle with a different STRONG_TYPE.
template<typename T, typename STRONG_TYPE>
struct Handle
{
    static constexpr T kInvalid = 0;

    static Handle Invalid()
    {
        return Handle(kInvalid);
    }

    T Value() const { return m_Value; }
    bool IsValid() const { return m_Value != kInvalid; }

    explicit Handle(T value)
        : m_Value(value)
    { }

    Handle() = default;
    Handle(const Handle&) = default;
    Handle(Handle&&) = default;
    Handle& operator=(const Handle&) = default;
    Handle& operator=(Handle&&) = default;

    bool operator == (const Handle& other) const
    {
        return m_Value == other.m_Value;
    }

    class Generator
    {
    public:
        Handle Next()
        {
            if (++m_LastHandle == kInvalid)
                ++m_LastHandle;
            return Handle(m_LastHandle);
        }

    private:
        T m_LastHandle = kInvalid;
    };

private:
    T m_Value = kInvalid;
};

} // namespace CameraImageApi

namespace std
{
    template <typename T, typename STRONG_TYPE>
    struct hash<CameraImageApi::Handle<T, STRONG_TYPE>>
    {
        size_t operator()(const CameraImageApi::Handle<T, STRONG_TYPE>& handle) const
        {
            return hash<T>()(handle.Value());
        }
    };
}
