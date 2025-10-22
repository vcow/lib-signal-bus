# Signal bus
This is the multithread signal bus for Unity that lets you dispatch strongly typed messages safely across threads and scene scopes. Create a shared `SignalBus`, subscribe to the signals you need with `Subscribe<T>(Action<T>)`, and call `Fire(signal)` (or `Fire<T>()`) to notify every subscriber, including listeners attached to child buses created via `CreateSubBus()`.  
<br/>Use it to decouple systems, keep gameplay logic thread-friendly, and still control the lifetime of subscriptions by disposing a bus (or any of its sub-buses) when a scope ends.

<br/>**CommandExtension.** Commands are signals that must be handled exactly once. The extension enforces this contract: `SubscribeCommand<T>()` guarantees there is only one listener, and `FireCommand(command)` throws if the command has zero or several subscribers. This makes commands a safe fit for request–response style interactions where a single handler is required.

<br/>**R3 Observable extension.** The R3 integration wraps `SignalBus` subscriptions into an `Observable<T>` so you can manage signal listeners as `IDisposable`. Call `ObserveSignal<T>()` to bridge the bus with reactive pipelines and dispose the subscription alongside any other reactive resources.

## How to install
Select one of the following methods:

1. From Unity package.<br/>Select latest release from the https://github.com/vcow/lib-signal-bus/releases and download **signal-bus.unitypackage** from Assets section.

2. From Git URL.<br/>Go to **Package Manager**, press **+** in the top left of window and select **Install package from git URL**. Enter the URL below:
```
https://github.com/vcow/lib-signal-bus.git#upm
```
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;or
```
https://github.com/vcow/lib-signal-bus.git#1.0.0
```
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;if you want to install exactly 1.0.0 version.

3. From OpenUPM.<br/>Go to **Edit -> Project Settings -> Package Manager** and add next scoped registry:
* **Name**: package.openupm.com
* **URL**: https://package.openupm.com
* **Scope(s)**: com.vcow.signal-bus

&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;Press **Save**, then go to **Package Manager** and install **Signal Bus** from the **My Registries -> package.openupm.com** section.

4. Add to the `manifest.json`.<br/>Open `manifest.json` and add next string to the `dependencies` section:
```
{
  "dependencies": {
    "com.vcow.signal-bus": "https://github.com/vcow/lib-signal-bus.git#upm",
    ...
  }
}
```
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;or
```
{
  "dependencies": {
    "com.vcow.signal-bus": "https://github.com/vcow/lib-signal-bus.git#1.0.0",
    ...
  }
}
```
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;if you want to install exactly 1.0.0 version.

### To install SignalBusExtensions
1. From Unity package.<br/>Select latest release from the https://github.com/vcow/lib-signal-bus/releases and download **signal-bus.unitypackage** from Assets section.

3. From Git URL.<br/>Go to **Package Manager**, press **+** in the top left of window and select **Install package from git URL**. Enter the URL below:
```
https://github.com/vcow/lib-signal-bus.git#upm-extensions
```
