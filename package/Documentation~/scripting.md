# Scripting in Tiny Mode

Scripting in Tiny Mode is based on the [Entity-Component-System](https://en.wikipedia.org/wiki/Entity%E2%80%93component%E2%80%93system) (ECS) architectural pattern. While you can author entities and components in the Editor, Systems are authored in code.

Tiny supports a single programming language at the moment: [TypeScript](https://www.typescriptlang.org/).

_Note: as of December 2018, we are working on C# support, which will become the recommended programming language on Tiny. You will be able to invoke JavaScript from C# on Web platforms._


## Setup

You don't need to install anything to get TypeScript working in Tiny, but you may want to setup a different IDE to open **.ts** files. You can configure your default TypeScript IDE by opening the _Preferences_ settings window (**Unity / Preferences...** on Mac, **Edit / Preferences...** on Windows), go to the _Tiny Preferences_ tab, and modify the _IDE Path_ property to your preferred IDE.

We highly recommend [Visual Studio Code](https://code.visualstudio.com/), but other popular IDEs such as JetBrains Rider, Sublime Text, and Visual Studio also work just fine.

