# Documentation Overview

Documentation can be selected as a build configuration when exporting. All documentation is built on demand using the project setup and dependencies. What this means is that you only see what you have included in your project. User defined objects and assets are automatically exported.

All documentation is generated using `jsdoc`.

**Example**
```javascript

/**
 * @method
 * @desc Brief description of the method
 * @param {int32}  param1 - Description of parameter 1
 * @param {string} param2 - Description of parameter 2
 * @returns {object} Description of return object
 */
function MyCustomMethod(param1, param2) {

}
```

[Full spec can be found here http://usejsdoc.org/](http://usejsdoc.org/)

# Tutorials

Custom tutorials or files can be added to the documentation. This allows you to write arbitrary files that are placed in the tutorial section.

Tutorials can have the following extensions.

- .htm
- .html
- .markdown (converted from Markdown to HTML)
- .md (converted from Markdown to HTML)
- .xhtml
- .xml (treated as HTML)

Tutorials can be added by creating a configuration file with a `.json` extensions and adding it to your project or module under `Settings > Documentation > Tutorial Configuration`

**Example**

```json
{
	"tutorialFileNameWithNoExtension": {
		"title": "My Tutorial"
	}
}
```

Tutorials can be linked to from documentation

**IMPORTANT** During the build process tutorials names are converted to thier fully qualified name. Make sure to include the module or project namespace when linking

```javascript
/**
 * ...
 * @tutorial namespace.tutorialFileNameWithNoExtension
 * ...
 */
```

[For more information see http://usejsdoc.org/about-tutorials.html](http://usejsdoc.org/about-tutorials.html)