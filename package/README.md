## The Master branch is currently targeting 19.1 ##

# About _3D Project Template_

Empty 3D Project. Configures Project settings for 3D apps that use Unity’s built-in rendering pipeline.

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

    ![Template in new project](Packages/com.unity.template.3d/Documentation~/images/template_in_new_project.png)

    If you are launching the Unity editor without the hub, you will not see additional templates in the list.
