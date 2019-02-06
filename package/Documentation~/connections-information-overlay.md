# Connections Information Overlay

After opening a project, regardless if it is the first time or not, the Connections Information Overlay will **not** be visible until a successful build is made. A build can be performed by pressing the `Play` button at the top of the editor window.

Once one successful build is finished, a local HTTP server will be automatically started to host your project's last successful build content at `http://localhost:{port}`. The Connections Information Overlay should now be visible over the Game view.

The Connections Information Overlay will display when the last build was made, a QR code that you can press or scan with a device to open a browser to your project's last successful build, and the list of connected clients. The view can be minimzed or maximized using the button at the bottom left of the Game view.

When used with [Play Mode](play-mode.md) enabled, selecting the `Active Client` will affect which client will be synchronized when pressing the `Pause` button.