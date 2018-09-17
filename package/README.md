## The Master branch is currently targeting 19.1 ##

# About _2D Project Template_

Empty 2D Project. Configures Project settings for 2D apps that use Unity’s built-in rendering pipeline. Special 2D configurations include Texture (Image) Import, Sprite Packer, Scene View, Lighting, and Orthographic Camera.

## Trying out this template locally.

If you want to try out this template locally from a user's perspective, you will need to make it available to Unity. This can be accomplished by following these steps:

1. Package your project template

    It is necessary to package your template to make it available to Unity, since you cannot simply copy your current package template development project.

    We include a python `build.py` script that can easily do this. The script is used in Gitlab CI pipelines and can be used locally. You can package your project template locally through the following command:

    ```
    python build.py package
    ```

    This will output a `com.unity.template.<your-template-name>-0.1.0-preview.1.tgz` file in your project's folder.

    You need to make sure you have both `python` _(install it from [here](https://www.python.org/downloads/))_ and `npm` _(install it from [here](https://nodejs.org/en/))_ installed on your machine to package successfully, as the `build.py` calls `npm` under the hood for packaging and publishing. The script is tested with `python 2.7` and `python 3.6`.

    You cannot directly use `npm package`, as the project template folder structure is different from the package folder structure.

1. Include the project template package in Unity editor

    You can then copy the project template's `tgz` package file in Unity in one of these paths to make it available in the editor when creating new projects:

    1. Mac: `<Unity Editor Root>/Contents/Resources/PackageManager/ProjectTemplates`

    1. Windows: `<Unity Editor Root>/Data/Resources/PackageManager/ProjectTemplates`

    The result when creating a new project should be something like this:

    ![Template in new project](Packages/com.unity.template.3d/Documentation~/images/template_in_new_project.png)
