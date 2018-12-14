# Advanced Topics


## **Low-level: Entity Command Buffers**

Using an **EntityCommandBuffer** object, you can create a list of changes (commands), and commit them to a **World** later. Command buffers allow you to create entities, destroy them, and add / modify / remove components on them.

You can store a list of commands in an **EntityCommandBuffer** object, and **commit** it when needed. For example, you can use command buffers to _queue_ changes during iteration, and effectively _defer_ structural changes until the iteration is over.


## **tsconfig.json**

Tiny will generate and maintain a **tsconfig.json** file at the root of your _Unity_ project folder (not the Tiny project folder). You can ignore this file from source control, as it is regularly updated by the Editor - for example when you switch between projects, modify module dependencies, etc.

Should you need to modify this file - for example to control compiler options - you can add a **tsconfig.override.json** file at the root of your project **Scripts** folder. What you put in this file gets merged in the final **tsconfig.json** file when updated.

Here's an example you can use to disallow implicit _any_ type references in your scripts:
```
{
    "compilerOptions": {
        "noImplicitAny": true
    }
}
```

See the [tsconfig.json](https://www.typescriptlang.org/docs/handbook/tsconfig-json.html) page of the TypeScript handbook for more options.
