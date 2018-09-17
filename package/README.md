## The Master branch is currently targeting 19.1 ##

# About _High Definition Project Template_

This template is a good starting point for people focused on high-end graphics that want to develop games for platforms that support Shader Model 5.0 (DX11 and above). 
This template utilizes the High Definition Render Pipeline, a modern rendering pipeline that includes advanced material types and a configurable hybrid tile/cluster deferred/forward lighting architecture.
This template also includes the new Shadergraph tool, Post-Processing stack, several Presets to jump start development, and example content. 

This Project Template uses the following features:

* High Definition Render Pipeline - For more information, see the <a href="https://github.com/Unity-Technologies/ScriptableRenderPipeline/wiki">wiki</a> on the Scriptable Render Pipeline repository.
* Shader Graph tool - This tool allows you to create shaders using a visual node editor instead of writing code. For more information on the Shader Graph, see the <a href="https://github.com/Unity-Technologies/ShaderGraph/wiki">wiki</a> on the Shader Graph repository.
* Post-processing stack - The Post-Processing Stack enables artists and designers to apply full -screen filters to scenes using an artist-friendly interface. For more information, see the <a href="https://github.com/Unity-Technologies/PostProcessing/wiki">wiki</a>  on the Post-Processing GitHub repository.

*Note:* The High Definition Render Pipeline is currently in development, so consider it incomplete and subject to change (API, UX, scope). As such, it is not covered by regular Unity support. Unity is seeking feedback on the feature. To ask questions about the feature, visit the <a href="https://forum.unity.com/forums/graphics-experimental-previews.110/?_ga=2.9250101.1048197026.1528313382-1647295365.1509665782">Unity preview forum</a>.

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

    ![Template in new project](Packages/com.unity.template.hd/Documentation~/images/template_in_new_project.png)
