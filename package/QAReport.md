# QA Report
Use this file to outline the test strategy for this package.

## QA Owner: [Pedro Albuquerque](mailto:pedroa@unity3d.com)

## Test strategy
* A link to the Test Plan (Test Rails, other)
* Results from the package's editor and runtime test suite.
```none
./utr.pl --suite=editor --testprojects=PackageManagerUI
Overall result: PASS
Total Tests run: 64, Passed: 64, Failures: 0, Errors: 0, Inconclusives: 0
Total not run : 0, Invalid: 0, Ignored: 0, Skipped: 0
Duration: 2.327 seconds```
* Link to automated test results (if any)
```none
Suite UnityEditor.PackageManager.UI.Tests.PackageCollectionTests
	Case 1012 [AddPackageInfo_PackagesChangeEventIsPropagated] : Passed
	Case 1011 [AddPackageInfos_PackagesChangeEventIsPropagated] : Passed
	Case 1013 [ClearPackages_PackagesChangeEventIsPropagated] : Passed
	Case 1002 [Constructor_Instance_FilterIsLocal] : Passed
	Case 1003 [Constructor_Instance_PackageInfosIsEmpty] : Passed
	Case 1004 [SetFilter_WhenFilterChange_FilterChangeEventIsPropagated] : Passed
	Case 1006 [SetFilter_WhenFilterChange_FilterIsChanged] : Passed
	Case 1008 [SetFilter_WhenFilterChangeNoRefresh_PackagesChangeEventIsNotPropagated] : Passed
	Case 1005 [SetFilter_WhenNoFilterChange_FilterChangeEventIsNotPropagated] : Passed
	Case 1009 [SetFilter_WhenNoFilterChangeNoRefresh_PackagesChangeEventIsNotPropagated] : Passed
	Case 1007 [SetFilter_WhenNoFilterChangeRefresh_PackagesChangeEventIsNotPropagated] : Passed
	Case 1010 [SetPackageInfos_PackagesChangeEventIsPropagated] : Passed
Suite UnityEditor.PackageManager.UI.Tests.PackageInfoTests
	Case 1019 [IsInPreview_WhenPackageVersionMajorIsGreaterThanZero_ReturnsFalse] : Passed
	Case 1018 [IsInPreview_WhenPackageVersionMajorIsZero_ReturnsTrue] : Passed
	Case 1017 [IsInPreview_WhenPreviewPackageVersionTagIsNotPreview_ReturnsFalse] : Passed
	Case 1015 [IsInPreview_WhenPreviewPackageVersionTagIsPreviewLowerCase_ReturnsTrue] : Passed
	Case 1016 [IsInPreview_WhenPreviewPackageVersionTagIsPreviewUpperCase_ReturnsTrue] : Passed
	Case 1021 [VersionWithoutTag_WhenVersionContainsOtherTag_ReturnsVersionOnly] : Passed
	Case 1020 [VersionWithoutTag_WhenVersionContainsPreviewTag_ReturnsVersionOnly] : Passed
	Case 1022 [VersionWithoutTag_WhenVersionDoesNotContainTag_ReturnsVersionOnly] : Passed
Suite UnityEditor.PackageManager.UI.Tests.PackageManagerWindowTests
	Case 1050 [When_Default_FirstPackageUIElement_HasSelectedClass] : Passed
	Case 1052 [When_Default_PackageGroupsCollapsedState] : Passed
	Case 1057 [When_Filter_Changes_Shows_Correct_List] : Passed
	Case 1051 [When_PackageCollection_Changes_PackageList_Updates] : Passed
	Case 1056 [When_PackageCollection_Remove_Fails_PackageLists_NotUpdated] : Passed
	Case 1055 [When_PackageCollection_Remove_PackageLists_Updated] : Passed
	Case 1054 [When_PackageCollection_Update_Fails_Package_Stay_Current] : Passed
	Case 1053 [When_PackageCollection_Updates_PackageList_Updates] : Passed
Suite UnityEditor.PackageManager.UI.Tests.PackageTests
	Case 1035 [Add_WhenPackageInfoIsCurrent_AddOperationIsNotCalled] : Passed
	Case 1036 [Add_WhenPackageInfoIsNotCurrent_AddOperationIsCalled] : Passed
	Case 1041 [CanBeRemoved_WhenNotPackageManagerUIPackage_ReturnsTrue] : Passed
	Case 1040 [CanBeRemoved_WhenPackageManagerUIPackage_ReturnsFalse] : Passed
	Case 1027 [Constructor_WithEmptyPackageInfos_ThrowsException] : Passed
	Case 1025 [Constructor_WithEmptyPackageName_ThrowsException] : Passed
	Case 1034 [Constructor_WithMultiplePackagesInfo_VersionsCorrespond] : Passed
	Case 1026 [Constructor_WithNullPackageInfos_ThrowsException] : Passed
	Case 1024 [Constructor_WithNullPackageName_ThrowsException] : Passed
	Case 1028 [Constructor_WithOnePackageInfo_CurrentIsFirstVersion] : Passed
	Case 1030 [Constructor_WithOnePackageInfo_LatestAndCurrentAreEqual] : Passed
	Case 1029 [Constructor_WithOnePackageInfo_LatestIsLastVersion] : Passed
	Case 1031 [Constructor_WithTwoPackageInfos_CurrentIsFirstVersion] : Passed
	Case 1032 [Constructor_WithTwoPackageInfos_LatestIsLastVersion] : Passed
	Case 1033 [Constructor_WithTwoPackagesInfo_LatestAndCurrentAreNotEqual] : Passed
	Case 1046 [Display_WhenCurrentAndLatest_ReturnsLatest] : Passed
	Case 1044 [Display_WhenCurrentIsNotNull_ReturnsCurrent] : Passed
	Case 1045 [Display_WhenCurrentIsNull_ReturnsLatest] : Passed
	Case 1043 [DocumentationLink_ReturnsNotEmptyString] : Passed
	Case 1042 [Name_ReturnsExpectedValue] : Passed
	Case 1039 [Remove_RemoveOperationIsCalled] : Passed
	Case 1037 [Update_WhenCurrentIsLatest_AddOperationIsNotCalled] : Passed
	Case 1038 [Update_WhenCurrentIsNotLatest_AddOperationIsCalled] : Passed
	Case 1047 [Versions_WhenOrderedPackageInfo_ReturnsOrderedValues] : Passed
	Case 1048 [Versions_WhenUnorderedPackageInfo_ReturnsOrderedValues] : Passed
Suite UnityEditor.PackageManager.ValidationSuite.Tests.ManifestValidationTests
	Case 1062 [When_Description_Short_Validation_Fails] : Passed
	Case 1060 [When_Manifest_OK_Validation_Succeeds] : Passed
	Case 1059 [When_Manifest_WrongFormat_Validation_Fails] : Passed
	Case 1061 [When_Name_Invalid_Validation_Fails] : Passed
	Case 1063 [When_PackageVersion_WrongFormat_Validation_Fails] : Passed
	Case 1064 [When_UnityVersion_NotMatching_Validation_Fails] : Passed
Suite UnityEditor.PackageManager.ValidationSuite.Tests.ValidationSuiteTests
	Case 1066 [When_AllTests_Pass_Suite_Succeeds] : Passed
	Case 1070 [When_Cancel_PartialResults_Registered] : Passed
	Case 1067 [When_NewTest_Is_Written_Its_AutoDiscovered] : Passed
	Case 1069 [When_OneTests_Crashes_Suite_Fails] : Passed
	Case 1068 [When_OneTests_Fails_Suite_Fails] : Passed
```
* Manual test results, [Package Manager UI - QA Matrix](https://docs.google.com/a/unity3d.com/spreadsheets/d/1Vh4x1Tjk1Pvv9NER6mFShBIwNvN6wOtjVlo89OTfunY/edit?usp=sharing)

## Package Status
* package stability
	* Stable
* known bugs, issues
	* The Package Manager includes the following known limitations:
	Modifying the manifest.json by hand doesn't update the package list. You need to either re-open the window or change filters to force an update.

In other words, a general feeling on the health of this package.
