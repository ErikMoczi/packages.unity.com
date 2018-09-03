#ifndef THIRD_PARTY_ARCORE_AR_CORE_C_API_ARCORE_C_API_EXPERIMENTAL_H_
#define THIRD_PARTY_ARCORE_AR_CORE_C_API_ARCORE_C_API_EXPERIMENTAL_H_

#include "arcore_c_api.h"

#ifdef __cplusplus
extern "C" {
#endif

// If compiling for c++11, use the 'enum underlying type' feature to enforce
// size for ABI compatibility. In pre-c++11, use int32_t for fixed size.
#if __cplusplus >= 201100
#define AR_DEFINE_ENUM(_type) enum _type : int32_t
#else
#define AR_DEFINE_ENUM(_type) \
  typedef int32_t _type;      \
  enum
#endif

// This function demonstrates how to add an experimental API function to an
// existing object (ArSession).
/*DYNAMITE_FUNCTION_0*/
void ArSession_exampleApi_experimental(ArSession* session);

// Similar to ArSession_create(), this is bundled in SDK as it serves as an
// entry point to dynamite. It is implemented in session_create.cc to share
// the dynamite infrastructure.
// Return values are the same as ArSession_create().
ArStatus ArSession_createWithSettings_experimental(
    void* env, void* application_context, const char* settings_name_value_pairs,
    ArSession** out_session_pointer);

// ArCore static function to load remote Java classes from Dynamite remote
// context, currently it's an experimental function for experimental features
// need accessing Java classes in ARCore.apk.
// It is bundled in SDK and implemented in session_create.cc(when
// ENABLE_EXPERIMENTAL is defined) to share the dynamite infrastructure.
// Return values:
// AR_SUCCESS
// AR_ERROR_INVALID_ARGUMENT
// AR_ERROR_FATAL
ArStatus ArSdk_getRemoteJavaClass_experimental(void* void_env,
                                               const ArSession* session,
                                               const char* class_name,
                                               void** out_remote_class_pointer);

// Implements the Session creation with additional settings used at session
// construction time. This is to allow dependency injection to be configurable.
// 'settings' is a 'name=value\nname=value' string.
// TODO(b/70387399) design better ArSession_createWithSettings() experimental
// api. In particular, pass object with better structure instead of a loosely
// structured string. Return values are the same as
// ArSession_createImplementation().
/*DYNAMITE_FUNCTION_0*/
ArStatus ArSession_createWithSettingsImplementation_experimental(
    void* void_env, void* void_application_context, void* remote_class_loader,
    const char* sdk_version, const char* settings_name_value_pairs,
    ArSession** out_session_pointer);

// === Camera intrinstics types and methods ===
// The physical characteristics of a given camera.
//
// Allocate with ArCameraIntrinsics_create()<br>
// Populate with ArCamera_getIntrinsics()<br>
// Release with ArCameraIntrinsics_destroy()
typedef struct ArCameraIntrinsics_ ArCameraIntrinsics;

AR_DEFINE_ENUM(ArCameraDistortionModel){
    AR_CAMERA_DISTORTION_MODEL_UNKNOWN = 0, AR_CAMERA_DISTORTION_MODEL_NONE = 1,

    // The FOV camera model with (fx, fy, cx, cy, w) described in
    // <a href="https://scholar.google.com/scholar?cluster=9093137934172132605">
    // Parallel tracking and mapping for small AR workspaces</a>a
    // The parameter vector represents the distortion as [w]..
    // w: Fisheye-ness parameter. A value of w = 0.0 describes a pinhole camera;
    //    a value of w = 2*atan(0.5) = 0.927295... describes an equidistant
    //    fisheye camera.
    AR_CAMERA_DISTORTION_MODEL_EQUIDISTANT = 2,

    // The FOV camera model with (fx, fy, cx, cy, w, k1) described in
    // <a href="https://scholar.google.com/scholar?cluster=9093137934172132605">
    // Parallel tracking and mapping for small AR workspaces</a>.
    // The parameter vector represents the distortion as [w, k1].
    // w: Same as AR_CAMERA_DISTORTION_MODEL_EQUIDISTANT.
    // k1: Additional cubic distortion coefficient.
    AR_CAMERA_DISTORTION_MODEL_EQUIDISTANT_POLY_1_PARAM = 3,

    // Brown's distortion model, with the parameter vector representing the
    // distortion as [k1, k2].
    AR_CAMERA_DISTORTION_MODEL_BROWNS_POLY_2_PARAMS = 4,

    // Brown's distortion model, with the parameter vector representing the
    // distortion as [k1, k2, k3].
    AR_CAMERA_DISTORTION_MODEL_BROWNS_POLY_3_PARAMS = 5,

    // Brown's distortion model, with the parameter vector representing the
    // distortion as [k1, k2, p1, p2, k3].
    // Also known as opencv_pinhole_model.
    AR_CAMERA_DISTORTION_MODEL_BROWNS_POLY_5_PARAMS = 6};

// Allocates a camera intrinstics object.
/*DYNAMITE_FUNCTION_0*/
void ArCameraIntrinsics_create_experimental(
    const ArSession* session, ArCameraIntrinsics** out_camera_intrinsics);

// Retrieves the physical characteristics of the given camera.
// Returns the (rotated) camera characteristics for the "AR Preview" stream,
// which may be the 1920x1080 one.
// @param camera This can be retrieved from any frame. In the future, if the
// camera intrinsics should change per-frame, the values returned per-frame may
// be distinct.
// @param display_rotation Number of CCW turns by 90 degrees from the native
// device rotation (same as Android conventions in
// android.view.Surface#ROTATION_0, etc.).
/*DYNAMITE_FUNCTION_0*/
void ArCamera_getIntrinsics_experimental(
    const ArSession* session, const ArCamera* camera, int32_t display_rotation,
    ArCameraIntrinsics* out_camera_intrinsics);

// Returns the focal length in pixels.
/*DYNAMITE_FUNCTION_0*/
void ArCameraIntrinsics_getFocalLength_experimental(
    const ArSession* session, const ArCameraIntrinsics* intrinsics,
    float* out_fx, float* out_fy);

// Returns the principal point in pixels.
/*DYNAMITE_FUNCTION_0*/
void ArCameraIntrinsics_getPrincipalPoint_experimental(
    const ArSession* session, const ArCameraIntrinsics* intrinsics,
    float* out_cx, float* out_cy);

// Returns the image's width and height in pixels.
/*DYNAMITE_FUNCTION_0*/
void ArCameraIntrinsics_getImageDimensions_experimental(
    const ArSession* session, const ArCameraIntrinsics* intrinsics,
    int32_t* out_width, int32_t* out_height);

// Returns the distortion model used in this camera's intrinsics.
/*DYNAMITE_FUNCTION_0*/
void ArCameraIntrinsics_getDistortionModel_experimental(
    const ArSession* session, const ArCameraIntrinsics* intrinsics,
    ArCameraDistortionModel* out_distortion_model);

// Retrieves the number of elements in the distortion coefficients' array.
/*DYNAMITE_FUNCTION_0*/
void ArCameraIntrinsics_getDistortionCoefficientsSize_experimental(
    const ArSession* session, const ArCameraIntrinsics* intrinsics,
    int32_t* out_distortion_coefficients_size);

// Returns the distortion coefficients used in this CameraIntrinsics'
// distortion model.
/*DYNAMITE_FUNCTION_0*/
void ArCameraIntrinsics_getDistortionCoefficients_experimental(
    const ArSession* session, const ArCameraIntrinsics* intrinsics,
    float* out_distortion_coefficients);

// Releases the provided camera intrinsics object.
/*DYNAMITE_FUNCTION_0*/
void ArCameraIntrinsics_destroy_experimental(
    ArCameraIntrinsics* camera_intrinsics);

// TODO(b/70180372): Add getter methods here.

// Gets the ID of the Anchor. The ID is blank for a standard local anchor, but
// is non-empty when the anchor has been hosted. Other AR clients can obtain
// a hosted anchor using its ID.
// This function will allocate memory for the ID string, and set *out_anchor_id
// to point to that string. The caller is expected to free the memory once it
// is no longer needed.
/*DYNAMITE_FUNCTION_0*/
void ArAnchor_getId_experimental(ArSession* session, ArAnchor* anchor,
                                 char** out_anchor_id);

// LINT.IfChange(host_status_enum)
AR_DEFINE_ENUM(ArHostStatus){AR_NOT_HOSTED = 0,
                             AR_HOST_IN_PROGRESS = 1,
                             AR_HOST_SUCCESS = 2,
                             AR_HOST_AUTH_FAILURE = 3,
                             AR_HOST_LOCALIZATION_FAILURE = 4,
                             AR_HOST_INTERNAL_ERROR = 5,
                             AR_HOST_SERVER_PROCESSING = 6,
                             AR_HOST_UNKNOWN_ERROR = 7};
// LINT.ThenChange(//depot/google3/third_party/arcore/java/com/google/ar/core/SessionExperimental.java:host_status_enum)

// Hosts the specified anchor in an asynchronous operation. This function may
// modify some metadata of the anchor to reflect its hosted state. Once the
// hosting task is completed, the anchor's host status will be updated to
// reflect that.
/*DYNAMITE_FUNCTION_0*/
void ArSession_hostAnchor_experimental(ArSession* session, ArAnchor* anchor);

// Resolves a previously hosted anchor using the given anchor ID. The anchor
// returned immediately has an undefined pose and tracking state, and will be
// updated when the server query returns. The host status of the anchor will
// also be updated accordingly.
/*DYNAMITE_FUNCTION_0*/
void ArSession_resolveHostedAnchor_experimental(ArSession* session,
                                                const char* anchor_id,
                                                ArAnchor** out_anchor);

// Gets the host status of the anchor.
/*DYNAMITE_FUNCTION_0*/
ArHostStatus ArAnchor_getHostStatus(ArSession* session, const ArAnchor* anchor);

// Experimental trackable type for tracked images.
#define AR_EXPERIMENTAL_TRACKABLE_TRACKED_IMAGE ((ArTrackableType)0x41520104)

// Experimental trackable type for faces.
#define AR_EXPERIMENTAL_TRACKABLE_FACE ((ArTrackableType)0x41520105)

// === ArTrackedImageDatabase methods ===
/// A database of images to be tracked by ARCore (@ref ownership "value type").
///
/// Create with ArTrackedImageDatabase_create_experimental() or
/// ArTrackedImageDatabase_deserialize_experimental()<br>
/// Release with: ArTrackedImageDatabase_destroy_experimental()
typedef struct ArTrackedImageDatabase_ ArTrackedImageDatabase;

/// Creates a new empty tracked image database.
/*DYNAMITE_FUNCTION_0*/
void ArTrackedImageDatabase_create_experimental(
    const ArSession* session,
    ArTrackedImageDatabase** out_tracked_image_database);

/// Creates a new tracked image database from a byte array. The contents of the
/// byte array must have been generated by the database builder developer tool.
///
/// @return #AR_SUCCESS or any of:
/// - #AR_ERROR_INVALID_ARGUMENT: Invalid format for database_raw_bytes.
/*DYNAMITE_FUNCTION_0*/
ArStatus ArTrackedImageDatabase_deserialize_experimental(
    const ArSession* session, const uint8_t* database_raw_bytes,
    int32_t database_raw_bytes_size,
    ArTrackedImageDatabase** out_tracked_image_database);

/// Adds a single image to an image database, from an array of grayscale pixel
/// values. Returns the positional index of the image within the image database.
///
/// @return #AR_SUCCESS or any of:
/// - #AR_ERROR_INVALID_ARGUMENT
/*DYNAMITE_FUNCTION_0*/
ArStatus ArTrackedImageDatabase_addImage_experimental(
    const ArSession* session, ArTrackedImageDatabase* tracked_image_database,
    const uint8_t* image_grayscale_pixels, int32_t image_width_in_pixels,
    int32_t image_height_in_pixels, int32_t* out_index);

/// Returns the number of images in the tracked image database.
/*DYNAMITE_FUNCTION_0*/
void ArTrackedImageDatabase_getNumImages_experimental(
    const ArSession* session, ArTrackedImageDatabase* tracked_image_database,
    int32_t* out_num_images);

/// Releases memory used by a tracked image database.
/*DYNAMITE_FUNCTION_0*/
void ArTrackedImageDatabase_destroy_experimental(
    ArTrackedImageDatabase* tracked_image_database);

/// Sets the tracked image database in the session configuration.
/*DYNAMITE_FUNCTION_0*/
void ArConfig_setTrackedImageDatabase_experimental(
    ArConfig* config, ArTrackedImageDatabase* tracked_image_database);

// === ArTrackedImage methods ===
/// A detected tracked image trackable (@ref ownership "reference type,
/// long-lived").
///
/// Trackable type: #AR_EXPERIMENTAL_TRACKABLE_TRACKED_IMAGE <br>
/// Release with: ArTrackable_release()
typedef struct ArTrackedImage_ ArTrackedImage;

/// Returns the pose of the center of the detected tracked image. The pose's
/// transformed +Y axis will be point normal out of the tracked image.
/*DYNAMITE_FUNCTION_0*/
void ArTrackedImage_getCenterPose_experimental(
    const ArSession* session, const ArTrackedImage* tracked_image,
    ArPose* out_pose);

/// Retrieves the number of elements (not vertices) in the 3D boundary polygon.
/// The number of vertices is 1/3 this size.
/*DYNAMITE_FUNCTION_0*/
void ArTrackedImage_getBoundaryPolygonSize_experimental(
    const ArSession* session, const ArTrackedImage* tracked_image,
    int32_t* out_boundary_polygon_size);

/// Returns the 3D points of the boundary polygon for the detected tracked
/// image, in the form <tt>[x1, y1, z1, x2, y2, z2, ...]</tt>. These values are
/// in world coordinates.
///
/// @param[in]    session                The ARCore session.
/// @param[in]    tracked_image          The tracked image to retrieve the
///     boundary polygon from.
/// @param[inout] out_boundary_polygon   A pointer to an array of floats.  The
///     length of this array must be at least that reported by
///     ArTrackedImage_getBoundaryPolygonSize().
/*DYNAMITE_FUNCTION_0*/
void ArTrackedImage_getBoundaryPolygon_experimental(
    const ArSession* session, const ArTrackedImage* tracked_image,
    float* out_boundary_polygon);

/// Retrieves the length of this tracked image's boundary rectangle measured
/// along the local X-axis of the coordinate space defined by the output of
/// ArTrackedImage_getCenterPose().
/*DYNAMITE_FUNCTION_0*/
void ArTrackedImage_getExtentX_experimental(const ArSession* session,
                                            const ArTrackedImage* tracked_image,
                                            float* out_extent_x);

/// Retrieves the length of this tracked image's boundary rectangle measured
/// along the local Z-axis of the coordinate space defined by the output of
/// ArTrackedImage_getCenterPose().
/*DYNAMITE_FUNCTION_0*/
void ArTrackedImage_getExtentZ_experimental(const ArSession* session,
                                            const ArTrackedImage* tracked_image,
                                            float* out_extent_z);

/// Returns the (zero-based) index of this tracked image from its originating
/// tracked image database.
/*DYNAMITE_FUNCTION_0*/
void ArTrackedImage_getIndex_experimental(const ArSession* session,
                                          const ArTrackedImage* tracked_image,
                                          int32_t* out_index);

/// Returns the latest depth image and related information.
/// @param[in]    session                The ARCore session.
/// @param[inout] out_width              Width of the returned depth image in
///     pixels.
/// @param[inout] out_height             Height of the returned depth image in
///     pixels.
/// @param[inout] out_timestamp_ns       Center exposure timestamp of the latest
///     camera image that contributed to depth estimation.
/// @param[inout] out_pixels             Data of the latest depth image. Memory
///     for the image is allocated on each call and must be freed by the app.
///     The format used is 16-bit unsigned integer depth values in mm. If no
///     depth image has been computed yet, *out_pixels is set to null.
/// @param[inout] out_homography         A pointer to an array of doubles. The
///     length of this array must be at least 9. The values are set to the 3x3
///     row-major homography matrix between the camera pose of the returned
///     depth image and the camera pose of the latest ARFrame. The homography
///     matrix values are normalized w.r.t. image size.
/// NOTE(avirodov):
/// Caller releasing memory using free() is generally not ABI-compatible. For
/// example, if the app links to libc statically, the app free() will not be
/// same as arcore malloc(). We will need an explicit ARCore release() function
/// when this API is made public.
/*DYNAMITE_FUNCTION_0*/
void ArSession_getLatestDepthData_experimental(ArSession* session,
                                               int32_t* out_width,
                                               int32_t* out_height,
                                               int64_t* out_timestamp_ns,
                                               uint8_t** out_pixels,
                                               double* out_homography_3x3);

// Starts recording sensor data when the following conditions are true:
//   1. The session is created with the setting "enable_recording".
//   2. The session is in resumed state.
//   3. Recording is not already started.
// This will record IMU, features, mono images at full rate, and color image
// as fast as it can. The recorded data will be saved to "output_folder".
// Return values:
// AR_SUCCESS
// AR_ERROR_INVALID_ARGUMENT
// AR_ERROR_FATAL
/*DYNAMITE_FUNCTION_0*/
ArStatus ArSession_startRecording_experimental(ArSession* session,
                                               const char* output_folder);

// Stops recording. After this, the data can be accessed at the specified
// location. Clients can also start another recording session after calling
// stopRecording.
// Return values:
// AR_SUCCESS
// AR_ERROR_FATAL
/*DYNAMITE_FUNCTION_0*/
ArStatus ArSession_stopRecording_experimental(ArSession* session);

// Experimental API for AR Faces
// A detected face trackable (@ref ownership "reference type,
// long-lived").
//
// Trackable type: #AR_EXPERIMENTAL_TRACKABLE_FACE <br>
// Release with: ArTrackable_release()
typedef struct ArFace_ ArFace;

// Upcasts to ArTrackable
inline ArTrackable* ArAsTrackable(ArFace* face) {
  return reinterpret_cast<ArTrackable*>(face);
}

// Downcasts to ArFace
inline ArFace* ArAsFace(ArTrackable* trackable) {
  return reinterpret_cast<ArFace*>(trackable);
}

//  Used in ArConfig to indicate whether the face tracking is to be enabled or
//  disabled. Default value is disable
AR_DEFINE_ENUM(ArFaceTrackingMode){// Disable face tracking
                                   AR_FACE_TRACKING_MODE_DISABLED = 0,

                                   // Enable face tracking
                                   AR_FACE_TRACKING_MODE_ENABLED = 1};

// Used in ArConfig to indicate which camera is to be used for ArSession
AR_DEFINE_ENUM(ArCameraId){
    // Default rear facing camera. In-case of dual/multi cameras facing
    // outwards, ARCore chooses an appropriate camera
    AR_WORLD_FACING_CAMERA = 0,

    // Default front facing camera. In-case of dual/multi cameras facing
    // inwards, ARCore chooses an appropriate camera
    // Only inward facing camera is supported when face tracking mode is enabled
    AR_INWARD_FACING_CAMERA = 1};

// Stores the currently configured face tracking mode into
// @c *face_tracking_mode.
/*DYNAMITE_FUNCTION_0*/
void ArConfig_getFaceTrackingMode_experimental(
    const ArSession* session, const ArConfig* config,
    ArFaceTrackingMode* face_tracking_mode);

// Sets the face tracking mode that should be used. See
// ::ArFaceTrackingMode for available options.
/*DYNAMITE_FUNCTION_0*/
void ArConfig_setFaceTrackingMode_experimental(
    const ArSession* session, ArConfig* config,
    ArFaceTrackingMode face_tracking_mode);

/// Stores the currently configured camera id into
/// @c *camera_id.
/*DYNAMITE_FUNCTION_0*/
void ArConfig_getCameraId_experimental(const ArSession* session,
                                       const ArConfig* config,
                                       ArCameraId* camera_id);

// Sets the camera id that should be used.
// See ArCameraId for available options.
/*DYNAMITE_FUNCTION_0*/
void ArConfig_setCameraId_experimental(const ArSession* session,
                                       ArConfig* config, ArCameraId camera_id);

// Creates a new configuration object and initializes it appropriately for
// front face tracking. Plane detection and lighting estimation are disabled,
// and blocking update is selected.
/*DYNAMITE_FUNCTION_0*/
void ArConfig_createFrontFaceTracking(const ArSession* session,
                                      ArConfig** out_config);

// Returns number of vertices. The number of texture coordinates is the same as
// this, and this value stays constant since the topology of the face remains
// the same.
/*DYNAMITE_FUNCTION_0*/
void ArFace_getNumberOfVertices(const ArSession* session, const ArFace* face,
                                int32_t* out_number_of_vertices);

// Returns a pointer to an array of vertices in (x, y, z) packing.
// The pointer returned by this function is valid until ArTrackable_release()
// is called. The application must copy the data if they wish to retain it for
// longer.
/*DYNAMITE_FUNCTION_0*/
void ArFace_getVertices(const ArSession* session, const ArFace* face,
                        const float** out_vertices);

// Returns a pointer to an array of uv texture coordinates in (u, v) packing.
// There is a pair texture coordinates for each vertex. These values
// never change.
/*DYNAMITE_FUNCTION_0*/
void ArFace_getTextureCoordinates(const ArSession* session, const ArFace* face,
                                  const float** out_texture_coordinates);

// Returns number of triangles. This is 1/3 the number of triangle indices.
// These values never change.
/*DYNAMITE_FUNCTION_0*/
void ArFace_getNumberOfTriangles(const ArSession* session, const ArFace* face,
                                 int32_t* out_number_of_triangles);

// Returns a pointer to an array of triangles indices in consecutive triplets.
//
// Every consecutive three values are indices that represent a triangle. The
// vertex position and texture coordinates are mapped by the indices. The front
// face of each triangle is defined by the face where the vertices are in
// counterclockwise winding order. These values never change.
/*DYNAMITE_FUNCTION_0*/
void ArFace_getTriangleIndices(const ArSession* session, const ArFace* face,
                               const uint16_t** out_triangle_indices);

// Camera configuration objects and list.

/// @addtogroup cameraconfiguration
/// @{

/// A camera configuration struct that contains the config supported by
/// the physical camera obtained from the low level device profiles.
/// (@ref ownership "value type").
///
/// Allocate with ArCameraConfiguration_create()<br>
/// Release with ArCameraConfiguration_destroy()
typedef struct ArCameraConfiguration_ ArCameraConfiguration;

/// A list of camera configuration (@ref ownership "value type").
///
/// Allocate with ArCameraConfigurationList_create()<br>
/// Release with ArCameraConfigurationList_destroy()
typedef struct ArCameraConfigurationList_ ArCameraConfigurationList;

/// @}

// === ArCameraConfigurationList and ArCameraConfiguration methods ===

/// @addtogroup cameraconfiguration
/// @{

// === ArCameraConfigurationList methods ===

/// Creates an camera configuration list object.
/*DYNAMITE_FUNCTION_0*/
void ArCameraConfigurationList_create_experimental(
    const ArSession* session, ArCameraConfigurationList** out_list);

/// Releases the memory used by an camera configuration list object,
/// along with all the camera confguration references it holds.
/*DYNAMITE_FUNCTION_0*/
void ArCameraConfigurationList_destroy_experimental(
    ArCameraConfigurationList* list);

/// Retrieves the number of camera configurations in this list.
/*DYNAMITE_FUNCTION_0*/
void ArCameraConfigurationList_getSize_experimental(
    const ArSession* session, const ArCameraConfigurationList* list,
    int32_t* out_size);

/// Retrieves the specific camera configuration based based on the position
/// in this list.
/*DYNAMITE_FUNCTION_0*/
void ArCameraConfigurationList_getItem_experimental(
    const ArSession* session, const ArCameraConfigurationList* list,
    int32_t index, ArCameraConfiguration* out_camera_config);

// === ArCameraConfiguration methods ===

/// Creates an camera configuration object/struct
/*DYNAMITE_FUNCTION_0*/
void ArCameraConfiguration_create_experimental(
    const ArSession* session, ArCameraConfiguration** out_camera_config);

/// Releases the memory used by an camera configuration struct
/*DYNAMITE_FUNCTION_0*/
void ArCameraConfiguration_destroy_experimental(
    ArCameraConfiguration* camera_config);

/// Obtains the camera image width for the given camera configuration
/*DYNAMITE_FUNCTION_0*/
void ArCameraConfiguration_getImageWidth_experimental(
    const ArSession* session, const ArCameraConfiguration* camera_config,
    int32_t* out_width);

/// Obtains the camera image height for the given camera configuration
/*DYNAMITE_FUNCTION_0*/
void ArCameraConfiguration_getImageHeight_experimental(
    const ArSession* session, const ArCameraConfiguration* camera_config,
    int32_t* out_height);

/// Obtains the texture width for the given camera configuration
/*DYNAMITE_FUNCTION_0*/
void ArCameraConfiguration_getTextureWidth_experimental(
    const ArSession* session, const ArCameraConfiguration* camera_config,
    int32_t* out_width);

/// Obtains the texture height for the given camera configuration
/*DYNAMITE_FUNCTION_0*/
void ArCameraConfiguration_getTextureHeight_experimental(
    const ArSession* session, const ArCameraConfiguration* camera_config,
    int32_t* out_height);

/// Obtains the frames per second (fps) for the given camera configuration
/*DYNAMITE_FUNCTION_0*/
void ArCameraConfiguration_getFps_experimental(
    const ArSession* session, const ArCameraConfiguration* camera_config,
    int32_t* out_fps);

/// @}

// Camera configuration filters and camera configuration filters objects.

/// @addtogroup cameraconfigurationfilters
/// @{

/// A camera configuration filter struct contains the filters that are desired
/// by the application. (@ref ownership "value type").
///
/// Allocate with ArCameraConfigurationFilters_create()<br>
/// Release with ArCameraConfigurationFilters_destroy()
typedef struct ArCameraConfigurationFilters_ ArCameraConfigurationFilters;

/// @}

// -----------------------------------------------------------------------------
// New type, ArCameraConfigurationFilters, value type
//

AR_DEFINE_ENUM(ArCameraConfigurationFilterType){
    // Camera image defaults to enabled, as ARCore provides a CPU
    // accessible camera image by default.
    AR_CAMERA_CONFIGURATION_FILTER_IMAGE = 1,
    // Camera texture defaults to enabled, as ARCore has a passthrough
    // texture enabled by default.
    AR_CAMERA_CONFIGURATION_FILTER_TEXTURE = 2,
    // The frames per second rate is set to 30 by default for backward
    // capability.
    AR_CAMERA_CONFIGURATION_FILTER_FPS_30 = 3,
    // The frame rate of 60 is turned off by default.
    AR_CAMERA_CONFIGURATION_FILTER_FPS_60 = 4};

// Gets the specified configuration filter enable mode.
/*DYNAMITE_FUNCTION_0*/
void ArCameraConfigurationFilters_getEnabled_experimental(
    const ArSession* session, const ArCameraConfigurationFilters* filters,
    const ArCameraConfigurationFilterType filterType,
    int32_t* out_filter_enabled);

// Sets the specified configuration filter enable mode.
/*DYNAMITE_FUNCTION_0*/
void ArCameraConfigurationFilters_setEnabled_experimental(
    const ArSession* session, ArCameraConfigurationFilters* filters,
    const ArCameraConfigurationFilterType filterType, int32_t filter_enabled);

// Create a camera configuration filters object with default values set for
// backward compatibility. The caller can update the configuration filters it
// wants and then get the matching camera configurations.
/*DYNAMITE_FUNCTION_0*/
void ArCameraConfigurationFilters_create_experimental(
    const ArSession* session, ArCameraConfigurationFilters** out_filters);

// Cleanup the camera configuration filters object after usage by making this
// destroy call.
/*DYNAMITE_FUNCTION_0*/
void ArCameraConfigurationFilters_destroy_experimental(
    ArCameraConfigurationFilters* filters);

// Enumerate the list of supported camera configurations for the given set of
// camera configuration filters. Can be called at any time. The supported
// camera configurations will filled in the provided list.
/*DYNAMITE_FUNCTION_0*/
void ArSession_getSupportedCameraConfigurations_experimental(
    const ArSession* session, const ArCameraConfigurationFilters* filters,
    ArCameraConfigurationList* list);

// Set the ArCameraConfiguration that the ArSession should use.  The provided
// ArCameraConfiguration must be from a list returned by
// ArSession_getSupportedCameraConfigurations.  The session must be paused.
//
// @return #AR_SUCCESS or any of:
// - #AR_ERROR_FATAL
// - #AR_ERROR_SESSION_NOT_PAUSED
/*DYNAMITE_FUNCTION_0*/
ArStatus ArSession_setCameraConfiguration_experimental(
    const ArSession* session, const ArCameraConfiguration* camera_config);

// -----------------------------------------------------------------------------
// Vertical Plane new types.
//
// Detection of only vertical planes is enabled.
#define AR_EXPERIMENTAL_PLANE_FINDING_MODE_VERTICAL ((ArPlaneFindingMode)2)

// Detection of horizontal and vertical planes is enabled.
#define AR_EXPERIMENTAL_PLANE_FINDING_MODE_HORIZONTAL_AND_VERTICAL \
  ((ArPlaneFindingMode)3)

// A vertical plane (for example a wall).
#define AR_EXPERIMENTAL_PLANE_VERTICAL ((ArPlaneType)2)

#undef AR_DEFINE_ENUM

#ifdef __cplusplus
}  // extern "C"
#endif

#endif  // THIRD_PARTY_ARCORE_AR_CORE_C_API_ARCORE_C_API_INTERNAL_H_
