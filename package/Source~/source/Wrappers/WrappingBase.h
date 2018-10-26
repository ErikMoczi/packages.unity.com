#pragma once

#include <cstddef>

template<typename T>
class WrappingBase;

template<typename T>
class WrappingRef
{
private:
    WrappingBase<T>& m_ArPtr;

    WrappingRef(WrappingBase<T>& arPtr)
        : m_ArPtr(arPtr)
    {
    }

    friend class WrappingBase<T>;

public:
    typedef T UnderlyingType;

    inline operator T**()
    {
        return m_ArPtr.ReleaseAndGetAddressOf();
    }

    inline T* operator*() throw ()
    {
        return m_ArPtr;
    }

    inline T* const* GetAddressOf() const throw ()
    {
        return m_ArPtr.GetAddressOf();
    }

    inline T** ReleaseAndGetAddressOf() throw ()
    {
        return m_ArPtr.ReleaseAndGetAddressOf();
    }
};

enum eWrappedConstruction
{
    Default
};

template<typename T>
class WrappingBase
{
public:
    typedef T UnderlyingType;

    inline WrappingBase()
        : m_Ptr(nullptr)
        , m_RefCount(nullptr)
    {}

    inline ~WrappingBase()
    {
        Release();
    }

    WrappingBase(const WrappingBase& original)
        : m_Ptr(nullptr)
        , m_RefCount(nullptr)
    {
        Clone(original);
    }

    const WrappingBase& operator=(const WrappingBase& ptr)
    {
        Clone(ptr);
        return *this;
    }

    inline WrappingBase& operator=(std::nullptr_t)
    {
        Release();
        return *this;
    }

    inline bool operator!() const
    {
        return nullptr == m_Ptr;
    }

    inline operator const T*() const
    {
        return m_Ptr;
    }

    inline operator T*()
    {
        return m_Ptr;
    }

    inline T& operator*()
    {
        Assert(nullptr != m_Ptr);
        return *m_Ptr;
    }

    inline WrappingRef<T> operator&()
    {
        return WrappingRef<T>(*this);
    }

    inline void AssumeOwnership(T* ptr)
    {
        Release();
        m_Ptr = ptr;
        InitRefCount();
    }

    inline T* Get() const
    {
        return m_Ptr;
    }

    inline T** ReleaseAndGetAddressOf()
    {
        Release();
        return &m_Ptr;
    }

    inline T* const* GetAddressOf()
    {
        return &m_Ptr;
    }

    inline bool operator==(std::nullptr_t) const
    {
        return m_Ptr == nullptr;
    }

    inline bool operator!=(std::nullptr_t) const
    {
        return m_Ptr != nullptr;
    }

    void Release()
    {
        if (m_Ptr == nullptr)
            return;

        --(*m_RefCount);
        if (*m_RefCount == 0)
        {
            ReleaseImpl();
            delete m_RefCount;
        }

        m_RefCount = nullptr;
        m_Ptr = nullptr;
    }

protected:
    void CreateOrAcquireDefault()
    {
        Release();
        CreateOrAcquireDefaultImpl();
        InitRefCount();
    }

    void InitRefCount()
    {
        m_RefCount = new int(1);
    }

    T* m_Ptr;
    int* m_RefCount;

private:
    void Clone(const WrappingBase& ptr)
    {
        Release();
        if (ptr == nullptr)
            return;

        m_Ptr = ptr.m_Ptr;
        m_RefCount = ptr.m_RefCount;
        ++(*m_RefCount);
    }

    void CreateOrAcquireDefaultImpl();
    void ReleaseImpl();
};
