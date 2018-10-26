#pragma once

#include "arcore_c_api.h"
#include "Unity/UnityXRTrackable.h"

class WrappedAnchorList;
class WrappedPose;

class WrappedAnchor
{
public:
    WrappedAnchor();
    WrappedAnchor(const ArAnchor* arAnchor);

    operator const ArAnchor*() const;
    const ArAnchor* Get() const;

    void GetPose(ArPose* arPose) const;
    void GetPose(UnityXRPose& xrPose) const;
    UnityXRTrackingState GetTrackingState() const;    

protected:
    const ArAnchor* m_ArAnchor;
};

class WrappedAnchorMutable : public WrappedAnchor
{
public:
    WrappedAnchorMutable();
    WrappedAnchorMutable(ArAnchor* arAnchor);

    operator ArAnchor*();
    ArAnchor* Get();

    void Detach();

protected:
    ArAnchor*& GetArAnchorMutable();
};

class WrappedAnchorRaii : public WrappedAnchorMutable
{
public:
    WrappedAnchorRaii();
    ~WrappedAnchorRaii();

    ArStatus TryAcquireAtPose(const ArPose* arPose);
    ArStatus TryAcquireAtPose(const UnityXRPose& xrPose);
    ArStatus TryAcquireAtTrackable(ArTrackable* arTrackable, ArPose* arPose);
    ArStatus TryAcquireAtTrackable(ArTrackable* arTrackable, const UnityXRPose& xrPose);
    void AcquireFromList(const ArAnchorList* arAnchorList, int32_t index);

    void Release();

	void AssumeOwnership(ArAnchor*& arAnchor);
	ArAnchor* TransferOwnership();

private:
	WrappedAnchorRaii(const WrappedAnchorRaii& original);
	WrappedAnchorRaii& operator=(const WrappedAnchorRaii& copyFrom);
};
