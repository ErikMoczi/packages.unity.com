# Animation C# Jobs Samples

The **Animation C# Jobs** allow to modify the animation stream directly inside
the data flow of the animation tree. This feature is powered by the
**Playables**.

This package gives examples of how this feature could be used:

* CustomMixer
* FullBodyIK
* TwoBoneIK

## Prerequisites

In order to play with the animation C# jobs, knowledge of C# and the Playable
system is required.

## See also

The [GraphVisualizer](https://github.com/Unity-Technologies/graph-visualizer)
allows to dynamically see all the Playable graphs in the scene.



## Preparing your package for Staging

Before publishing your package to production, you must send your package on the Package Manager's **staging** repository.  The staging repository is monitored by QA and release management, and is where package validation will take place before it is accepted in production.

1. Publishing your changes to the package manager's **staging** repository happens from Gitlab.  To do so, simply setup your project's Continuous integration, which will be triggered by "Tags" on your branches.
    * Join the **#devs-packman** channel on Slack, and request a staging **USERNAME** and **API_KEY**.
    * In Gitlab, under the **Settings-> CI/CD -> Secret Variables** section, setup the following 2 project variables:
        * API_KEY = [your API KEY]
        * USER_NAME = [your USER NAME@unity]
    * You're almost done!  To publish a version of your package, make sure all your changes are checked into Gitlab, then create a new tag to reflect the version you are publishing (ex. "v1.2.2"), **the tag will trigger a publish to Staging**. You can view progress you the publish request by switch over to the "CI / CD" part of your project.

2. Test your package locally

    Now that your package is published on the package manager's **staging** repository, you can test your package in the editor by creating a new project, and editing the project's `manifest.json` file to point to your staging package, as such:
      ```
      dependencies: {
        "com.unity.[sub-group].[your-package-name]": "0.1.0"
      },
      "registry": "https://staging-packages.unity.com"
      ```

## Get your package published to Production

Packages are promoted to the **production** repository from **staging**, described above. Certain criteria must be met before submitting a request to promote a package to production.
[The list of criteria can be found here](https://docs.google.com/document/d/1TSnlSKJ6_h0C-CYO2LvV0fyGxJvH6OxC2-heyN8o-Gw/edit#heading=h.xxfb5jk2jda2)

Once you feel comfortable that your package meets the list of Release Management Criteria, [Submit your package publishing request to Release Management](https://docs.google.com/forms/d/e/1FAIpQLSdSIRO6s6_gM-BxXbDtdzIej-Hhk-3n68xSyC2sM8tp7413mw/viewform).

**Release management will validate your package content, and check that the editor/playmode tests are passed before promoting the package to production.  You will receive a confirmation email once the package is in production.**

**You're not done!**
At this point, your package is available on the cloud, 2 more steps are required to make your package discoverable in the editor:

1. Contact the Package Manager team in #devs-packman to ask them to add your package to the list of discoverable package for the Unity Editor.  All you need to provide is the package name (com.unity.[sub-group].[your-package-name])
1. If your package is meant to ship with a release of the editor (Unity Recommended Packages), follow these steps:
	* Modify the editor manifest ``[root]\External\PackageManager\Editor\Manifest.json`` to include your package in the list of dependencies.
    * Submit one or more Test Project(s) in Ono, so that your new package can be tested in all ABVs moving forward.  The following steps will create a test project that will run in ABVs, load your package into the project, and run all the tests found in your package.  The better your test coverage, the more confident you'll be that your package works with trunk.
    	* Create a branch in Ono, based on the latest branch this package must be compatible with (trunk, or release branch)
    	* If your package contains **Editor Tests**:
    		* In ``[root]\Tests\Editor.Tests``, create a new EditorTest Project (for new packages use **YourPackageName**) or use an existing project (for new versions of existing package).
    		* A [skeleton of EditorTest Project can be found here](https://oc.unity3d.com/index.php/s/Cldvuy6NpxqYy8y).
    		* Modify the project’s manifest.json file to include the production version of the package (name@version).
    		* Your project's manifest.json file should contain the following line: ``"testables" : [ "com.unity.[sub-group].[your-package-name]" ]``
    	* If your package contains **PlaymodeTests**:
    		* In ``[root]\Tests\PlaymodeTests``, create a new PlaymodeTest Project (for new packages use **YourPackageName**) or use an existing project (for new versions of existing package).
    		* Modify the project’s manifest.json file to include the staging version of the package (name@version).
    		* Your project's manifest.json file should contain the following line: ``"testables" : [ "com.unity.[sub-group].[your-package-name]" ]``.
    		* Commit your branch changes to Ono, and run all Windows & Mac Editor/PlayMode tests (not full ABV) in Katana.
    * Once the tests are green on Katana, create your PR, add both `Latest Release Manager` and  `Trunk Merge Queue` as reviewers.
