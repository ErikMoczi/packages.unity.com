# Play Mode

You can enable or disable Play Mode through the Tiny menu option by clicking the `Enable Play Mode` to toggle the option. When enabled, clicking Unity `Play` button at the top of the editor will not only launch a build of your project, but also allows Unity to enter Play Mode. At this point, [Connections Information Overlay](connections-information-overlay.md) should be visible over the Game view.

When in Play Mode, the project re-opens in read-only mode, and it is now possible to pause and download the world state of a running client by pressing the `Pause` button. The downloaded world state will be loaded in the editor, enabling inspection of entities and components. **Note that any changes made while in Play Mode are not persisted or reflected back to running clients.**

Exiting Play Mode does **not** close the local HTTP server, so you can quickly re-open a page to the local HTTP server content if needed by either pressing or scanning the QR code in the [Connections Information Overlay](connections-information-overlay.md). Closing your project will stop the local HTTP server.