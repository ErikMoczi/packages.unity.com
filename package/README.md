## The Master branch is currently targeting 19.1 ##

# About _3D With Extras Project Template_

This template is a good starting point for people who want to develop 3D projects using Unity’s built-in renderer and extra graphics features. This project includes the new 
Post-Processing stack, several presets to jump-start development, and example content.

This Project Template uses the following features:

* Post-processing stack - The Post-Processing Stack enables artists and designers to apply full -screen filters to scenes using an artist-friendly interface. For more information, see the <a href="https://github.com/Unity-Technologies/PostProcessing/wiki">wiki</a>  on the Post-Processing GitHub repository.

*Note:* The Post Processing V2 package is currently in development, so consider it incomplete and subject to change (API, UX, scope). As such, it is not covered by regular Unity support. Unity is seeking feedback on the feature. To ask questions about the feature, visit the <a href="https://forum.unity.com/forums/graphics-experimental-previews.110/?_ga=2.9250101.1048197026.1528313382-1647295365.1509665782">Unity preview forum</a>.

## Trying out this template locally.

If you want to try out this template locally from a user's perspective, you will need to make it available to Unity. This can be accomplished by following these steps:

1. Package your project template

    It is necessary to package your template to make it available to Unity, since you cannot simply copy your current package template development project.

    We include a `build` script that can easily do this. The script is used in Gitlab CI pipelines and can be used locally. You can package your project template locally through the following command: 

    ```
    ./build.sh template-ci pack
    ```
    or on Windows machines
    ```
    build.bat template-ci pack
    ```

    This will output a `com.unity.template.<your-template-name>-0.1.0-preview.1.tgz` file in your project's folder.

    You need to make sure you have `Node.js` and `npm` _(install it from [here](https://nodejs.org/en/))_ installed on your machine to package successfully, as the script calls `npm` under the hood for packaging and publishing. The script is tested with `node@v6.10.0` and `npm@3.10.10`.

    Additionally, you need to make sure you have internet access when you use the `build` script for the first time, as the script will download and install the dependant `Node.js` package that's used under the hood.

    You cannot directly use `npm pack`, as the project template folder structure is different from the package folder structure.

1. Include the project template package in Unity editor

    You can then copy the project template's `tgz` package file in Unity in one of these paths to make it available in the editor when creating new projects:

    1. Mac: `<Unity Editor Root>/Contents/Resources/PackageManager/ProjectTemplates`

    1. Windows: `<Unity Editor Root>/Data/Resources/PackageManager/ProjectTemplates`

1. Preview your project template

    The result when creating a new project should be something like this when using the Unity Hub:

    ![Template in new project](Packages/com.unity.template.3dextra/Documentation~/images/template_in_new_project.png)

    If you are launching the Unity editor without the hub, you will not see additional templates in the list.
